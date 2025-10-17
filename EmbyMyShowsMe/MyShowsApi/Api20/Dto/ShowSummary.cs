namespace EmbyMyShowsMe.MyShowsApi.Api20.Dto
{
    public class ShowSummary
    {
        public int id { get; set; }
        public string title { get; set; }
        public string titleOriginal { get; set; }
        public string status { get; set; }
        public EpisodeSummary[] episodes { get; set; }
    }
}
