using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using EmbyMyShowsMe.Configuration;
using EmbyMyShowsMe.OAuth;

using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace EmbyMyShowsMe
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage, IHasTranslations
    {
        public static Plugin Instance { get; private set; }

        public const string PluginGuidString = "ef35f6b1-7fe6-44ca-1234-232089fb9bc7";

        public override string Name => "MyShows.Me";
        public override string Description => "Scrobble your watched shows with MyShows.Me";
        public override Guid Id => Guid.Parse(PluginGuidString);

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

        public TranslationInfo[] GetTranslations()
        {
            var basePath = GetType().Namespace + ".i18n.Configuration.";
            return GetType().Assembly.GetManifestResourceNames()
                .Where(i => i.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                .Select(i =>
                    new TranslationInfo
                    {
                        Locale = Path.GetFileNameWithoutExtension(i.Substring(basePath.Length)),
                        EmbeddedResourcePath = i
                    })
                .ToArray();
        }
    }
}
