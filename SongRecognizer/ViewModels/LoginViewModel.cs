using SongRecognizer.Commands;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using TLSharp.Core;

namespace SongRecognizer.ViewModels
{
    public class LoginViewModel : ViewModel
    {
        private readonly TelegramClient _telegramClient;
        private string _codeHash;

        public string PhoneNumber { get; set; }
        public string ReceivedCode { get; set; }

        private bool? dialogResult;
        public bool? DialogResult
        {
            get => dialogResult;
            set
            {
                dialogResult = value;
                OnPropertyChanged();
            }
        }

        public ICommand QueryPhoneCodeCommand { get; }
        public ICommand AuthCommand { get; }

        public LoginViewModel(TelegramClient telegramClient)
        {
            _telegramClient = telegramClient ?? throw new ArgumentNullException(nameof(telegramClient));

            QueryPhoneCodeCommand = new AsyncCommand(QueryPhoneCodeAsync, CanQueryPhoneCode, OnError);
            AuthCommand = new AsyncCommand(AuthAsync, CanAuth, OnError);
        }

        private async Task QueryPhoneCodeAsync()
        {
            _codeHash = await _telegramClient.SendCodeRequestAsync(PhoneNumber);
        }

        private bool CanQueryPhoneCode()
        {
            return !string.IsNullOrEmpty(PhoneNumber);
        }

        private async Task AuthAsync()
        {
            var user = await _telegramClient.MakeAuthAsync(PhoneNumber, _codeHash, ReceivedCode);
            DialogResult = true;
        }

        private bool CanAuth()
        {
            return !string.IsNullOrEmpty(PhoneNumber) && !string.IsNullOrEmpty(_codeHash) && !string.IsNullOrEmpty(ReceivedCode);
        }

        private void OnError(Exception exception)
        {
            Debug.WriteLine(exception);
        }
    }
}
