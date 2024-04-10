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
        public String GetB2cExtensionAppId(string tenantId)
        {
            string appId = string.Empty;
            // TODO : Implement this method
            return appId;
        }

        public List<string> GetUserExtensionAttributes(string userIdentifier, string tenantId)
        {
            List<string> extensionAttributes = new List<string>();
            // TODO : Implement this method
            return extensionAttributes;
        }
    }
}

