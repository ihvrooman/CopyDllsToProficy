using AppStandards;
using AppStandards.Helpers;
using CopyDllsToProficy.Helpers;
using CopyDllsToProficy.Properties;
using CopyDllsToProficy.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CopyDllsToProficy.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region Fields
        private bool _loadingWindow = true;
        #endregion

        #region Properties
        private MainWindowViewModel _viewModel
        {
            get
            {
                return (MainWindowViewModel)DataContext;
            }
        }
        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
        }
        #endregion

        #region Private methods
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = new MainWindowViewModel(DialogCoordinator.Instance);
            ServerSettingsView.Servers = _viewModel.Servers;
            _loadingWindow = false;
            _viewModel.ConvertServerNamesToUpperCase();
            Task.Run(() =>
            {
                Task.Delay(1200).Wait();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _viewModel.ShowMessages();
                });
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Settings.Default.MainWindowPlacement = this.GetPlacement();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Routines.Shutdown(AppInfo.BaseAppInfo);
            _viewModel.Dispose();
        }
        #endregion

        #region Protected methods
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.SetPlacement(Settings.Default.MainWindowPlacement);
        }
        #endregion
    }
}
