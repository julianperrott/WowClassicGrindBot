using System;

namespace Game
{
    public class ScreenChangeEventArgs : EventArgs
    {
        public string Screenshot { get; }
        public DateTime EventTime { get; }

        public ScreenChangeEventArgs(string screenshot)
        {
            this.Screenshot = screenshot;
            this.EventTime = DateTime.UtcNow;
        }
    }
}