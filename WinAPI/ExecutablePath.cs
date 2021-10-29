using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;

namespace WinAPI
{
    public static class ExecutablePath
    {
        public static string Get(Process process)
        {
            var wmiQueryString = "SELECT ProcessId, ExecutablePath, CommandLine FROM Win32_Process";
            using (var searcher = new ManagementObjectSearcher(wmiQueryString))
            using (var results = searcher.Get())
            {
                var query = from p in Process.GetProcesses()
                            join mo in results.Cast<ManagementObject>()
                            on p.Id equals (int)(uint)mo["ProcessId"]
                            select new
                            {
                                Process = p,
                                Path = (string)mo["ExecutablePath"],
                                CommandLine = (string)mo["CommandLine"],
                            };

                foreach (var item in query)
                {
                    if (item.Process.Id == process.Id)
                    {
                        var path = Path.GetDirectoryName(item.Path);
                        return string.IsNullOrEmpty(path) ? string.Empty : path;
                    }
                }
            }

            return string.Empty;
        }

    }
}
