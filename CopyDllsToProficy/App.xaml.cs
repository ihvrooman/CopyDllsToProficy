using AppStandards.Logging;
using CopyDllsToProficy.Helpers;
using CopyDllsToProficy.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CopyDllsToProficy
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Public methods
        [STAThread]
        public static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            //Upgrade settings
            try
            {
                if (Settings.Default.UpgradeSettings)
                {
                    //Upgrate main settings
                    AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Upgrading main settings.");
                    Settings.Default.Upgrade();
                    Settings.Default.UpgradeSettings = false;
                    Settings.Default.Save();
                    AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Main settings successfully upgraded.");

                    //Upgrade server settings
                    AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Upgrading server settings.");
                    ServerSettings.Default.Upgrade();
                    ServerSettings.Default.Save();
                    AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Server settings successfully upgraded.");
                }
            }
            catch (Exception ex)
            {
                AppInfo.BaseAppInfo.Log.QueueLogMessageAsync($"Failed to upgrade main and server settings. Error message: {ex.Message}", LogMessageType.Error);
            }

            var application = new App();
            application.InitializeComponent();
            application.Run();
        }
        #endregion

        #region Private methods
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            var errorMessage = $"An unhandled exception occurred. Error message: {ex.Message} Exception stack trace: {ex.StackTrace}";
            if (ex.InnerException != null)
            {
                errorMessage += $" | Inner exception message: {ex.InnerException.Message} Inner exception stack trace: {ex.InnerException.StackTrace}";
            }
            AppInfo.BaseAppInfo.Log.QueueLogMessageAsync(errorMessage, LogMessageType.Error);

            //Wait for message queue to be flushed
            Task.Delay(31).Wait();
        }
        #endregion
    }
}
