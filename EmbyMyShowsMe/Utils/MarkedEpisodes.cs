using System;
using System.Collections.Generic;

using EmbyMyShowsMe.Configuration;

using MediaBrowser.Controller.Entities.TV;

namespace EmbyMyShowsMe.Utils
{
    internal class MarkedEpisodes
    {
        public UserConfig User { get; set; }
        public Guid CurrentSeriesId { get; set; }
        public List<Episode> SeenEpisodes { get; set; }
        public List<Episode> UnSeenEpisodes { get; set; }

        public MarkedEpisodes()
        {
            SeenEpisodes = new List<Episode>();
            UnSeenEpisodes = new List<Episode>();
        }
    }
}
