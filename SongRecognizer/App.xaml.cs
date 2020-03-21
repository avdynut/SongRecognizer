using SongRecognizer.ViewModels;
using System.Windows;
using System.Windows.Shell;
using System.Windows.Threading;

namespace SongRecognizer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void OnStartup(object sender, StartupEventArgs e)
        {
            var taskBarItemInfo = new TaskbarItemInfo();
            var mainViewModel = new MainViewModel(taskBarItemInfo);
            MainWindow = new MainWindow { DataContext = mainViewModel, TaskbarItemInfo = taskBarItemInfo };
            // move taskbariteminfo
            MainWindow.Show();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString(), e.Exception.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
