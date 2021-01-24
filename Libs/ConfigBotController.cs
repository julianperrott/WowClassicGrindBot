using Libs.GOAP;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Libs
{
    public class ConfigBotController : IBotController
    {
        public AddonReader AddonReader { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Thread? screenshotThread { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Thread addonThread { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Thread? botThread { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public GoalFactory ActionFactory { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public GoapAgent? GoapAgent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public RouteInfo? RouteInfo { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public WowScreen WowScreen { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string? SelectedClassProfile { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string? SelectedPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsBotActive => throw new NotImplementedException();

        public IImageProvider? MinimapImageFinder { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ClassConfiguration? ClassConfig { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event EventHandler? ProfileLoaded;

        public void Shutdown()
        {
            throw new NotImplementedException();
        }

        public void StopBot()
        {
            throw new NotImplementedException();
        }

        public void ToggleBotStatus()
        {
            throw new NotImplementedException();
        }

        public void LoadClassProfile(string classProfileFileName)
        {
            ProfileLoaded?.Invoke(this, EventArgs.Empty);
            throw new NotImplementedException();
        }

        public List<string> ClassFileList()
        {
            throw new NotImplementedException();
        }

        public List<string> PathFileList()
        {
            throw new NotImplementedException();
        }

        public void LoadPathProfile(string pathProfileFileName)
        {
            ProfileLoaded?.Invoke(this, EventArgs.Empty);
            throw new NotImplementedException();
        }
    }
}