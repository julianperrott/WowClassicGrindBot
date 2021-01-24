using Libs.GOAP;
using System.Collections.Generic;
using System.Threading;

namespace Libs
{
    public interface IBotController
    {
        AddonReader AddonReader { get; set; }
        Thread? screenshotThread { get; set; }
        Thread addonThread { get; set; }
        Thread? botThread { get; set; }
        //GoalFactory ActionFactory { get; set; }
        GoapAgent? GoapAgent { get; set; }
        RouteInfo? RouteInfo { get; set; }
        WowScreen WowScreen { get; set; }
        ClassConfiguration? ClassConfig { get; set; }
        IImageProvider? MinimapImageFinder { get; set; }

        string? SelectedClassProfile { get; set; }
        string? SelectedPath { get; set; }

        event System.EventHandler? ProfileLoaded;

        void ToggleBotStatus();
        void StopBot();

        void Shutdown();

        bool IsBotActive { get; }

        List<string> ClassFileList();

        void LoadClassProfile(string classProfileFileName);

        List<string> PathFileList();

        void LoadPathProfile(string pathProfileFileName);
    }
}