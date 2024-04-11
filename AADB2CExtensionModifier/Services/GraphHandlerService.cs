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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace AADB2CExtensionModifier.Services
{
    internal class GraphHandlerService
    {
        // This method is used to get the B2C extension application id and parse it for use in querying the extension attributes.
        public String GetB2cExtensionAppId(GraphServiceClient graphclient)
        {
            string appId = string.Empty;
            ApplicationCollectionResponse apps = graphclient.Applications.GetAsync(requestConfig =>
            {
                requestConfig.QueryParameters.Select =
                    ["id", "displayName", "appId"];
                requestConfig.QueryParameters.Filter =
                    $"startswith(displayname, 'b2c-extensions-app')";
            }).Result;
            Application app = apps.Value.FirstOrDefault();
            if (app != null)
            {
                appId = app.AppId.Replace("-", "");
            }
            Console.WriteLine($"B2C Extension App Id: {appId}");
            return appId;
        }
        public List<IdentityUserFlowAttribute> GetExtensionAttributes(GraphServiceClient graphclient)
        {
            List<IdentityUserFlowAttribute> extensionAttributes = graphclient.Identity.UserFlowAttributes.GetAsync().Result.Value.ToList();
            Console.WriteLine($"Extension Attributes: {extensionAttributes.ToString}");
            return extensionAttributes;
        }

        // This method gets the B2C extension attributes for the user.
        public User GetUserExtensionAttributes(string userIdentifier, GraphServiceClient graphclient)
        {
            IdentityUserFlowAttributeCollectionResponse extensionAttributes = graphclient.Identity.UserFlowAttributes.GetAsync(requestConfig =>
            {
                requestConfig.QueryParameters.Select =
                    ["id"];
            }).Result;
            String filterIdList = String.Join(',', extensionAttributes.Value.Select(x => x.Id));
            Console.WriteLine($"Filter Id List: {filterIdList}");
            User userAttributes = graphclient.Users[userIdentifier].GetAsync(requestConfig =>
            {
                requestConfig.QueryParameters.Select =
                    [$"'id', {filterIdList}"];
            }).Result;

            // TODO : Implement this method
            return userAttributes;
        }

        // This method updates the user's extension attributes.
        public void UpdateUserExtensionAttributes(string userIdentifier, User user, GraphServiceClient graphclient)
        {
            // TODO : Implement this method
        }

        // This method gets the user's graph user object. It should take an email as input and return the user object.
        public User GetGraphUser(string email, GraphServiceClient graphclient)
        {
            Console.WriteLine($"Getting user with email: {email}");
            UserCollectionResponse? users;
            users = graphclient.Users.GetAsync(requestConfig =>
            {
                requestConfig.QueryParameters.Select =
                    ["id", "displayName", "mail"];
                requestConfig.QueryParameters.Filter =
                    $"mail eq '{email}'";
            }).Result;
            if (users == null)
            {
                Console.WriteLine($"User with email {email} not found");
                return null;
            }
            User user = users.Value.FirstOrDefault();
            Console.WriteLine($"User found: {user.DisplayName}");
            return user;
        }

        // method to authenticate to graph api
        public GraphServiceClient GetGraphClient(string tenantId, string clientId, string[] scopes)
        {
            Console.WriteLine("Creating graph client");
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
    }
}

