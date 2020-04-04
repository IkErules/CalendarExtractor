using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace CalendarExtractor.API.Helper
{
    public class DeviceCodeAuthProvider : IAuthenticationProvider
    {
        private readonly IConfidentialClientApplication _msalClient;
        private readonly string[] _scope = {"https://graph.microsoft.com/.default"};
        private IAccount _userAccount;

        public DeviceCodeAuthProvider(string clientId, string secret, string authority)
        {
            _msalClient = ConfidentialClientApplicationBuilder.Create(clientId)
               .WithClientSecret(secret)
               .WithAuthority(new Uri(authority))
               .Build();
        }

        // This is the required function to implement IAuthenticationProvider
        // The Graph SDK will call this function each time it makes a Graph
        // call.
        public async Task AuthenticateRequestAsync(HttpRequestMessage requestMessage)
        {
            requestMessage.Headers.Authorization =
                new AuthenticationHeaderValue("bearer", await GetAccessToken());
        }

        private async Task<string> GetAccessToken()
        {
            // If there is no saved user account, the user must sign-in
            if (_userAccount == null)
            {
                try
                {
                    // Invoke device code flow so user can sign-in with a browser
                    var result = await _msalClient.AcquireTokenForClient(_scope).ExecuteAsync();

                    _userAccount = result.Account;
                    return result.AccessToken;
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Error getting access token: {exception.Message}");
                    return null;
                }
            }
            else
            {
                // If there is an account, call AcquireTokenSilent
                // By doing this, MSAL will refresh the token automatically if
                // it is expired. Otherwise it returns the cached token.
                var result = await _msalClient
                    .AcquireTokenSilent(_scope, _userAccount)
                    .ExecuteAsync();

                return result.AccessToken;
            }
        }
    }
}