using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SharedLib
{
    public class WowProcess
    {
        private Process _warcraftProcess;
        public Process WarcraftProcess
        {
            get
            {
                if (this._warcraftProcess == null)
                {
                    var process = Get();
                    if (process == null)
                    {
                        throw new ArgumentOutOfRangeException("Unable to find the Wow process");
                    }
                    this._warcraftProcess = process;
                }

                return this._warcraftProcess;
            }
        }

        public WowProcess()
        {
            var process = Get();
            if (process == null)
            {
                throw new ArgumentOutOfRangeException("Unable to find the Wow process");
            }

            this._warcraftProcess = process;
        }

        //Get the wow-process, if success returns the process else null
        public static Process? Get(string name = "")
        {
            var names = string.IsNullOrEmpty(name) ? new List<string> { "Wow", "WowClassic", "WowClassicT", "Wow-64", "WowClassicB" } : new List<string> { name };

            var processList = Process.GetProcesses();
            foreach (var p in processList)
            {
                if (names.Contains(p.ProcessName))
                {
                    return p;
                }
            }

            //logger.Error($"Failed to find the wow process, tried: {string.Join(", ", names)}");

            return null;
        }
    }
}