using System;
using System.Collections.Generic;
using System.IO;

using EmbyMyShowsMe.Configuration;
using EmbyMyShowsMe.OAuth;

using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace EmbyMyShowsMe
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
    {
        public static Plugin Instance { get; private set; }

        public override string Name => "MyShows.Me";
        public override string Description => "Scrobble your watched shows with MyShows.me";
        public override Guid Id => Guid.Parse("ef35f6b1-7fe6-44ca-1234-232089fb9bc7");

        public MyShowsMeOAuthService OAuthService { get; private set; }

        public Plugin(
            IApplicationPaths appPaths,
            IXmlSerializer xmlSerializer,
            MyShowsMeOAuthService oAuthService
        ) : base(appPaths, xmlSerializer)
        {
            Instance = this;
            OAuthService = oAuthService;
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "myshowsme",
                    EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.myshowsme.html"
                },
                new PluginPageInfo
                {
                    Name = "myshowsmejs",
                    EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.myshowsme.js"
                },
            };
        }

        /// <inheritdoc />
        public Stream GetThumbImage()
        {
            Type type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".thumb.png");
        }

        /// <inheritdoc />
        public ImageFormat ThumbImageFormat => ImageFormat.Png;
    }
}
