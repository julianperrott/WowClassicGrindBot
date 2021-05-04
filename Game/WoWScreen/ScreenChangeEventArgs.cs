using System;

namespace Game
{
    public class ScreenChangeEventArgs : EventArgs
    {
        public String Screenshot { get; }
        public DateTime EventTime { get; }

        public ScreenChangeEventArgs(String screenshot)
        {
            this.Screenshot = screenshot;
            this.EventTime = DateTime.Now;
        }
    }
}