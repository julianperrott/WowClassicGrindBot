namespace Core
{
    public class StartupConfigAddonData
    {
        public const string Position = "AddonData";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public StartupConfigAddonData()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {

        }

        public StartupConfigAddonData(string Mode, int myPort, string connectTo, int connectPort)
        {
            this.Mode = Mode;
            this.myPort = myPort;
            this.connectTo = connectTo;
            this.connectPort = connectPort;
        }

        public string Mode { get; set; }
        public int myPort { get; set; }

        public string connectTo { get; set; }

        public int connectPort { get; set; }
    }
}