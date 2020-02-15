using System;
using System.Windows;
using TLSharp.Core;

namespace SongRecognizer
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly TelegramClient _telegramClient;
        private string _codeHash;

        public LoginWindow(TelegramClient telegramClient)
        {
            _telegramClient = telegramClient ?? throw new ArgumentNullException(nameof(telegramClient));

            InitializeComponent();
        }

        private async void OnGetCodeButtonClick(object sender, RoutedEventArgs e)
        {
            _codeHash = await _telegramClient.SendCodeRequestAsync(PhoneNumber.Text);
        }

        private async void OnAuthButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var user = await _telegramClient.MakeAuthAsync(PhoneNumber.Text, _codeHash, ReceivedCode.Text);
                DialogResult = true;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
