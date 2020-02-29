using SongRecognizer.ViewModels;
using System.Windows;
using System.Windows.Threading;

namespace SongRecognizer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            var mainViewModel = new MainViewModel();
            MainWindow = new MainWindow { DataContext = mainViewModel };
            MainWindow.Show();
            await mainViewModel.InitializeAsync();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString(), e.Exception.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
