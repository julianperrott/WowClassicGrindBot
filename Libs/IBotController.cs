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
        WowInput? WowInput { get; set; }
        ClassConfiguration? ClassConfig { get; set; }
        IImageProvider? MinimapImageFinder { get; set; }

        ActionBarPopulator? ActionBarPopulator { get; set; }

        string SelectedClassFilename { get; set; }
        string? SelectedPathFilename { get; set; }

        event System.EventHandler? ProfileLoaded;
        event System.EventHandler<bool> StatusChanged;

        void ToggleBotStatus();
        void StopBot();

        void Shutdown();

        bool IsBotActive { get; }

        List<string> ClassFileList();

        void LoadClassProfile(string classFilename);

        List<string> PathFileList();

        void LoadPathProfile(string pathFilename);

        void OverrideClassConfig(ClassConfiguration classConfiguration);
    }
}