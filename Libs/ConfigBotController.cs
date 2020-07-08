using Libs.GOAP;
using System;
using System.Threading;

namespace Libs
{
    public class ConfigBotController : IBotController
    {
        public AddonReader AddonReader { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Thread? screenshotThread { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Thread addonThread { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Thread? botThread { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ActionFactory ActionFactory { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public GoapAgent? GoapAgent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public RouteInfo? RouteInfo { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public WowScreen WowScreen { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsBotActive => throw new NotImplementedException();

        public IImageProvider? MinimapImageFinder { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ClassConfiguration? ClassConfig { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void StopBot()
        {
            throw new NotImplementedException();
        }

        public void ToggleBotStatus()
        {
            throw new NotImplementedException();
        }
    }
}