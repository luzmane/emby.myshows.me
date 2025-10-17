using System;
using System.Collections.Generic;

using EmbyMyShowsMe.Configuration;
using EmbyMyShowsMe.MyShowsApi;
using EmbyMyShowsMe.Utils;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace EmbyMyShowsMe
{
    public class Scrobbler : IServerEntryPoint
    {
        private readonly ILogger _logger;
        private readonly ISessionManager _sessionManager;
        private readonly IUserDataManager _userDataManager;
        private readonly List<Guid> _lastScrobbled;
        private MyShowsApiFactory _apiFactory;
        private UserDataHelper _userDataHelper;
        private DateTime _nextTry;

        public Scrobbler(
            ISessionManager sessionManager,
            IUserDataManager userDataManager,
            ILogManager logManager,
            IHttpClient httpClient,
            IJsonSerializer jsonSerializer
        )
        {
            _sessionManager = sessionManager;
            _userDataManager = userDataManager;
            _logger = logManager.GetLogger(Plugin.Instance.Name);

            _apiFactory = new MyShowsApiFactory(_logger, httpClient, jsonSerializer);
            _userDataHelper = new UserDataHelper(_logger, _apiFactory);

            _nextTry = DateTime.UtcNow;
            _lastScrobbled = new List<Guid>();
        }

        /// <inheritdoc />
        public void Run()
        {
            this._userDataManager.UserDataSaved += OnUserDataSaved;
            this._sessionManager.PlaybackStart += OnPlaybackStart;
            this._sessionManager.PlaybackStopped += OnPlaybackStopped;
            this._sessionManager.PlaybackProgress += OnPlaybackProgress;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this._userDataManager.UserDataSaved -= OnUserDataSaved;
            this._sessionManager.PlaybackStart -= OnPlaybackStart;
            this._sessionManager.PlaybackStopped -= OnPlaybackStopped;
            this._sessionManager.PlaybackProgress -= OnPlaybackProgress;

            _apiFactory = null;
            _userDataHelper = null;
            _lastScrobbled.Clear();
        }

        private async void OnPlaybackProgress(object sender, PlaybackProgressEventArgs e)
        {
            try
            {
                if (DateTime.UtcNow < _nextTry)
                {
                    return; // postpone
                }

                _nextTry = DateTime.UtcNow.AddSeconds(30);

                if (!CheckConstraintsAndGetUser(e.Session.UserId, e.Item, out var user))
                {
                    return;
                }

                if (e.Session.PlayState.PositionTicks == null || e.Session.NowPlayingItem.RunTimeTicks == null)
                {
                    return;
                }

                // don't scrobble if percentage watched is below 90%
                float percentageWatched = (float)e.Session.PlayState.PositionTicks / (float)e.Session.NowPlayingItem.RunTimeTicks * 100f;
                if (percentageWatched < user.ScrobbleAt)
                {
                    return;
                }

                if (!(e.Item is Episode episode))
                {
                    return;
                }

                if (_lastScrobbled.Contains(episode.Id))
                {
                    return;
                }

                _logger.Info("Item is played 90%. Scrobble");
                var result = await _apiFactory.GetApi(user.ApiVersion).CheckEpisode(user, episode);
                _logger.Info("Checked episode '{0}' S{1}E{2} {3}",
                    episode.Series.Name,
                    episode.Season.IndexNumber,
                    episode.IndexNumber,
                    result ? "successfully" : "failed");
                if (result)
                {
                    _lastScrobbled.Add(episode.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending watching status update on playback progress", ex);
            }
        }

        private async void OnPlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            try
            {
                _logger.Info("Playback Started");
                if (!CheckConstraintsAndGetUser(e.Session.UserId, e.Item, out var user))
                {
                    return;
                }

                if (!(e.Item is Episode episode))
                {
                    return;
                }

                var result = await _apiFactory.GetApi(user.ApiVersion).SetShowStatusToWatching(user, episode.Series);
                _logger.Debug("Started watching show '{0}' {1}", episode.Series.Name, result ? "successfully" : "failed");
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending watching status update on playback start", ex);
            }
        }

        private async void OnPlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            try
            {
                _logger.Info("Playback Stopped");

                if (!CheckConstraintsAndGetUser(e.Session.UserId, e.Item, out var user))
                {
                    return;
                }

                if (!e.PlayedToCompletion)
                {
                    return;
                }

                if (!(e.Item is Episode episode))
                {
                    return;
                }

                if (_lastScrobbled.Contains(episode.Id))
                {
                    return;
                }

                _logger.Info("Item is played. Scrobble");

                var result = await _apiFactory.GetApi(user.ApiVersion).CheckEpisode(user, episode);
                _logger.Info("Checked episode '{0}' S{1}E{2} {3}",
                    episode.Series.Name,
                    episode.Season.IndexNumber,
                    episode.IndexNumber,
                    result ? "successfully" : "failed");
                if (result)
                {
                    _lastScrobbled.Add(episode.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending watching status update on playback stop", ex);
            }
        }

        private async void OnUserDataSaved(object sender, UserDataSaveEventArgs e)
        {
            try
            {
                // ignore change events for any reason other than manually toggling played.
                if (e.SaveReason != UserDataSaveReason.TogglePlayed)
                {
                    return;
                }

                if (e.Item == null)
                {
                    return;
                }

                var user = Plugin.Instance.Configuration.GetUserByGuid(e.User.Id);

                // Can't progress
                if (user == null || !CanSync(e.Item))
                {
                    return;
                }

                await _userDataHelper.AddEvent(user, e);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending watching status update on data save", ex);
            }
        }

        private bool CheckConstraintsAndGetUser(string userId, BaseItem item, out UserConfig user)
        {
            user = Plugin.Instance.Configuration.GetUserById(userId);
            if (user == null)
            {
                _logger.Info("Could not match user with any stored credentials");
                return false;
            }

            if (!CanSync(item))
            {
                _logger.Debug("Can not sync this type of items: {0}", item?.MediaType);
                return false;
            }

            return true;
        }

        private static bool CanSync(BaseItem item)
        {
            if (item?.Path == null || item.LocationType == LocationType.Virtual)
            {
                return false;
            }

            if (item is Episode episode
                && episode.Series != null
                && episode.Season?.IndexNumber != null
                && episode.IndexNumber.HasValue
                && !episode.IsMissingEpisode
               )
            {
                var (_, source) = episode.Series.GetBestProviderId();
                return source != null;
            }

            return false;
        }
    }
}
