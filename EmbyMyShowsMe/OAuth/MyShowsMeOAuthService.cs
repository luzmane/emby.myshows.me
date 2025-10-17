using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using EmbyMyShowsMe.MyShowsApi.Api20;

using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace EmbyMyShowsMe.OAuth
{
    public class MyShowsMeOAuthService
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;

        public MyShowsMeOAuthService(IHttpClient httpClient, ILogManager logManager, IJsonSerializer jsonSerializer)
        {
            _httpClient = httpClient;
            _logger = logManager.GetLogger(GetType().Name);
            _jsonSerializer = jsonSerializer;
        }

        public async Task<(OAuthToken, OAuthError)> GetToken(string login, string password)
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_id", ApiConstants.ClientId),
                new KeyValuePair<string, string>("client_secret", ApiConstants.ClientSecret),
                new KeyValuePair<string, string>("username", login),
                new KeyValuePair<string, string>("password", password)
            });

            return await SendRequest(formContent);
        }

        public async Task<(OAuthToken, OAuthError)> RefreshToken(string token)
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("client_id", ApiConstants.ClientId),
                new KeyValuePair<string, string>("client_secret", ApiConstants.ClientSecret),
                new KeyValuePair<string, string>("refresh_token", token)
            });

            return await SendRequest(formContent);
        }

        private async Task<(OAuthToken, OAuthError)> SendRequest(FormUrlEncodedContent formContent, CancellationTokenSource cancellationTokenSource = null)
        {
            try
            {
                var response = await _httpClient.Post(new HttpRequestOptions
                {
                    CancellationToken = cancellationTokenSource?.Token ?? CancellationToken.None,
                    Url = ApiConstants.OauthTokenUri,
                    BufferContent = false,
                    RequestContent = (await formContent.ReadAsStringAsync()).AsMemory()
                });

                if (response.StatusCode >= HttpStatusCode.OK && response.StatusCode < HttpStatusCode.Ambiguous)
                {
                    var token = await _jsonSerializer.DeserializeFromStreamAsync<OAuthToken>(response.Content);
                    return (token, null);
                }
                else
                {
                    var error = await _jsonSerializer.DeserializeFromStreamAsync<OAuthError>(response.Content);
                    return (null, error);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Failed to fetch token due: {Message}", ex, ex.Message);
                return (null, new OAuthError()
                {
                    error_description = ex.Message,
                });
            }
        }
    }
}
