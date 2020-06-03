using CopyDllsToProficy.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for ServerSettingsView.xaml
    /// </summary>
    public partial class ServerSettingsView : UserControl
    {
        #region Properties
        private ServerSettingsViewModel _viewModel
        {
            get
            {
                return (ServerSettingsViewModel)DataContext;
            }
        }
        public ObservableCollection<string> Servers
        {
            get { return (ObservableCollection<string>)GetValue(ServersProperty); }
            set { SetValue(ServersProperty, value); }
        }
        #endregion

        #region Dependency properties
        private static readonly DependencyProperty ServersProperty = DependencyProperty.Register("Servers", typeof(ObservableCollection<string>), typeof(ServerSettingsView), new FrameworkPropertyMetadata(new ObservableCollection<string>()));
        #endregion

        #region Constructor
        public ServerSettingsView()
        {
            InitializeComponent();
        }
        #endregion

        #region Private methods
        private void ServerSettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = new ServerSettingsViewModel(DialogCoordinator.Instance, Servers);
        }
        #endregion
    }
}
