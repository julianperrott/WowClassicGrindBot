using System;

namespace PatherPath
{
    public class Logger
    {
        public Logger()
        {

        }

        private Action<string> onWrite;

        public Logger(Action<string> action)
        {
            this.onWrite = action;
        }

        public void WriteLine(string message)
        {
            if (onWrite != null)
            {
                onWrite(message);
            }
            System.Diagnostics.Debug.WriteLine(message);
        }

        public void Debug(string message)
        {
            WriteLine(message);
        }
    }
}