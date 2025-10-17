namespace EmbyMyShowsMe.MyShowsApi.Api20.Dto
{
    public class JsonRpcCall
    {
        public string jsonrpc { get; set; }
        public string method { get; set; }
        public int id { get; set; }
        public object @params { get; set; }
    }
}
