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
        public String GetB2cExtensionAppId(string tenantId, GraphServiceClient graphclient)
        {
            string appId = string.Empty;
            // TODO : Implement this method
            return appId;
        }

        // This method gets the B2C extension attributes for the user.
        public List<string> GetUserExtensionAttributes(string userIdentifier, string tenantId, GraphServiceClient graphclient)
        {
            List<string> extensionAttributes = new List<string>();
            // TODO : Implement this method
            return extensionAttributes;
        }

        // This method updates the user's extension attributes.
        public void UpdateUserExtensionAttributes(string userIdentifier, string tenantId, List<string> extensionAttributes, GraphServiceClient graphclient)
        {
            // TODO : Implement this method
        }

        // This method gets the user's graph user object. It should take an email as input and return the user object.
        public User GetGraphUser(string email, string tenantId, GraphServiceClient graphclient)
        {
            User user = new User();
            // TODO : Implement this method
            return user;
        }

        // method to authenticate to graph api
        public GraphServiceClient GetGraphClient(string tenantId, string clientId, string[] scopes)
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
            return graphClient;
        }
    }
}

