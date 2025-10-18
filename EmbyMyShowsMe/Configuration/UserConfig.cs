using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

using EmbyMyShowsMe.MyShowsApi;

namespace EmbyMyShowsMe.Configuration
{
    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public class UserConfig
    {
        /// <summary>
        /// Emby's user id (Guid).
        /// </summary>
        public string Id { get; set; }

        public string Name { get; set; }

        public MyShowsApiVersion ApiVersion { get; set; }

        /// <summary>
        /// OAuth access token
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// OAuth refresh token.
        /// </summary>
        public string RefreshToken { get; set; }

        /// <summary>
        /// OAuth expires_in.
        /// </summary>
        public DateTime ExpirationTime { get; set; }

        /// <summary>
        /// Percentage after which to scrobble.
        /// </summary>
        public int ScrobbleAt { get; set; } = 90;

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != typeof(UserConfig))
            {
                return false;
            }

            if (!(obj is UserConfig other))
            {
                return false;
            }

            return Id != other.Id
                   && Name == other.Name
                   && ApiVersion.Equals(other.ApiVersion)
                   && AccessToken == other.AccessToken
                   && RefreshToken == other.RefreshToken
                   && ExpirationTime == other.ExpirationTime
                   && ScrobbleAt == other.ScrobbleAt
                ;
        }

        public override int GetHashCode()
        {
            return (Id,
                    Name,
                    ApiVersion,
                    AccessToken,
                    RefreshToken,
                    ExpirationTime,
                    ScrobbleAt)
                .GetHashCode();
        }

        /// <summary>
        /// Ensure OAuth access_token is valid and refresh if it's not.
        /// </summary>
        /// <returns>true if you can use token</returns>
        public async Task<bool> EnsureAccessTokenValid()
        {
            if (DateTime.Compare(ExpirationTime, DateTime.Now) >= 0)
            {
                return true;
            }

            try
            {
                var (token, error) = await Plugin.Instance.OAuthService.RefreshToken(RefreshToken);
                if (error != null)
                {
                    RemoveSelf();
                    return false;
                }

                AccessToken = token.access_token;
                RefreshToken = token.refresh_token;
                ExpirationTime = DateTime.Now.AddSeconds(token.expires_in);
                return true;
            }
            catch (Exception)
            {
                RemoveSelf();
                return false;
            }
            finally
            {
                Plugin.Instance.SaveConfiguration();
            }
        }

        private void RemoveSelf()
        {
            var self = Plugin.Instance.Configuration.Users.FirstOrDefault(user => user.Id.Equals(Id));
            if (self != null)
            {
                Plugin.Instance.Configuration.RemoveUser(self);
            }
        }
    }
}
