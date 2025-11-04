using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace PsiBot.Services.Authentication
{
    /// <summary>
    /// Implements resource owner password credential flow for bot integration scenarios.
    /// </summary>
    public class UserPasswordAuthenticationProvider : ObjectRoot, IRequestAuthenticationProvider
    {
        /// <summary>
        /// The application name.
        /// </summary>
        private readonly string appName;

        /// <summary>
        /// Gets the application identifier.
        /// </summary>
        /// <value>
        /// The application identifier.
        /// </value>
        private readonly string appId;

        /// <summary>
        /// Gets the application secret.
        /// </summary>
        /// <value>
        /// The application secret.
        /// </value>
        private readonly string appSecret;

        /// <summary>
        /// Username leveraged when requesting access tokens.
        /// </summary>
        private readonly string userName;

        /// <summary>
        /// Password leveraged when requesting access tokens.
        /// </summary>
        private readonly string password;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserPasswordAuthenticationProvider"/> class.
        /// </summary>
        /// <param name="appName">The application name.</param>
        /// <param name="appId">The application identifier.</param>
        /// <param name="appSecret">The application secret.</param>
        /// <param name="userName">The username to be used.</param>
        /// <param name="password">Password associated with the passed username.</param>
        /// <param name="logger">The logger.</param>
        public UserPasswordAuthenticationProvider(string appName, string appId, string appSecret, string userName, string password, IGraphLogger logger)
            : base(logger.NotNull(nameof(logger)).CreateShim(nameof(UserPasswordAuthenticationProvider)))
        {
            this.appName = appName.NotNullOrWhitespace(nameof(appName));
            this.appId = appId.NotNullOrWhitespace(nameof(appId));
            this.appSecret = appSecret.NotNullOrWhitespace(nameof(appSecret));

            this.userName = userName.NotNullOrWhitespace(nameof(userName));
            this.password = password.NotNullOrWhitespace(nameof(password));
        }

        /// <inheritdoc />
        public async Task AuthenticateOutboundRequestAsync(HttpRequestMessage request, string tenantId)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(tenantId), $"Invalid {nameof(tenantId)}.");

            const string BearerPrefix = "Bearer";
            const string ReplaceString = "{tenant}";
            const string TokenAuthorityMicrosoft = "https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token";
            const string Resource = @"https://graph.microsoft.com/.default";

            var tokenLink = TokenAuthorityMicrosoft.Replace(ReplaceString, tenantId);
            OAuthResponse authResult = null;

            try
            {
                using (var httpClient = new HttpClient())
                {
                    var result = await httpClient.PostAsync(tokenLink, new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", "password"),
                        new KeyValuePair<string, string>("username", this.userName),
                        new KeyValuePair<string, string>("password", this.password),
                        new KeyValuePair<string, string>("scope", Resource),
                        new KeyValuePair<string, string>("client_id", this.appId),
                        new KeyValuePair<string, string>("client_secret", this.appSecret),
                    })).ConfigureAwait(false);

                    if (!result.IsSuccessStatusCode)
                    {
                        throw new Exception("Failed to generate user token.");
                    }

                    var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                    authResult = JsonConvert.DeserializeObject<OAuthResponse>(content);

                    request.Headers.Authorization = new AuthenticationHeaderValue(BearerPrefix, authResult.Access_Token);
                }
            }
            catch (Exception ex)
            {
                this.GraphLogger.Error(ex, $"Failed to generate user token for user: {this.userName}");
                throw;
            }

            this.GraphLogger.Info($"Generated OAuth token. Expires in {authResult.Expires_In / 60}  minutes.");
        }

        /// <inheritdoc />
        public Task<RequestValidationResult> ValidateInboundRequestAsync(HttpRequestMessage request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Strongly typed representation of the OAuth token response.
        /// </summary>
        private class OAuthResponse
        {
            /// <summary>
            /// Access token issued for the call.
            /// </summary>
            public string Access_Token { get; set; }

            /// <summary>
            /// Expiration time (seconds) for the issued token.
            /// </summary>
            public int Expires_In { get; set; }
        }
    }
}
