using System.IO;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using Game;
using WinAPI;

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

            this.wowProcess = new WowProcess();
        }

        public bool Installed()
        {
            return GetInstalledVersion() != null;
        }

        public bool Validate()
        {
            if (!Directory.Exists(addonConfig.InstallPath))
            {
                logger.LogError($"{GetType().Name}.InstallPath - error - does not exists: '{addonConfig.InstallPath}'");
                return false;
            }
            else
            {
                logger.LogInformation($"{GetType().Name}.InstallPath - correct: '{addonConfig.InstallPath}'");
                if (!Directory.Exists(AddonBasePath))
                {
                    logger.LogError($"{GetType().Name}.InstallPath - error - unable to locate Interface\\Addons folder: '{addonConfig.InstallPath}'");
                    return false;
                }
                else
                {
                    logger.LogInformation($"{GetType().Name}.InstallPath - correct - Interface\\Addons : '{addonConfig.InstallPath}'");
                }
            }

            if (string.IsNullOrEmpty(addonConfig.Author))
            {
                logger.LogError($"{GetType().Name}.Author - error - cannot be empty: '{addonConfig.Author}'");
                return false;
            }

            if (!string.IsNullOrEmpty(addonConfig.Title))
            {
                // this will appear in the lua code so
                // special character not allowed
                // also numbers not allowed
                addonConfig.Title = Regex.Replace(addonConfig.Title, @"[^\u0000-\u007F]+", string.Empty);
                addonConfig.Title = new string(addonConfig.Title.Where(char.IsLetter).ToArray());
                addonConfig.Title = addonConfig.Title.Trim();
                addonConfig.Title = addonConfig.Title.Replace(" ", "");

                if (addonConfig.Title.Length == 0)
                {
                    logger.LogError($"{GetType().Name}.Title - error - use letters only: '{addonConfig.Title}'");
                    return false;
                }
            }
            else
            {
                logger.LogError($"{GetType().Name}.Title - error - cannot be empty: '{addonConfig.Title}'");
                return false;
            }

            return true;
        }

        public void Install()
        {
            try
            {
                DeleteAddon();
                CopyAllAddons();
                RenameAddon();
                MakeUnique();

                logger.LogInformation($"{GetType().Name}.Install - Success");
            }
            catch (Exception e)
            {
                logger.LogInformation($"{GetType().Name}.Install - Failed\n" + e.Message);
            }
        }

        private void DeleteAddon()
        {
            if (Directory.Exists(DefaultAddonPath))
            {
                logger.LogInformation($"{GetType().Name}.DeleteAddon -> Default Addon Exists");
                Directory.Delete(DefaultAddonPath, true);
            }

            if (!string.IsNullOrEmpty(addonConfig.Title) && Directory.Exists(FinalAddonPath))
            {
                logger.LogInformation($"{GetType().Name}.DeleteAddon -> Unique Addon Exists");
                Directory.Delete(FinalAddonPath, true);
            }
        }

        private void CopyAllAddons()
        {
            try
            {
                CopyFolder("");
                logger.LogInformation($"{GetType().Name}.CopyFiles - Success");
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);

                // This only should be happen when running from IDE
                CopyFolder(".");
                logger.LogInformation($"{GetType().Name}.CopyFiles - Success");
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

        private void MakeUnique()
        {
            BulkRename(FinalAddonPath, DefaultAddonName, addonConfig.Title);
            EditToc();
            EditMainLua();
            EditModulesLua();
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
            string mainLuaPath = Path.Join(FinalAddonPath, addonConfig.Title + ".lua");
            string text = File.ReadAllText(mainLuaPath);
            text = text.Replace(DefaultAddonName, addonConfig.Title);

            //edit slash command
            addonConfig.Command = addonConfig.Title.Trim().ToLower();
            text = text.Replace("dc", addonConfig.Command);
            text = text.Replace("DC", addonConfig.Command);

            File.WriteAllText(mainLuaPath, text);
        }

        private void EditModulesLua()
        {
            FileInfo[] files = new DirectoryInfo(FinalAddonPath).GetFiles();
            foreach (var f in files)
            {
                if (f.Extension.Contains("lua"))
                {
                    string path = f.FullName;
                    string text = File.ReadAllText(path);
                    text = text.Replace(DefaultAddonName, addonConfig.Title);

                    File.WriteAllText(path, text);
                }
            }
        }

        public void Delete()
        {
            DeleteAddon();
            AddonConfig.Delete();
        }

        public void Save()
        {
            addonConfig.Save();
        }


        #region InstallPath

        public void FindPathByExecutable()
        {
            if (wowProcess.WarcraftProcess != null)
            {
                addonConfig.InstallPath = ExecutablePath.Get(wowProcess.WarcraftProcess);
                if (!string.IsNullOrEmpty(addonConfig.InstallPath))
                {
                    logger.LogInformation($"{GetType().Name}.InstallPath - found running instance: '{addonConfig.InstallPath}'");
                    return;
                }
            }

            logger.LogError($"{GetType().Name}.InstallPath - game not running");
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
                file.CopyTo(tempPath, true);
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

        public bool UpdateAvailable()
        {
            if (addonConfig.IsDefault())
                return false;

            Version? repo = GetRepoVerion();
            Version? installed = GetInstalledVersion();

            return installed != null && repo != null && repo > installed;
        }

        public Version? GetRepoVerion()
        {
            Version? repo = null;
            try
            {
                repo = GetVersion(Path.Join(AddonSourcePath, DefaultAddonName), DefaultAddonName);

                if (repo == null)
                {
                    string parentFolder = ".";

                    repo = GetVersion(Path.Join(parentFolder + AddonSourcePath, DefaultAddonName), DefaultAddonName);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
            }
            return repo;
        }

        public Version? GetInstalledVersion()
        {
            return GetVersion(FinalAddonPath, addonConfig.Title);
        }

        private static Version? GetVersion(string path, string fileName)
        {
            string tocPath = Path.Join(path, fileName + ".toc");

            if (!File.Exists(tocPath))
                return null;

            string begin = "## Version: ";
            var line = File
                .ReadLines(tocPath)
                .SkipWhile(line => !line.StartsWith(begin))
                .FirstOrDefault();

            string? versionStr = line?.Split(begin)[1];
            if (Version.TryParse(versionStr, out var version))
                return version;

            return null;
        }
    }
}