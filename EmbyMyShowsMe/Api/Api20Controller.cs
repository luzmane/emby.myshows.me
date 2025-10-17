using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;

using EmbyMyShowsMe.Configuration;
using EmbyMyShowsMe.MyShowsApi;

using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Services;

namespace EmbyMyShowsMe.Api
{

    [Authenticated(Roles = "User")]
    public class Api20Controller : IService
    {
        private readonly ILogger _logger;

        public Api20Controller(ILogManager logManager)
        {
            _logger = logManager.GetLogger(Plugin.Instance.Name);
        }

        public async Task<object> Post(ApiLoginRequest loginRequest)
        {
            _logger.Info("Try to login with emby userId '{id}' and MyShows.me login '{login}'", loginRequest.Id, loginRequest.Login);
            try
            {
                var (token, error) = await Plugin.Instance.OAuthService.GetToken(loginRequest.Login, loginRequest.Password);
                if (error != null)
                {
                    return new
                    {
                        success = false,
                        statusText = error.error_description
                    };
                }

                Plugin.Instance.Configuration.AddUser(new UserConfig
                {
                    ApiVersion = MyShowsApiVersion.V20,
                    AccessToken = token.access_token,
                    RefreshToken = token.refresh_token,
                    ExpirationTime = DateTime.Now.AddSeconds(token.expires_in),
                    Id = loginRequest.Id,
                    Name = loginRequest.Login
                });

                return new
                {
                    success = true
                };
            }
            catch (HttpRequestException e)
            {
                return new
                {
                    success = false,
                    statusText = e.Message
                };
            }
        }
    }

    [Route("/emby/MyShows/v2/login", "POST", Summary = "Login to MyShows.me to get token")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class ApiLoginRequest : IReturn<object>
    {
        /// <summary>
        /// Emby user ID
        /// </summary>
        [ApiMember(Name = "id", Description = "Emby user ID", IsRequired = true, DataType = "string", ParameterType = "form", Verb = "POST")]
        public string Id { get; set; }

        /// <summary>
        /// MyShows.me login
        /// </summary>
        [ApiMember(Name = "login", Description = "MyShows.me login", IsRequired = true, DataType = "string", ParameterType = "form", Verb = "POST")]
        public string Login { get; set; }

        /// <summary>
        /// MyShows.me password
        /// </summary>
        [ApiMember(Name = "password", Description = "MyShows.me password", IsRequired = true, DataType = "string", ParameterType = "form", Verb = "POST")]
        public string Password { get; set; }
    }
}
