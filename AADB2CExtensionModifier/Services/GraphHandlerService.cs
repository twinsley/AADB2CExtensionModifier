using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AADB2CExtensionModifier.Services
{
    internal class GraphHandlerService
    {
        // This method is used to get the B2C extension application id and parse it for use in querying the extension attributes.
        public string GetB2cExtensionAppId(GraphServiceClient graphclient)
        {
            string appId = string.Empty;
            try
            {
                Debug.WriteLine("Searching for B2C extensions app...");
                ApplicationCollectionResponse apps = graphclient.Applications.GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Select =
                        ["id", "displayName", "appId"];
                    requestConfig.QueryParameters.Filter =
                        $"startswith(displayname, 'b2c-extensions-app')";
                }).Result;
                
                Debug.WriteLine($"Found {apps?.Value?.Count ?? 0} applications matching b2c-extensions-app");
                
                if (apps?.Value != null)
                {
                    foreach (var application in apps.Value)
                    {
                        Debug.WriteLine($"  App: {application.DisplayName}, AppId: {application.AppId}");
                    }
                }
                
                Application app = apps?.Value?.FirstOrDefault();
                if (app != null && !string.IsNullOrEmpty(app.AppId))
                {
                    appId = app.AppId.Replace("-", "");
                    Debug.WriteLine($"B2C Extension App Id (formatted): {appId}");
                }
                else
                {
                    Debug.WriteLine("B2C Extension App not found or AppId is empty");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting B2C Extension App Id: {ex.Message}");
            }
            return appId;
        }

        public List<IdentityUserFlowAttribute> GetExtensionAttributes(GraphServiceClient graphclient)
        {
            try
            {
                List<IdentityUserFlowAttribute> extensionAttributes = graphclient.Identity.UserFlowAttributes.GetAsync().Result?.Value?.ToList();
                Debug.WriteLine($"Extension Attributes Count: {extensionAttributes?.Count ?? 0}");
                return extensionAttributes ?? new List<IdentityUserFlowAttribute>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting extension attributes: {ex.Message}");
                return new List<IdentityUserFlowAttribute>();
            }
        }

        // This method gets the B2C extension attributes for the user.
        public async Task<User> GetUserExtensionAttributesAsync(string userIdentifier, GraphServiceClient graphclient, string b2cExtensionAppId)
        {
            try
            {
                // Get the list of extension property names to explicitly request them
                var extensionPropertyNames = GetB2cExtensionPropertyNames(graphclient, b2cExtensionAppId);
                
                // Build the select list with all standard properties (*) and all extension properties
                var selectProperties = new List<string> { "*" };
                selectProperties.AddRange(extensionPropertyNames);
                
                Debug.WriteLine($"Requesting user with all standard properties and {extensionPropertyNames.Count} extension properties explicitly");

                // Get user with all standard properties and explicitly requested extension attributes
                var user = await graphclient.Users[userIdentifier].GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Select = selectProperties.ToArray();
                });

                // Microsoft Graph doesn't return extension properties that have never been set, even when explicitly requested
                // Manually add missing extension properties with null values
                if (user != null)
                {
                    user.AdditionalData ??= new Dictionary<string, object>();
                    
                    int addedCount = 0;
                    foreach (var extensionPropertyName in extensionPropertyNames)
                    {
                        if (!user.AdditionalData.ContainsKey(extensionPropertyName))
                        {
                            user.AdditionalData[extensionPropertyName] = null;
                            addedCount++;
                            Debug.WriteLine($"Added missing extension property with null value: {extensionPropertyName}");
                        }
                    }
                    
                    Debug.WriteLine($"Added {addedCount} missing extension properties as null");
                }

                return user;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting user extension attributes: {ex.Message}");
                throw;
            }
        }

        // This method updates the user's extension attributes.
        public async Task UpdateUserExtensionAttributesAsync(string userIdentifier, Dictionary<string, object> extensionAttributes, GraphServiceClient graphclient)
        {
            try
            {
                var user = new User
                {
                    AdditionalData = extensionAttributes
                };

                await graphclient.Users[userIdentifier].PatchAsync(user);
                Debug.WriteLine($"Successfully updated extension attributes for user: {userIdentifier}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating user extension attributes: {ex.Message}");
                throw;
            }
        }

        // This method deletes a user from Azure AD B2C.
        public async Task DeleteUserAsync(string userIdentifier, GraphServiceClient graphclient)
        {
            try
            {
                Debug.WriteLine($"Attempting to delete user with ID: {userIdentifier}");
                await graphclient.Users[userIdentifier].DeleteAsync();
                Debug.WriteLine($"Successfully deleted user: {userIdentifier}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting user: {ex.Message}");
                throw;
            }
        }

        // This method gets the user's graph user object. It searches across all identity fields including B2C identities.
        public User GetGraphUser(string email, GraphServiceClient graphclient, string tenantDomain = null)
        {
            Debug.WriteLine($"Getting user with email/identity: {email}");
            try
            {
                // First, try searching by mail or userPrincipalName (works for standard Azure AD users)
                UserCollectionResponse? users;
                users = graphclient.Users.GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Select =
                        ["id", "displayName", "mail", "userPrincipalName", "identities"];
                    requestConfig.QueryParameters.Filter =
                        $"mail eq '{email}' or userPrincipalName eq '{email}'";
                }).Result;

                User user = users?.Value?.FirstOrDefault();
                
                if (user != null)
                {
                    Debug.WriteLine($"User found via mail/UPN: {user.DisplayName}");
                    return user;
                }

                // If tenant domain is provided, search by identities with issuer (B2C users)
                if (!string.IsNullOrEmpty(tenantDomain))
                {
                    Debug.WriteLine("User not found by mail/UPN, searching in identities with issuer...");
                    users = graphclient.Users.GetAsync(requestConfig =>
                    {
                        requestConfig.QueryParameters.Select =
                            ["id", "displayName", "mail", "userPrincipalName", "identities"];
                        // Search for users where any identity issuerAssignedId matches the email with the specified issuer
                        requestConfig.QueryParameters.Filter =
                            $"identities/any(c:c/issuerAssignedId eq '{email}' and c/issuer eq '{tenantDomain}')";
                    }).Result;

                    user = users?.Value?.FirstOrDefault();

                    if (user != null)
                    {
                        Debug.WriteLine($"User found via identities: {user.DisplayName}");
                        return user;
                    }
                }
                else
                {
                    // If no tenant domain, try getting all users and search client-side by identities
                    Debug.WriteLine("User not found by mail/UPN, searching all users with identities (client-side filter)...");
                    users = graphclient.Users.GetAsync(requestConfig =>
                    {
                        requestConfig.QueryParameters.Select =
                            ["id", "displayName", "mail", "userPrincipalName", "identities"];
                        requestConfig.QueryParameters.Top = 999;
                    }).Result;

                    user = users?.Value?.FirstOrDefault(u => 
                        u.Identities != null && 
                        u.Identities.Any(i => i.IssuerAssignedId != null && 
                                             i.IssuerAssignedId.Equals(email, StringComparison.OrdinalIgnoreCase)));

                    if (user != null)
                    {
                        Debug.WriteLine($"User found via identities (client-side): {user.DisplayName}");
                        return user;
                    }
                }

                // If still not found, try a broader search with startswith (less efficient but more thorough)
                Debug.WriteLine("User not found by identities, trying broader search...");
                users = graphclient.Users.GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Select =
                        ["id", "displayName", "mail", "userPrincipalName", "identities"];
                    requestConfig.QueryParameters.Filter =
                        $"startswith(mail,'{email}') or startswith(userPrincipalName,'{email}')";
                }).Result;

                user = users?.Value?.FirstOrDefault();

                if (user != null)
                {
                    Debug.WriteLine($"User found via broader search: {user.DisplayName}");
                    return user;
                }

                Debug.WriteLine($"User with email/identity {email} not found");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting graph user: {ex.Message}");
                throw;
            }
        }

        // method to authenticate to graph api
        public GraphServiceClient GetGraphClient(string tenantId, string clientId, string[] scopes)
        {
            Debug.WriteLine("Creating graph client");
            try
            {
                var options = new InteractiveBrowserCredentialOptions
                {
                    TenantId = tenantId,
                    ClientId = clientId,
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                    RedirectUri = new Uri("http://localhost"),
                };
                
                var interactiveCredential = new InteractiveBrowserCredential(options);
                var graphClient = new GraphServiceClient(interactiveCredential, scopes);
                
                Debug.WriteLine("Graph client created");
                return graphClient;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating graph client: {ex.Message}");
                throw;
            }
        }

        // This method retrieves the verified domain for the tenant
        public string GetTenantDomain(GraphServiceClient graphclient)
        {
            try
            {
                var organization = graphclient.Organization.GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Select = ["verifiedDomains"];
                }).Result;

                var org = organization?.Value?.FirstOrDefault();
                if (org?.VerifiedDomains != null)
                {
                    // Try to find the default domain first
                    var defaultDomain = org.VerifiedDomains.FirstOrDefault(d => d.IsDefault == true);
                    if (defaultDomain != null)
                    {
                        Debug.WriteLine($"Tenant domain (default): {defaultDomain.Name}");
                        return defaultDomain.Name;
                    }

                    // Otherwise, return the first verified domain
                    var firstDomain = org.VerifiedDomains.FirstOrDefault();
                    if (firstDomain != null)
                    {
                        Debug.WriteLine($"Tenant domain (first): {firstDomain.Name}");
                        return firstDomain.Name;
                    }
                }

                Debug.WriteLine("No verified domain found for tenant");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting tenant domain: {ex.Message}");
                return null;
            }
        }

        // This method gets all extension property names for the B2C extensions app
        public List<string> GetB2cExtensionPropertyNames(GraphServiceClient graphclient, string b2cExtensionAppId)
        {
            var extensionProperties = new List<string>();
            try
            {
                Debug.WriteLine($"Searching for extension properties for app ID: {b2cExtensionAppId}");

                // Convert the dashless GUID string back to standard GUID format
                string formattedAppId = b2cExtensionAppId;
                if (b2cExtensionAppId.Length == 32 && !b2cExtensionAppId.Contains('-'))
                {
                    // Insert dashes at the correct positions to create a valid GUID format
                    formattedAppId = b2cExtensionAppId.Insert(8, "-").Insert(13, "-").Insert(18, "-").Insert(23, "-");
                }

                Debug.WriteLine($"Formatted app ID for query: {formattedAppId}");

                // Find the application object ID (not the app/client ID)
                var apps = graphclient.Applications.GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Select = ["id", "appId"];
                    requestConfig.QueryParameters.Filter = $"appId eq '{formattedAppId}'";
                }).Result;

                var app = apps?.Value?.FirstOrDefault();
                if (app == null)
                {
                    Debug.WriteLine("Could not find application by appId, trying by display name...");
                    apps = graphclient.Applications.GetAsync(requestConfig =>
                    {
                        requestConfig.QueryParameters.Select = ["id", "appId"];
                        requestConfig.QueryParameters.Filter = "startswith(displayname, 'b2c-extensions-app')";
                    }).Result;
                    app = apps?.Value?.FirstOrDefault();
                }

                if (app != null)
                {
                    Debug.WriteLine($"Found application object ID: {app.Id}");

                    // Get extension properties for this application
                    var extensions = graphclient.Applications[app.Id].ExtensionProperties.GetAsync().Result;
                    Debug.WriteLine($"Found {extensions?.Value?.Count ?? 0} extension properties");

                    if (extensions?.Value != null)
                    {
                        foreach (var ext in extensions.Value)
                        {
                            Debug.WriteLine($"  Extension property: {ext.Name} (TargetObjects: {string.Join(",", ext.TargetObjects ?? new List<string>())})");
                            if (ext.Name != null)
                            {
                                extensionProperties.Add(ext.Name);
                            }
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("Could not find B2C extensions application");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting extension properties: {ex.Message}");
            }

            return extensionProperties;
        }
    }
}

