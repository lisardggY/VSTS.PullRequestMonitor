using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace VSTSMonitor
{
    internal class Authentication
    {
        internal const string VSTSResourceId = "499b84ac-1321-427f-aa17-267ca6975798";
        internal const string clientId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";          //change to your app registration's Application ID, unless you are an MSA backed account
        internal const string replyUri = "urn:ietf:wg:oauth:2.0:oob";         

        public static async Task<string> GetAccessToken()
        {
            AuthenticationContext ctx = GetAuthenticationContext(null);
            var result = await ctx.AcquireTokenAsync(VSTSResourceId, clientId, new Uri(replyUri), new PlatformParameters(PromptBehavior.Auto));
            return result.AccessToken;
        }

        private static AuthenticationContext GetAuthenticationContext(string tenant)
        {
            AuthenticationContext ctx = null;
            if (tenant != null)
                ctx = new AuthenticationContext("https://login.microsoftonline.com/" + tenant);
            else
            {
                ctx = new AuthenticationContext("https://login.windows.net/common");
                if (ctx.TokenCache.Count > 0)
                {
                    string homeTenant = ctx.TokenCache.ReadItems().First().TenantId;
                    ctx = new AuthenticationContext("https://login.microsoftonline.com/" + homeTenant);
                }
            }

            return ctx;
        }
    }
}