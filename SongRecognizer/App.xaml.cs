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

        private readonly TelegramClient _telegramClient = new TelegramClient(ApiId, ApiHash);

        private async void OnStartup(object sender, StartupEventArgs e)
        {
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;

            await _telegramClient.ConnectAsync();

            if (!_telegramClient.IsUserAuthorized())
            {
                var loginWindow = new LoginWindow(_telegramClient);

                if (loginWindow.ShowDialog() == false)
                {
                    Shutdown();
                    return;
                }
            }

            await mainWindow.InitializeAsync(_telegramClient);
            MainWindow.Show();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString(), e.Exception.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
