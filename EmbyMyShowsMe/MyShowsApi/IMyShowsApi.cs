using System.Collections.Generic;
using System.Threading.Tasks;

using EmbyMyShowsMe.Configuration;

using MediaBrowser.Controller.Entities.TV;

namespace EmbyMyShowsMe.MyShowsApi
{
    internal interface IMyShowsApi
    {
        Task<bool> SetShowStatusToWatching(UserConfig user, Series item);
        Task<bool> CheckEpisode(UserConfig user, Episode item);
        Task<bool> UnCheckEpisode(UserConfig user, Episode item);
        Task<bool> SyncEpisodes(UserConfig user, List<Episode> seen, List<Episode> unseen);
    }
}
