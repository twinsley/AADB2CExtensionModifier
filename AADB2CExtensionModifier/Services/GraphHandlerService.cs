using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
                ApplicationCollectionResponse apps = graphclient.Applications.GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Select =
                        ["id", "displayName", "appId"];
                    requestConfig.QueryParameters.Filter =
                        $"startswith(displayname, 'b2c-extensions-app')";
                }).Result;
                
                Application app = apps?.Value?.FirstOrDefault();
                if (app != null && !string.IsNullOrEmpty(app.AppId))
                {
                    appId = app.AppId.Replace("-", "");
                }
                Console.WriteLine($"B2C Extension App Id: {appId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting B2C Extension App Id: {ex.Message}");
            }
            return appId;
        }

        public List<IdentityUserFlowAttribute> GetExtensionAttributes(GraphServiceClient graphclient)
        {
            try
            {
                List<IdentityUserFlowAttribute> extensionAttributes = graphclient.Identity.UserFlowAttributes.GetAsync().Result?.Value?.ToList();
                Console.WriteLine($"Extension Attributes Count: {extensionAttributes?.Count ?? 0}");
                return extensionAttributes ?? new List<IdentityUserFlowAttribute>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting extension attributes: {ex.Message}");
                return new List<IdentityUserFlowAttribute>();
            }
        }

        // This method gets the B2C extension attributes for the user.
        public async Task<User> GetUserExtensionAttributesAsync(string userIdentifier, GraphServiceClient graphclient, string b2cExtensionAppId)
        {
            try
            {
                // Get user with all properties including extension attributes
                var user = await graphclient.Users[userIdentifier].GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Select = new[] { "*" };
                });

                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user extension attributes: {ex.Message}");
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
                Console.WriteLine($"Successfully updated extension attributes for user: {userIdentifier}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user extension attributes: {ex.Message}");
                throw;
            }
        }

        // This method gets the user's graph user object. It searches across all identity fields including B2C identities.
        public User GetGraphUser(string email, GraphServiceClient graphclient, string tenantDomain = null)
        {
            Console.WriteLine($"Getting user with email/identity: {email}");
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
                    Console.WriteLine($"User found via mail/UPN: {user.DisplayName}");
                    return user;
                }

                // If tenant domain is provided, search by identities with issuer (B2C users)
                if (!string.IsNullOrEmpty(tenantDomain))
                {
                    Console.WriteLine("User not found by mail/UPN, searching in identities with issuer...");
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
                        Console.WriteLine($"User found via identities: {user.DisplayName}");
                        return user;
                    }
                }
                else
                {
                    // If no tenant domain, try getting all users and search client-side by identities
                    Console.WriteLine("User not found by mail/UPN, searching all users with identities (client-side filter)...");
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
                        Console.WriteLine($"User found via identities (client-side): {user.DisplayName}");
                        return user;
                    }
                }

                // If still not found, try a broader search with startswith (less efficient but more thorough)
                Console.WriteLine("User not found by identities, trying broader search...");
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
                    Console.WriteLine($"User found via broader search: {user.DisplayName}");
                    return user;
                }

                Console.WriteLine($"User with email/identity {email} not found");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting graph user: {ex.Message}");
                throw;
            }
        }

        // method to authenticate to graph api
        public GraphServiceClient GetGraphClient(string tenantId, string clientId, string[] scopes)
        {
            Console.WriteLine("Creating graph client");
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
                
                Console.WriteLine("Graph client created");
                return graphClient;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating graph client: {ex.Message}");
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
                        Console.WriteLine($"Tenant domain (default): {defaultDomain.Name}");
                        return defaultDomain.Name;
                    }

                    // Otherwise, return the first verified domain
                    var firstDomain = org.VerifiedDomains.FirstOrDefault();
                    if (firstDomain != null)
                    {
                        Console.WriteLine($"Tenant domain (first): {firstDomain.Name}");
                        return firstDomain.Name;
                    }
                }

                Console.WriteLine("No verified domain found for tenant");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting tenant domain: {ex.Message}");
                return null;
            }
        }
    }
}

