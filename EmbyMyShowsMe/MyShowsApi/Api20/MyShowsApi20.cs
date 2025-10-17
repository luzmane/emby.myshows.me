using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using EmbyMyShowsMe.Cache;
using EmbyMyShowsMe.Configuration;
using EmbyMyShowsMe.MyShowsApi.Api20.Dto;
using EmbyMyShowsMe.Utils;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace EmbyMyShowsMe.MyShowsApi.Api20
{
    internal class MyShowsApi20 : IMyShowsApi
    {
        private static readonly TimeSpan CachedShowStorageInterval = TimeSpan.FromHours(24);

        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ExpirableCache<string, ShowSummary> _showsCache = new ExpirableCache<string, ShowSummary>();
        private readonly List<Guid> _lastWatchedShows = new List<Guid>();
        private int _counter = 1;

        public MyShowsApi20(ILogger logger, IHttpClient httpClient, IJsonSerializer jsonSerializer)
        {
            _logger = logger;
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
        }

        public async Task<bool> SetShowStatusToWatching(UserConfig user, Series item)
        {
            if (_lastWatchedShows.Contains(item.Id))
            {
                return true;
            }

            var show = await GetShow(user, item);
            if (show == null)
            {
                return false;
            }

            var showStatus = await Execute<ShowStatus[]>(user, "profile.ShowStatuses", new ProfileShowStatuses
            {
                showIds = new[] { show.id }
            });

            if (showStatus?.FirstOrDefault()?.watchStatus == "watching")
            {
                _lastWatchedShows.Add(item.Id);
                return true;
            }

            var success = await Execute<bool>(user, "manage.SetShowStatus", new ManageSetShowStatusArgs
            {
                id = show.id,
                status = "watching"
            });

            if (success)
            {
                _lastWatchedShows.Add(item.Id);
            }

            return success;
        }

        public async Task<bool> CheckEpisode(UserConfig user, Episode item)
        {
            return await ToggleEpisode(user, item, true);
        }

        public async Task<bool> UnCheckEpisode(UserConfig user, Episode item)
        {
            return await ToggleEpisode(user, item, false);
        }

        public async Task<bool> SyncEpisodes(UserConfig user, List<Episode> seen, List<Episode> unseen)
        {
            if (!seen.Any() && !unseen.Any())
            {
                return false;
            }

            var firstEpisode = seen.Any() ? seen.First() : unseen.First();
            var show = await GetShow(user, firstEpisode.Series);
            if (show == null)
            {
                return false;
            }

            var seenIds = new List<int>();
            var unSeenIds = new List<int>();
            foreach (var ep in seen)
            {
                var episode = show.episodes.FirstOrDefault(e => e.seasonNumber == ep.Season.IndexNumber && e.episodeNumber == ep.IndexNumber);
                if (episode != null)
                {
                    seenIds.Add(episode.id);
                }
            }

            foreach (var ep in unseen)
            {
                var episode = show.episodes.FirstOrDefault(e => e.seasonNumber == ep.Season.IndexNumber && e.episodeNumber == ep.IndexNumber);
                if (episode != null)
                {
                    unSeenIds.Add(episode.id);
                }
            }

            var success = await Execute<bool>(user, "manage.SyncEpisodesDelta", new ManageSyncEpisodesDeltaArgs
            {
                showId = show.id,
                checkedIds = seenIds.ToArray(),
                unCheckedIds = unSeenIds.ToArray(),
            });
            return success;
        }

        private async Task<ShowSummary> GetShow(UserConfig user, Series item)
        {
            var (id, source) = item.GetBestProviderId();
            if (source == null)
            {
                _logger.Warn("Not found any provider id for show '{0}'", item.Name);
                return null;
            }

            var cacheKey = id + source;
            var show = _showsCache.Get(cacheKey);
            if (show != null)
            {
                return show;
            }

            show = await Execute<ShowSummary>(user, "shows.GetByExternalId", new ShowsGetByExternalIdArgs
            {
                id = id,
                source = source
            });
            if (show == default(ShowSummary))
            {
                return null;
            }

            show = await Execute<ShowSummary>(user, "shows.GetById", new ShowsGetByIdArgs
            {
                showId = show.id,
                withEpisodes = true
            });

            _showsCache.Store(cacheKey, show, CachedShowStorageInterval);

            return show;
        }

        private async Task<bool> ToggleEpisode(UserConfig user, Episode item, bool check)
        {
            var method = check ? "manage.CheckEpisode" : "manage.UnCheckEpisode";
            var show = await GetShow(user, item.Series);
            if (show == null)
            {
                return false;
            }

            var episode = show.episodes.First(e => e.seasonNumber == item.Season.IndexNumber && e.episodeNumber == item.IndexNumber);

            var success = await Execute<bool>(user, method, new ManageEpisodeArgs
            {
                id = episode.id
            });
            return success;
        }

        private async Task<T> Execute<T>(UserConfig user, string method, object args)
        {
            var isTokenValid = await user.EnsureAccessTokenValid();
            if (!isTokenValid)
            {
                _logger.Warn("AccessToken invalidated and RefreshToken isn't helped. Too bad.");
                return default;
            }

            var call = new JsonRpcCall
            {
                jsonrpc = "2.0",
                id = _counter++,
                method = method,
                @params = args,
            };

            try
            {
                var request = new HttpRequestOptions
                {
                    CancellationToken = CancellationToken.None,
                    Url = ApiConstants.RpcUri,
                    BufferContent = false,
                    RequestContent = _jsonSerializer.SerializeToString(call).AsMemory(),
                    RequestContentType = "application/json"
                };
                request.RequestHeaders.Add("Authorization", $"Bearer {user.AccessToken}");
                var response = await _httpClient.Post(request);

                var result = await _jsonSerializer.DeserializeFromStreamAsync<JsonRpcResult<T>>(response.Content);
                if (result.error != null)
                {
                    _logger.Warn("JSON-RPC error: {0}", result.error.message);
                }

                return result.result;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Failed calling MyShows.me API due: {Message}", ex, ex.Message);
                return default;
            }
        }
    }
}
