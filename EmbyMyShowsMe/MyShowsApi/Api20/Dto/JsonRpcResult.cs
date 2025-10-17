namespace EmbyMyShowsMe.MyShowsApi.Api20.Dto
{
    public class JsonRpcResult<T>
    {
        public string jsonrpc { get; set; }
        public T result { get; set; }
        public int id { get; set; }
        public JsonRpcError error { get; set; }
    }
}
