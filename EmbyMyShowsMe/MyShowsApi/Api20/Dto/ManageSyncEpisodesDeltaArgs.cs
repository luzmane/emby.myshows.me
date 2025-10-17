namespace EmbyMyShowsMe.MyShowsApi.Api20.Dto
{
    public class ManageSyncEpisodesDeltaArgs
    {
        public int showId { get; set; }
        public int[] checkedIds { get; set; }
        public int[] unCheckedIds { get; set; }
    }
}
