using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Libs
{
    public class PressKeyThread
    {
        WowProcess wowProcess;
        ConsoleKey key;
        Thread thread;

        public PressKeyThread(WowProcess wowProcess, ConsoleKey key)
        {
            this.wowProcess = wowProcess;
            this.key = key;
            this.thread = new Thread(DoWork);
            this.thread.Start();
        }

        public void DoWork()
        {
            wowProcess.SetKeyState(this.key, true);
            Thread.Sleep(420);
            wowProcess.SetKeyState(this.key, false);
        }
    }
}
