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

        // This method gets the user's graph user object. It should take an email as input and return the user object.
        public User GetGraphUser(string email, GraphServiceClient graphclient)
        {
            Console.WriteLine($"Getting user with email: {email}");
            try
            {
                UserCollectionResponse? users;
                users = graphclient.Users.GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Select =
                        ["id", "displayName", "mail", "userPrincipalName"];
                    requestConfig.QueryParameters.Filter =
                        $"mail eq '{email}' or userPrincipalName eq '{email}'";
                }).Result;

                if (users == null || users.Value == null || !users.Value.Any())
                {
                    Console.WriteLine($"User with email {email} not found");
                    return null;
                }

                User user = users.Value.FirstOrDefault();
                Console.WriteLine($"User found: {user.DisplayName}");
                return user;
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
    }
}

