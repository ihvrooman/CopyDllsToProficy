using AppStandards;
using AppStandards.Logging;
using CopyDllsToProficy.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CopyDllsToProficy.Helpers
{
    public static class AppInfo
    {
        private static string _twoNewLines = Environment.NewLine + Environment.NewLine;
        public static BaseAppInfo BaseAppInfo { get; private set; } = new BaseAppInfo();
        public static string Description { get; private set; } = $"An application used to copy .dll files created when developing Proficy displays from development PCs to Proficy servers.{_twoNewLines}Note: The application copies the dlls to the file share \"{Settings.Default.ProficyDllFolderPath}\" which must exist on every Proficy server. The dlls must still be manually loaded into Proficy.";
        public static string Credits { get; private set; } = $"Special thanks to the creators of MahApps.Metro!";
        public static string AboutInformation { get; private set; } = $"Version Number:{Environment.NewLine + BaseAppInfo.VersionNumber + _twoNewLines}Description:{Environment.NewLine + Description + _twoNewLines}Credits:{Environment.NewLine + Credits}";
        public static string AddFileShareToAllServersMessage { get; private set; } = $"Please ensure that each of the Proficy servers found in the server settings have a file share named \"{Settings.Default.ProficyDllFolderPath}\" which points to where all of the .dll files are located.";
        public static string AddFileShareToNewlyAddedServersMessage { get; private set; } = $"Please ensure that this Proficy server has a file share named \"{Settings.Default.ProficyDllFolderPath}\" which points to where it's .dll files are located.";

        /// <summary>
        /// Overrides cursor to wait, checks to see whether or not folder exists, removes cursor override, and returns result of Exists check.
        /// </summary>
        /// <returns></returns>
        public static bool FolderExists(DirectoryInfo folder, bool showWaitCursor = true)
        {
            if (showWaitCursor)
            {
                Mouse.OverrideCursor = Cursors.Wait;
            }
            folder?.Refresh();
            var exists = folder != null && folder.Exists;
            if (showWaitCursor)
            {
                Mouse.OverrideCursor = null;
            }
            return exists;
        }

        /// <summary>
        /// Overrides cursor to wait, checks to see whether or not file exists, removes cursor override, and returns result of Exists check.
        /// </summary>
        /// <returns></returns>
        public static bool FileExists(FileInfo file, bool showWaitCursor = true)
        {
            if (showWaitCursor)
            {
                Mouse.OverrideCursor = Cursors.Wait;
            }
            file?.Refresh();
            var exists = file != null && file.Exists;
            if (showWaitCursor)
            {
                Mouse.OverrideCursor = null;
            }
            return exists;
        }
    }

    public class BaseAppInfo : IAppInfo
    {
        #region Fields
        private Log _log;
        #endregion

        #region Properties
        public string AppName { get; } = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

        public string UserFriendlyAppName { get; private set; }

        public string VersionNumber { get; } = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public Log Log
        {
            get
            {
                if (_log == null)
                {
                    _log = new Log(Settings.Default.OnlineLogFolderPath, Settings.Default.OfflineLogFolderPath, AppName);
                }
                return _log;
            }
        }

        public string Company => "Weyerhaeuser";
        #endregion

        #region Constructor
        public BaseAppInfo()
        {
            Routines.Startup(this);
            ExtractUserFriendlyAppName();
        }
        #endregion

        #region Private methods
        private void ExtractUserFriendlyAppName()
        {
            try
            {
                Log.QueueLogMessageAsync($"Attempting to extract user-friendly app name from assembly app name: \"{AppName}\".", LogMessageType.Verbose);
                var charArray = AppName.ToCharArray();
                var breakIndexes = new List<int>();
                for (int i = 0; i < charArray.Length; i++)
                {
                    var character = charArray[i];
                    if (char.IsUpper(character) && i != 0)
                    {
                        breakIndexes.Add(i);
                    }
                }

                UserFriendlyAppName = AppName;
                if (breakIndexes.Count > 0)
                {
                    for (int i = breakIndexes.Count - 1; i >= 0; i--)
                    {
                        var breakIndex = breakIndexes[i];
                        UserFriendlyAppName = UserFriendlyAppName.Insert(breakIndex, " ");
                    }
                }

                Log.QueueLogMessageAsync($"Successfully extracted user-friendly app name from assembly app name.", LogMessageType.Verbose);
            }
            catch (Exception ex)
            {
                Log.QueueLogMessageAsync($"Failed to extract user-friendly app name from assembly app name: \"{AppName}\". Defaulting to assembly app name. Error message: {ex.Message}", LogMessageType.Warning);
                UserFriendlyAppName = AppName;
            }
        }
        #endregion
    }
}
