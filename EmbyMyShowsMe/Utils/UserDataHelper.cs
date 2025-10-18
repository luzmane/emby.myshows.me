using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using EmbyMyShowsMe.Configuration;
using EmbyMyShowsMe.MyShowsApi;

using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;

namespace EmbyMyShowsMe.Utils
{
    internal class UserDataHelper
    {
        private const int TimerDelay = 5000;
        private readonly ILogger _logger;
        private readonly MyShowsApiFactory _client;
        private Timer _timer;
        private readonly List<MarkedEpisodes> _episodes = new List<MarkedEpisodes>();

        public UserDataHelper(ILogger logger, MyShowsApiFactory client)
        {
            _logger = logger;
            _client = client;
        }

        public async Task AddEvent(UserConfig user, UserDataSaveEventArgs args)
        {
            if (!(args.Item is Episode episode))
            {
                return;
            }

            var episodes = _episodes.FirstOrDefault(e => e.User.Equals(user));
            if (episodes == null)
            {
                episodes = new MarkedEpisodes
                {
                    User = user
                };
                _episodes.Add(episodes);
            }

            if (!episodes.CurrentSeriesId.Equals(episode.Series.Id))
            {
                if (episodes.CurrentSeriesId != Guid.Empty)
                {
                    await SendData(episodes);
                }

                episodes.CurrentSeriesId = episode.Series.Id;
            }

            if (args.UserData.Played)
            {
                episodes.SeenEpisodes.Add(episode);
                if (episodes.UnSeenEpisodes.Contains(episode))
                {
                    episodes.UnSeenEpisodes.Remove(episode);
                }
            }
            else
            {
                episodes.UnSeenEpisodes.Add(episode);
                if (episodes.SeenEpisodes.Contains(episode))
                {
                    episodes.SeenEpisodes.Remove(episode);
                }
            }

            Postpone();
        }

        private async Task SendData(MarkedEpisodes episodes)
        {
            var success = await _client
                .GetApi(episodes.User.ApiVersion)
                .SyncEpisodes(episodes.User, episodes.SeenEpisodes, episodes.UnSeenEpisodes);
            _logger.Info("Synced {0} episodes: {1}",
                episodes.SeenEpisodes.Count + episodes.UnSeenEpisodes.Count,
                success ? "success" : "failed");

            episodes.SeenEpisodes.Clear();
            episodes.UnSeenEpisodes.Clear();
        }

        private void Postpone()
        {
            if (_timer == null)
            {
                _timer = new Timer(OnTimerCallback, null, TimeSpan.FromMilliseconds(TimerDelay),
                    Timeout.InfiniteTimeSpan);
            }
            else
            {
                _timer.Change(TimeSpan.FromMilliseconds(TimerDelay), Timeout.InfiniteTimeSpan);
            }
        }

        private async void OnTimerCallback(object state)
        {
            try
            {
                foreach (var e in _episodes.Where(e => e.SeenEpisodes.Any() || e.UnSeenEpisodes.Any()))
                {
                    await SendData(e);
                }
            }
            catch (Exception ex)
            {
                _logger.Info("OnTimerCallback failed due to: {1}", ex.Message);
            }
        }
    }
}
