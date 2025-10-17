using System;

using EmbyMyShowsMe.MyShowsApi.Api20;

using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace EmbyMyShowsMe.MyShowsApi
{
    internal class MyShowsApiFactory
    {
        private readonly IMyShowsApi _api20;

        public MyShowsApiFactory(ILogger logger, IHttpClient httpClient, IJsonSerializer jsonSerializer)
        {
            _api20 = new MyShowsApi20(logger, httpClient, jsonSerializer);
        }

        public IMyShowsApi GetApi(MyShowsApiVersion version)
        {
            switch (version)
            {
                case MyShowsApiVersion.V20:
                    return _api20;
                default:
                    throw new Exception("Unknown API version");
            }
        }
    }
}
