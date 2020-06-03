using AppStandards;
using AppStandards.Logging;
using AppStandards.MVVM;
using CopyDllsToProficy.Helpers;
using CopyDllsToProficy.Properties;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CopyDllsToProficy.ViewModels
{
    public class ServerSettingsViewModel : PropertyChangedHelper
    {
        #region Fields
        private int _selectedIndex;
        private ICommand _addServerCommand;
        private ICommand _removeServerCommand;
        private IDialogCoordinator _dialogCoordinator;
        #endregion

        #region Properties
        public ObservableCollection<string> Servers { get; private set; } = new ObservableCollection<string>();
        public int SelectedIndex { get { return _selectedIndex; } set { _selectedIndex = value; RaisePropertyChangedEvent(); } }
        public string SelectedServer
        {
            get
            {
                try
                {
                    return ServerSettings.Default.Servers != null ? ServerSettings.Default.Servers[SelectedIndex] : string.Empty;
                }
                catch (Exception ex)
                {
                    return $"Error-{ex.Message}";
                }
            }
        }
        public ICommand AddServerCommand
        {
            get
            {
                if (_addServerCommand == null)
                {
                    _addServerCommand = new RelayCommand(AddServer);
                }
                return _addServerCommand;
            }
        }
        public ICommand RemoveServerCommand
        {
            get
            {
                if (_removeServerCommand == null)
                {
                    _removeServerCommand = new RelayCommand(RemoveServer, CanRemoveServer);
                }
                return _removeServerCommand;
            }
        }
        #endregion

        #region Constructor
        public ServerSettingsViewModel(IDialogCoordinator dialogCoordinator, ObservableCollection<string> servers)
        {
            _dialogCoordinator = dialogCoordinator;
            Servers = servers;
        }
        #endregion

        #region Private methods
        private async void AddServer()
        {
            var newServerName = _dialogCoordinator.ShowModalInputExternal(this, "Add Server", "Server name:")?.ToUpper();
            if (newServerName != null && string.IsNullOrWhiteSpace(newServerName))
            {
                await _dialogCoordinator.ShowMessageAsync(this, "Invalid Entry", "The new server name cannot be empty or whitespace.");
                return;
            }
            else if (newServerName != null)
            {
                //Show progress dialog while searching for new server
                var progressController = await _dialogCoordinator.ShowProgressAsync(this, "Looking for Server", $"Trying to find server \"{newServerName}\"...");
                progressController.SetIndeterminate();
                var serverExists = false;
                await Task.Run(() =>
                {
                    serverExists = new DirectoryInfo($"\\\\{newServerName}\\c$").Exists;
                });
                await progressController.CloseAsync();

                //If server can't be found, ask user if they want to continue adding server
                if (!serverExists && await _dialogCoordinator.ShowMessageAsync(this, "Server Not Found", $"The server \"{newServerName}\" could not be found. Would you still like to add it?", MessageDialogStyle.AffirmativeAndNegative) != MessageDialogResult.Affirmative)
                {
                    return;
                }

                //Determine whether or not server has already been added
                if (ServerSettings.Default.Servers.Contains(newServerName))
                {
                    await _dialogCoordinator.ShowMessageAsync(this, "Server Already Added", $"Server \"{newServerName}\" has already been added.");
                    return;
                }

                AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Adding server \"{newServerName}\" to ServerSettings.Servers.", LogMessageType.Verbose);
                ServerSettings.Default.Servers.Add(newServerName);
                AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Saving ServerSettings.", LogMessageType.Verbose);
                ServerSettings.Default.Save();
                Servers.Add(newServerName);
                AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Server \"{newServerName}\" was successfully added to ServerSettings.Servers.", LogMessageType.Verbose);
            }
        }

        private bool CanRemoveServer()
        {
            return Servers.Count > 0 && SelectedIndex < Servers.Count;
        }

        private async void RemoveServer()
        {
            try
            {
                if (await _dialogCoordinator.ShowMessageAsync(this, "Confirm Remove Server", $"Are you sure that you would like to remove server \"{SelectedServer}\"?", MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
                {
                    var serverName = Servers[SelectedIndex];
                    AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Removing server \"{serverName}\" from ServerSettings.Servers.", LogMessageType.Verbose);
                    ServerSettings.Default.Servers.RemoveAt(SelectedIndex);
                    AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Saving ServerSettings.", LogMessageType.Verbose);
                    ServerSettings.Default.Save();
                    Servers.RemoveAt(SelectedIndex);
                    SelectedIndex = 0;
                    AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Server \"{serverName}\" was successfully removed from ServerSettings.Servers.", LogMessageType.Verbose);
                }
            }
            catch (Exception ex)
            {
                var userMessage = $"Could not remove server.";
                AppInfo.BaseAppInfo.Log.QueueLogMessageAsync(userMessage + $" Error message: {ex.Message}", LogMessageType.Error);
                await _dialogCoordinator.ShowMessageAsync(this, "Remove Server Failed", userMessage);
            }
        }
        #endregion
    }
}
