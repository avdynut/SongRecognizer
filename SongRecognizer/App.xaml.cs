using SongRecognizer.ViewModels;
using SongRecognizer.Windows;
using System.Windows;
using System.Windows.Threading;
using TLSharp.Core;

namespace SongRecognizer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const int ApiId = 1087573;
        private const string ApiHash = "478d65ed651632ca1cb656e2b9013501";

        private TelegramClient _telegramClient;

        private async void OnStartup(object sender, StartupEventArgs e)
        {
            _telegramClient = new TelegramClient(ApiId, ApiHash);

            var mainViewModel = new MainViewModel(_telegramClient);
            MainWindow = new MainWindow { DataContext = mainViewModel };

            await _telegramClient.ConnectAsync();

            if (!_telegramClient.IsUserAuthorized())
            {
                var loginWindow = new LoginWindow { DataContext = new LoginViewModel(_telegramClient) };

                if (loginWindow.ShowDialog() == false)
                {
                    Shutdown();
                    return;
                }
            }

            await mainViewModel.InitializeAsync();
            MainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _telegramClient?.Dispose();
            base.OnExit(e);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString(), e.Exception.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
