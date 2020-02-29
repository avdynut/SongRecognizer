using MaterialDesignThemes.Wpf;
using SongRecognizer.Commands;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
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

        private int _selectedSlideIndex;
        public int SelectedSlideIndex
        {
            get => _selectedSlideIndex;
            set
            {
                _selectedSlideIndex = value;
                OnPropertyChanged();
            }
        }

        private bool _requestInProcess;
        public bool RequestInProcess
        {
            get => _requestInProcess;
            set
            {
                _requestInProcess = value;
                OnPropertyChanged();
            }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public ICommand QueryPhoneCodeCommand { get; }
        public ICommand AuthCommand { get; }

        public LoginViewModel(TelegramClient telegramClient)
        {
            _telegramClient = telegramClient ?? throw new ArgumentNullException(nameof(telegramClient));

            QueryPhoneCodeCommand = new AsyncCommand(QueryPhoneCodeAsync, CanQueryPhoneCode, OnError);
            AuthCommand = new AsyncCommand<IInputElement>(AuthAsync, CanAuth, OnError);
        }

        private async Task QueryPhoneCodeAsync()
        {
            RequestInProcess = true;
            ErrorMessage = null;

            try
            {
                _codeHash = await _telegramClient.SendCodeRequestAsync(PhoneNumber);
                SelectedSlideIndex = 1;
            }
            catch (Exception exception)
            {
                ErrorMessage = exception.Message;
            }
            finally
            {
                RequestInProcess = false;
            }
        }

        private bool CanQueryPhoneCode()
        {
            return !string.IsNullOrEmpty(PhoneNumber);
        }

        private async Task AuthAsync(IInputElement targetElement)
        {
            RequestInProcess = true;
            ErrorMessage = null;

            try
            {
                var user = await _telegramClient.MakeAuthAsync(PhoneNumber, _codeHash, ReceivedCode);
                DialogHost.CloseDialogCommand.Execute(null, targetElement);
            }
            catch (Exception exception)
            {
                ErrorMessage = exception.Message;
            }
            finally
            {
                RequestInProcess = false;
            }
        }

        private bool CanAuth(IInputElement targetElement)
        {
            return !string.IsNullOrEmpty(PhoneNumber)
                && !string.IsNullOrEmpty(_codeHash)
                && !string.IsNullOrEmpty(ReceivedCode)
                && targetElement != null;
        }

        private void OnError(Exception exception)
        {
            Debug.WriteLine(exception);
        }
    }
}
