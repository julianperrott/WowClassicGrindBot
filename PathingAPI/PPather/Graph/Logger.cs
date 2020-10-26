using System;

namespace PatherPath
{
    public class Logger
    {
        private Action<string> onWrite;

        public Logger(Action<string> onWrite)
        {
            this.onWrite = onWrite;
        }

        public void WriteLine(string message)
        {
            onWrite(message);
            System.Diagnostics.Debug.WriteLine(message);
        }

        public void Debug(string message)
        {
            WriteLine(message);
        }
    }
}