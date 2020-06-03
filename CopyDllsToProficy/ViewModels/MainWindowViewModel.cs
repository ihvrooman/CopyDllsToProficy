using AppStandards;
using AppStandards.Helpers;
using AppStandards.Logging;
using AppStandards.MVVM;
using CopyDllsToProficy.Helpers;
using CopyDllsToProficy.Models;
using CopyDllsToProficy.Properties;
using CopyDllsToProficy.Views;
using MahApps.Metro;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CopyDllsToProficy.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        #region Fields
        /// <summary>
        /// The field containing the text to be displayed in the status bar.
        /// </summary>
        private string _statusText = "";
        /// <summary>
        /// A timer used for autosaving the application and user settings.
        /// </summary>
        private System.Timers.Timer _autoSaveTimer = new System.Timers.Timer(2 * 60 * 1000);
        private System.Timers.Timer _checkServerAndFileStatusTimer = new System.Timers.Timer(100);
        private System.Timers.Timer _restartCheckServerAndFileStatusTimer = new System.Timers.Timer();
        private volatile bool _isFileValid;
        private volatile bool _isServerValid;
        private volatile bool _checkingServerAndFileStatus;
        private volatile bool _delayCheckServerAndFileStatus;
        private volatile bool _progressRingIsActive;
        private volatile bool _initialized;
        /// <summary>
        /// The field containing the command the command for selecting the source folder. 
        /// </summary>
        private ICommand _selectSourceFolderCommand;
        /// <summary>
        /// The field containing the command for copying the file to the server.
        /// </summary>
        private ICommand _copyFileCommand;
        /// <summary>
        /// The field containing the command for showing the About dialog.
        /// </summary>
        private ICommand _aboutCommand;
        /// <summary>
        /// The field containing the folder which contains the Visual Studio solutions.
        /// </summary>
        private DirectoryInfo _sourceFolder;
        /// <summary>
        /// The field containing the folder which contains the Visual Studio solution that the user selected.
        /// </summary>
        private DirectoryInfo _solutionFolder;
        /// <summary>
        /// The field containing the folder which contains the Visual Studio project that the user selected.
        /// </summary>
        private DirectoryInfo _projectFolder;
        /// <summary>
        /// The field containing the file that the user selected to copy.
        /// </summary>
        private FileInfo _selectedFile;
        /// <summary>
        /// The field containing the name of the server to copy the file to.
        /// </summary>
        private string _serverName;
        private bool _settingsFlyoutIsOpen;
        private ICommand _toggleSettingsFlyoutCommand;
        private IDialogCoordinator _dialogCoordinator;
        private Status _status;
        private bool _copying;
        private string _windowTitle = AppInfo.BaseAppInfo.UserFriendlyAppName;
        #endregion

        #region Properties
        public ICommand SelectSourceFolderCommand
        {
            get
            {
                if (_selectSourceFolderCommand == null)
                {
                    _selectSourceFolderCommand = new RelayCommand(ShowSelectSourceFolderDialog);
                }
                return _selectSourceFolderCommand;
            }
        }
        public ICommand CopyFileCommand
        {
            get
            {
                if (_copyFileCommand == null)
                {
                    _copyFileCommand = new RelayCommand(CopyFile, CanCopyFile);
                }
                return _copyFileCommand;
            }
        }
        public ICommand AboutCommand
        {
            get
            {
                if (_aboutCommand == null)
                {
                    _aboutCommand = new RelayCommand(ShowAboutDialog);
                }
                return _aboutCommand;
            }
        }
        /// <summary>
        /// The folder that contains all of the VS solutions.
        /// </summary>
        public DirectoryInfo SourceFolder
        {
            get { return _sourceFolder; }
            set
            {
                _sourceFolder = value;
                RaisePropertyChangedEvent();
                Settings.Default.SourceFolderPath = value.FullName;
                SolutionFolder = null;
                SolutionFolders.Clear();
                if (SourceFolder != null && Settings.Default.ServerShareMessageShown)
                {
                    LoadSolutionFolders();
                }
            }
        }
        public ObservableCollection<DirectoryInfo> SolutionFolders { get; private set; } = new ObservableCollection<DirectoryInfo>();
        /// <summary>
        /// The folder containing the VS solution that the user is interested in copying.
        /// </summary>
        public DirectoryInfo SolutionFolder
        {
            get { return _solutionFolder; }
            set
            {
                _solutionFolder = value;
                RaisePropertyChangedEvent();
                ProjectFolder = null;
                ProjectFolders.Clear();
                if (SolutionFolder != null)
                {
                    LoadProjectFolders();
                }
            }
        }
        public ObservableCollection<DirectoryInfo> ProjectFolders { get; private set; } = new ObservableCollection<DirectoryInfo>();
        /// <summary>
        /// The folder containing the project that the user is interested in copying.
        /// </summary>
        public DirectoryInfo ProjectFolder
        {
            get { return _projectFolder; }
            set
            {
                _projectFolder = value;
                RaisePropertyChangedEvent();
                SelectedFile = null;
                AvailableFiles.Clear();
                if (ProjectFolder != null)
                {
                    LoadAvailableFiles();
                }
            }
        }
        public ObservableCollection<FileInfo> AvailableFiles { get; private set; } = new ObservableCollection<FileInfo>();
        public string StatusText { get { return _statusText; } set { _statusText = value; RaisePropertyChangedEvent(); } }
        /// <summary>
        /// The file that the user is interested in copying.
        /// </summary>
        public FileInfo SelectedFile
        {
            get { return _selectedFile; }
            set
            {
                _selectedFile = value;
                RaisePropertyChangedEvent();
                if (SelectedFile?.Name == null)
                {
                    WindowTitle = AppInfo.BaseAppInfo.UserFriendlyAppName;
                }
                else
                {
                    WindowTitle = $"{SelectedFile.Name} - {AppInfo.BaseAppInfo.UserFriendlyAppName}";
                }
            }
        }
        public ObservableCollection<string> Servers { get; private set; } = new ObservableCollection<string>();
        /// <summary>
        /// The name of the server that the user is interested in copying to.
        /// </summary>
        public string ServerName { get { return _serverName; } set { _serverName = value; RaisePropertyChangedEvent(); } }
        private DirectoryInfo _serverTargetFolder
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ServerName))
                {
                    return new DirectoryInfo("ERROR -ServerNameNotSet");
                }
                else if (ServerName.Contains("Error") || ServerName.Contains("error"))
                {
                    return new DirectoryInfo("ERROR-ServerNameError");
                }
                return new DirectoryInfo("\\\\" + ServerName + "\\" + Settings.Default.ProficyDllFolderPath);
            }
        }
        /// <summary>
        /// Indicates whether or not the <see cref="MainWindowViewModel"/> is being run by a unit test.
        /// </summary>
        public bool Testing { get; set; }
        public bool IsFileValid { get { return _isFileValid; } set { _isFileValid = value; RelayCommand.ReEvaluateCanExecute(); } }
        public bool IsServerValid { get { return _isServerValid; } set { _isServerValid = value; RelayCommand.ReEvaluateCanExecute(); } }
        public bool SettingsFlyoutIsOpen { get { return _settingsFlyoutIsOpen; } set { _settingsFlyoutIsOpen = value; RaisePropertyChangedEvent(); } }
        public ICommand ToggleSettingsFlyoutCommand
        {
            get
            {
                if (_toggleSettingsFlyoutCommand == null)
                {
                    _toggleSettingsFlyoutCommand = new RelayCommand(ToggleSettingsFlyout);
                }
                return _toggleSettingsFlyoutCommand;
            }
        }
        public Status Status { get { return _status; } set { _status = value; RaisePropertyChangedEvent(); } }
        public bool ProgressRingIsActive
        {
            get { return _progressRingIsActive; }
            private set
            {
                _progressRingIsActive = value;
                if (!ProgressRingIsActive)
                {
                    Task.Run(() =>
                    {
                        Task.Delay(500).Wait();
                        RaisePropertyChangedEvent();
                    });
                }
            }
        }
        public int SelectedSolutionIndex
        {
            get { return Settings.Default.SelectedSolutionIndex; }
            set
            {
                Settings.Default.SelectedSolutionIndex = value;
            }
        }
        public int SelectedProjectIndex
        {
            get { return Settings.Default.SelectedProjectIndex; }
            set
            {
                Settings.Default.SelectedProjectIndex = value;
            }
        }
        public int SelectedFileIndex
        {
            get { return Settings.Default.SelectedFileIndex; }
            set
            {
                Settings.Default.SelectedFileIndex = value;
            }
        }
        public int SelectedServerIndex
        {
            get { return Settings.Default.SelectedServerIndex; }
            set
            {
                Settings.Default.SelectedServerIndex = value;
            }
        }
        public bool Copying { get { return _copying; }set { _copying = value; RaisePropertyChangedEvent(); } }
        public string WindowTitle { get { return _windowTitle; } set { _windowTitle = value; RaisePropertyChangedEvent(); } }
        #endregion

        #region Constructor
        public MainWindowViewModel(IDialogCoordinator dialogCoordinator)
        {
            _dialogCoordinator = dialogCoordinator;
            _autoSaveTimer.Elapsed += AutoSaveTimer_Tick;
            _autoSaveTimer.AutoReset = true;
            _autoSaveTimer.Start();
            _checkServerAndFileStatusTimer.Elapsed += CheckServerAndFileStatusTimer_Elapsed;
            _checkServerAndFileStatusTimer.AutoReset = true;
            _checkServerAndFileStatusTimer.Start();
            _restartCheckServerAndFileStatusTimer.Elapsed += RestartCheckServerAndFileStatusTimer_Elapsed;
            MigrateServerSettings();
            SourceFolder = new DirectoryInfo(Settings.Default.SourceFolderPath);
            LoadServers();
            CheckServerAndFileStatus(true);
            _initialized = true;
        }
        #endregion

        #region Private methods
        private void AutoSaveTimer_Tick(object sender, EventArgs e)
        {
            SaveApplicationSettings();
        }

        private void CheckServerAndFileStatusTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            CheckServerAndFileStatus();
        }

        private void DelayCheckServerAndFileStatusOperation(int timeoutInMilliseconds = 10000)
        {
            _delayCheckServerAndFileStatus = true;
            _checkServerAndFileStatusTimer.Stop();
            _restartCheckServerAndFileStatusTimer.Stop();
            _restartCheckServerAndFileStatusTimer.Interval = timeoutInMilliseconds;
            _restartCheckServerAndFileStatusTimer.Start();
        }

        private void RestartCheckServerAndFileStatusTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _delayCheckServerAndFileStatus = false;
            _checkServerAndFileStatusTimer.Start();
        }

        private async void CopyFile()
        {
            Copying = true;
            ProgressRingIsActive = true;
            DelayCheckServerAndFileStatusOperation();
            SetStatus("Preparing to copy file");

            var targetFilePath = _serverTargetFolder.FullName + "\\" + SelectedFile.Name;
            var targetFile = new FileInfo(targetFilePath);
            var targetFileExists = AppInfo.FileExists(targetFile);
            var confirmCopyFileMessage = $"Are you sure that you want to copy \"{SelectedFile.Name}\" to server \"{ServerName}\"?";
            var copyFileLogMessage = $"Copying file from path \"{SelectedFile.FullName}\" to path \"{targetFilePath}\".";

            if (targetFileExists)
            {
                var overwriteMessage = "The existing file will be archived in the 'Old' folder.";
                confirmCopyFileMessage += Environment.NewLine + Environment.NewLine + overwriteMessage;
            }

            if (Testing || await _dialogCoordinator.ShowMessageAsync(this, "Confirm Copy File", confirmCopyFileMessage, MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
            {
                try
                {
                    var cancelCopyFile = false;
                    if (targetFileExists)
                    {
                        //If target file already exists, archive it in the 'Old' folder
                        DelayCheckServerAndFileStatusOperation();
                        SetStatus("Locating archive folder");
                        AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Locating archive folder.", LogMessageType.Verbose);

                        var archiveFolder = new DirectoryInfo(_serverTargetFolder + "\\Old");
                        var archiveFolderExists = false;
                        foreach (var folder in _serverTargetFolder.GetDirectories())
                        {
                            if (folder.Name.Contains("Old") || folder.Name.Contains("old"))
                            {
                                archiveFolder = folder;
                                archiveFolderExists = true;
                                AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Found archive folder with path \"{archiveFolder.FullName}\".", LogMessageType.Verbose);
                                break;
                            }
                        }

                        var archiveFailureUserMessage = $"{AppInfo.BaseAppInfo.AppName} could not archive the existing file. Copying the new file will overwrite the existing file.{Environment.NewLine}Would you like to continue?";
                        if (!archiveFolderExists)
                        {
                            try
                            {
                                AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Creating archive folder with path \"{archiveFolder.FullName}\".");
                                await Task.Run(() => { archiveFolder.Create(); });
                                archiveFolderExists = true;
                                AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Successfully created archive folder with path \"{archiveFolder.FullName}\".");
                            }
                            catch (Exception ex)
                            {
                                DelayCheckServerAndFileStatusOperation();
                                SetStatus("Could not archive existing file", Status.Warn);

                                AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Could not create archive folder with path \"{archiveFolder.FullName}\". Archive file operation cancelled. Error message: {ex.Message}", LogMessageType.Warning);
                                if (await _dialogCoordinator.ShowMessageAsync(this, "File Archive Failed", archiveFailureUserMessage, MessageDialogStyle.AffirmativeAndNegative) != MessageDialogResult.Affirmative)
                                {
                                    cancelCopyFile = true;
                                    AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"User cancelled copy file operation.");
                                }
                            }
                        }

                        if (archiveFolderExists)
                        {
                            DelayCheckServerAndFileStatusOperation();
                            SetStatus("Archiving existing file");

                            try
                            {
                                AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Archiving file with path \"{targetFile.FullName}\".");
                                await Task.Run(() => { targetFile.CopyTo(archiveFolder.FullName + $"\\{targetFile.Name}", true); });
                                AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Successfully archived file with path \"{targetFile.FullName}\".");
                            }
                            catch (Exception ex)
                            {
                                DelayCheckServerAndFileStatusOperation();
                                SetStatus("Could not archive existing file", Status.Warn);

                                AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Could archive file with path \"{targetFile.FullName}\". Error message: {ex.Message}", LogMessageType.Warning);
                                if (await _dialogCoordinator.ShowMessageAsync(this, "File Archive Failed", archiveFailureUserMessage, MessageDialogStyle.AffirmativeAndNegative) != MessageDialogResult.Affirmative)
                                {
                                    cancelCopyFile = true;
                                    AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"User cancelled copy file operation.");
                                }
                            }
                        }
                    }

                    if (!cancelCopyFile)
                    {
                        DelayCheckServerAndFileStatusOperation();
                        SetStatus("Copying file");
                        AppInfo.BaseAppInfo.Log.QueueLogMessageAsync(copyFileLogMessage);
                        await Task.Run(() => { SelectedFile.CopyTo(targetFilePath, true); });                        
                        AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Successfully copied file from path \"{SelectedFile.FullName}\" to path \"{targetFilePath}\".");
                        DelayCheckServerAndFileStatusOperation();
                        SetStatus("File copied successfully", Status.Success);
                    }
                }
                catch (Exception ex)
                {
                    DelayCheckServerAndFileStatusOperation();
                    SetStatus("Failed to copy file", Status.Fail);
                    var userMessage = $"Could not copy file.";
                    if (!Testing)
                    {
                        AppInfo.BaseAppInfo.Log.QueueLogMessageAsync(userMessage + $" Error message: {ex.Message}", LogMessageType.Error);
                        await _dialogCoordinator.ShowMessageAsync(this, "Copy Failed", userMessage);
                    }
                }
            }
            else
            {
                SetStatus("Ready");
            }
            ProgressRingIsActive = false;
            Copying = false;
        }

        private void SetStatus(string statusText, Status status = Status.Ready)
        {
            StatusText = statusText;
            Status = status;
        }

        private void ResetStatus()
        {
            SetStatus(string.Empty);
        }

        private void CheckServerAndFileStatus(bool forceSetStatus = false)
        {
            if (_checkingServerAndFileStatus || _delayCheckServerAndFileStatus)
            {
                return;
            }

            ProgressRingIsActive = true;
            _checkingServerAndFileStatus = true;
            var isServerValid = AppInfo.FolderExists(_serverTargetFolder, false);
            var isFileValid = AppInfo.FileExists(SelectedFile, false);
            var didStatusChange = isServerValid != IsServerValid || isFileValid != IsFileValid;
            ProgressRingIsActive = false;

            if (_delayCheckServerAndFileStatus)
            {
                //Check if status check is to be delayed after checking file and server status becuase those operations could have taken a lot of time.
                _checkingServerAndFileStatus = false;
                return;
            }

            if (!isServerValid && !isFileValid && (forceSetStatus || didStatusChange))
            {
                SetStatus("Server and file are invalid", Status.Warn);
            }
            else if (!isServerValid && (forceSetStatus || didStatusChange))
            {
                SetStatus("Server is invalid", Status.Warn);
            }
            else if (!isFileValid && (forceSetStatus || didStatusChange))
            {
                SetStatus("File is invalid", Status.Warn);
            }
            else if (isServerValid && isFileValid)
            {
                SetStatus("Ready");
            }
            IsServerValid = isServerValid;
            IsFileValid = isFileValid;
            _checkingServerAndFileStatus = false;
        }

        private void SaveApplicationSettings()
        {
            AppInfo.BaseAppInfo.Log.QueueLogMessageAsync("Saving main application/user settings.", LogMessageType.Verbose);
            Settings.Default.Save();
        }

        private void ToggleSettingsFlyout()
        {
            SettingsFlyoutIsOpen = !SettingsFlyoutIsOpen;
        }

        private async void LoadSolutionFolders()
        {
            foreach (var solutionFolder in SourceFolder.GetDirectories())
            {
                SolutionFolders.Add(solutionFolder);
            }

            if (SolutionFolders.Count < 1 && _initialized)
            {
                await _dialogCoordinator.ShowMessageAsync(this, "No Folders Found", $"The selected source folder with path \"{SourceFolder?.FullName}\" contains no subfolders.");
            }
            else if (SolutionFolders.Count == 1)
            {
                SolutionFolder = SolutionFolders[0];
            }
        }

        private async void LoadProjectFolders()
        {
            foreach (var projectFolder in SolutionFolder.GetDirectories())
            {
                var projectFolderName = projectFolder.Name;
                if (!projectFolderName.Contains("packages") && !projectFolderName.Contains(".vs"))
                {
                    ProjectFolders.Add(projectFolder);
                }
            }

            if (ProjectFolders.Count < 1 && _initialized)
            {
                await _dialogCoordinator.ShowMessageAsync(this, "No Folders Found", $"The selected Visual Studio solution folder with path \"{SolutionFolder?.FullName}\" contains no subfolders.");
            }
            else if (ProjectFolders.Count == 1)
            {
                ProjectFolder = ProjectFolders[0];
            }
        }

        private async void LoadAvailableFiles()
        {
            var binFolder = new DirectoryInfo(ProjectFolder.FullName + "\\bin\\Debug");
            if (!AppInfo.FolderExists(binFolder) && _initialized)
            {
                var userMessagePart1 = $"Cannot find bin folder.";
                var userMessagePart2 = $"Expected path: \"{binFolder.FullName}\"";
                await _dialogCoordinator.ShowMessageAsync(this, "Cannot Find Folder", userMessagePart1 + Environment.NewLine + userMessagePart2);
                return;
            }

            foreach (var file in binFolder.GetFiles())
            {
                var fileName = file.Name;
                if (fileName.Contains(".dll"))
                {
                    AvailableFiles.Add(file);
                }
            }

            if (AvailableFiles.Count < 1 && _initialized)
            {
                await _dialogCoordinator.ShowMessageAsync(this, "No Files Found", $"The debug bin folder with path \"{binFolder?.FullName}\" contains no files.");
            }
            else if (AvailableFiles.Count == 1)
            {
                SelectedFile = AvailableFiles[0];
            }
        }

        private void LoadServers()
        {
            Servers.Clear();
            foreach (var server in ServerSettings.Default.Servers)
            {
                Servers.Add(server);
            }
        }

        private bool CanCopyFile()
        {
            return IsServerValid && IsFileValid;
        }

        private void ShowSelectSourceFolderDialog()
        {
            var dialog = new FolderBrowserDialog() { SelectedPath = Settings.Default.SourceFolderPath };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SourceFolder = new DirectoryInfo(dialog.SelectedPath);

            }
        }

        private async void ShowAboutDialog()
        {
            await _dialogCoordinator.ShowMessageAsync(this, $"About {AppInfo.BaseAppInfo.AppName}", AppInfo.AboutInformation);
        }

        private void MigrateServerSettings()
        {
            if (ServerSettings.Default.Servers == null)
            {
                ServerSettings.Default.Servers = new StringCollection();
            }

            if (Settings.Default.MigrateServerSettings)
            {
                AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Migrating user's servers from Settings.Servers to ServerSettings.Servers.", LogMessageType.Verbose);
                var numberMigrated = 0;
                foreach (var server in Settings.Default.Servers)
                {
                    if (!ServerSettings.Default.Servers.Contains(server))
                    {
                        ServerSettings.Default.Servers.Add(server.ToUpper());
                        ++numberMigrated;
                    }
                }
                Settings.Default.MigrateServerSettings = false;
                AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Saving ServerSettings.", LogMessageType.Verbose);
                ServerSettings.Default.Save();
                AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Server settings migration complete. Migrated {numberMigrated} servers.", LogMessageType.Verbose);
            }
        }
        #endregion

        #region Internal methods
        /// <summary>
        /// Converts the server names stored in the Settings file to uppercase.
        /// </summary>
        internal void ConvertServerNamesToUpperCase()
        {
            if (Settings.Default.ConvertServerNamesToUpperCase && Settings.Default.Servers.Count > 0)
            {
                try
                {
                    AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Converting server names to uppercase.");

                    var newServerNames = new StringCollection();
                    string originalServerNamesString = string.Empty;
                    string newServerNamesString = string.Empty;

                    foreach (var originalServerName in Settings.Default.Servers)
                    {
                        originalServerNamesString += $"-{originalServerName}";
                        var newServerName = originalServerName.ToUpper();
                        newServerNamesString += $"-{newServerName}";
                        newServerNames.Add(newServerName);
                    }

                    Settings.Default.Servers.Clear();
                    Settings.Default.Servers = newServerNames;
                    Settings.Default.ConvertServerNamesToUpperCase = false;
                    Settings.Default.Save();

                    AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Successfully converted server names to uppercase. | Original server names: {originalServerNamesString} | New server names: {newServerNamesString}");
                }
                catch (Exception ex)
                {
                    AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Failed to convert server names to uppercase. Error message: {ex.Message}", AppStandards.Logging.LogMessageType.Error);
                }
            }
        }

        internal async void ShowMessages()
        {
            if (!Settings.Default.ServerShareMessageShown)
            {
                SettingsFlyoutIsOpen = true;
                await _dialogCoordinator.ShowMessageAsync(this, "Setup Information", AppInfo.AddFileShareToAllServersMessage);
                Settings.Default.ServerShareMessageShown = true;
                Settings.Default.Save();
            }
        }

        internal void Dispose()
        {
            _autoSaveTimer.Stop();
            _checkServerAndFileStatusTimer.Stop();
            _restartCheckServerAndFileStatusTimer.Stop();
            SaveApplicationSettings();
        }
        #endregion
    }
}
