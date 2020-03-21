using MaterialDesignThemes.Wpf;
using SongRecognizer.Commands;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TdLib;

namespace SongRecognizer.ViewModels
{
    public class LoginViewModel : ViewModel
    {
        private readonly TdClient _telegramClient;

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

        public LoginViewModel(TdClient telegramClient)
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
                await _telegramClient.SetAuthenticationPhoneNumberAsync(PhoneNumber);
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
                await _telegramClient.CheckAuthenticationCodeAsync(ReceivedCode);
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
                && !string.IsNullOrEmpty(ReceivedCode)
                && targetElement != null;
        }

        private void OnError(Exception exception)
        {
            Debug.WriteLine(exception);
        }
    }
}
