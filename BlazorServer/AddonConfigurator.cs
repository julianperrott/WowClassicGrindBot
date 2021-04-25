using System.IO;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using Libs;
using System.Diagnostics;
using System.Management;
using System.Linq;
using System;
using System.Collections.Generic;

namespace BlazorServer
{
    public class AddonConfigurator
    {
        private readonly ILogger logger;
        private readonly AddonConfig addonConfig;
        private readonly WowProcess wowProcess;

        private const string DefaultAddonName = "DataToColor";
        private const string AddonSourcePath = @".\Addons\";

        private string AddonBasePath => Path.Join(addonConfig.InstallPath, "Interface", "AddOns");

        private string DefaultAddonPath => Path.Join(AddonBasePath, DefaultAddonName);
        private string FinalAddonPath => Path.Join(AddonBasePath, addonConfig.Title);

        public AddonConfigurator(ILogger logger, AddonConfig addonConfig)
        {
            this.logger = logger;
            this.addonConfig = addonConfig;

            this.wowProcess = new WowProcess(logger);
        }

        public bool Validate()
        {
            if(!Directory.Exists(addonConfig.InstallPath))
            {
                logger.LogError($"AddonConfigurator.InstallPath - error - does not exists: '{addonConfig.InstallPath}'");
                return false;
            }
            else
            {
                logger.LogInformation($"AddonConfigurator.InstallPath - correct: '{addonConfig.InstallPath}'");
                if(!Directory.Exists(Path.Join(addonConfig.InstallPath, "Interface", "AddOns")))
                {
                    logger.LogError($"AddonConfigurator.InstallPath - error - unable to locate Interface\\Addons folder: '{addonConfig.InstallPath}'");
                    return false;
                }
                else
                {
                    logger.LogInformation($"AddonConfigurator.InstallPath - correct - Interface\\Addons : '{addonConfig.InstallPath}'");
                }
            }

            if(!string.IsNullOrEmpty(addonConfig.Title))
            {
                // this will appear in the lua code so
                // special character not allowed
                // also numbers not allowed
                addonConfig.Title = new string(addonConfig.Title.Where(char.IsLetter).ToArray());
                addonConfig.Title = addonConfig.Title.Trim();
                addonConfig.Title = addonConfig.Title.Replace(" ", "");
            }
            else
            {
                logger.LogError($"AddonConfigurator.Title - error - cannot be empty: '{addonConfig.Title}'");
                return false;
            }

            if (string.IsNullOrEmpty(addonConfig.Author))
            {
                logger.LogError($"AddonConfigurator.Author - error - cannot be empty: '{addonConfig.Author}'");
                return false;
            }

            return true;
        }

        public void Install()
        {
            if(Validate())
            {
                DeleteExisting();

                CopyAllAddons();
                RenameAddon();
                bool success = MakeUnique();

                logger.LogInformation($"AddonConfigurator.Install {(success ? "successful" : "Failed")}");
            }
        }

        private void DeleteExisting()
        {
            if (Directory.Exists(DefaultAddonPath))
            {
                logger.LogInformation("AddonConfigurator.DeleteExisting DefaultAddonPath Exists");
                Directory.Delete(DefaultAddonPath, true);
            }

            if (!string.IsNullOrEmpty(addonConfig.Title) && Directory.Exists(FinalAddonPath))
            {
                logger.LogInformation("AddonConfigurator.DeleteExisting FinalAddonPath Exists");
                Directory.Delete(FinalAddonPath, true);
            }
        }

        private void CopyAllAddons()
        {
            try
            {
                CopyFolder("");
                logger.LogInformation("AddonConfigurator.CopyFiles - success");
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);

                // This only should be happen when running from IDE
                CopyFolder(".");
                logger.LogInformation("AddonConfigurator.CopyFiles - success");
            }
        }

        private void CopyFolder(string parentFolder)
        {
            DirectoryCopy(Path.Join(parentFolder + AddonSourcePath), AddonBasePath, true);
        }

        private void RenameAddon()
        {
            Directory.Move(Path.Join(AddonBasePath, DefaultAddonName), FinalAddonPath);
        }

        private bool MakeUnique()
        {
            BulkRename(FinalAddonPath, DefaultAddonName, addonConfig.Title);
            EditToc();
            EditMainLua();

            return true;
        }

        private static void BulkRename(string fPath, string match, string fNewName)
        {
            string fExt;
            string fFromName;
            string fToName;

            //copy all files from fPath to files array
            FileInfo[] files = new DirectoryInfo(fPath).GetFiles();
            //loop through all files
            foreach (var f in files)
            {
                //get the filename without the extension
                fFromName = Path.GetFileNameWithoutExtension(f.Name);

                if (!fFromName.Contains(match))
                    continue;

                //get the file extension
                fExt = Path.GetExtension(f.Name);

                //set fFromName to the path + name of the existing file
                fFromName = Path.Join(fPath, f.Name); //string.Format("{0}{1}", fPath, f.Name);
                //set the fToName as path + new name + _i + file extension
                fToName = Path.Join(fPath, fNewName) + fExt; //string.Format("{0}{1}{2}", fPath, fNewName, fExt);

                //rename the file by moving to the same place and renaming
                File.Move(fFromName, fToName);
            }
        }

        private void EditToc()
        {
            string tocPath = Path.Join(FinalAddonPath, addonConfig.Title + ".toc");
            string text = File.ReadAllText(tocPath);
            text = text.Replace(DefaultAddonName, addonConfig.Title);

            // edit author
            text = text.Replace("## Author: FreeHongKongMMO", "## Author: " + addonConfig.Author);

            File.WriteAllText(tocPath, text);
        }

        private void EditMainLua()
        {
            string tocPath = Path.Join(FinalAddonPath, addonConfig.Title + ".lua");
            string text = File.ReadAllText(tocPath);
            text = text.Replace(DefaultAddonName, addonConfig.Title);

            //edit slash command
            addonConfig.Command = addonConfig.Title.Trim().ToLower();
            text = text.Replace("dc", addonConfig.Command);
            text = text.Replace("DC", addonConfig.Command);

            File.WriteAllText(tocPath, text);
        }

        public void Delete()
        {
            DeleteExisting();
            AddonConfig.Delete();
        }

        public void Save()
        {
            if (Validate())
            {
                addonConfig.Save();
            }
        }


        #region InstallPath

        public void FindPathByExecutable()
        {
            if (wowProcess.WarcraftProcess != null)
            {
                addonConfig.InstallPath = GetRunningProcessFullPath();
                if (!string.IsNullOrEmpty(addonConfig.InstallPath))
                {
                    logger.LogInformation($"AddonConfigurator.InstallPath - found running instance: '{addonConfig.InstallPath}'");
                    return;
                }
            }

            logger.LogError($"AddonConfigurator.InstallPath - game not running");
        }

        private string GetRunningProcessFullPath()
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
                    if(item.Process.Id == wowProcess.WarcraftProcess.Id)
                    {
                        var path = Path.GetDirectoryName(item.Path);
                        return string.IsNullOrEmpty(path) ? string.Empty : path;
                    }
                }
            }

            return string.Empty;
        }

        #endregion

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

    }
}