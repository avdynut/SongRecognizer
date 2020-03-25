using NAudio.Wave;
using SongRecognizer.Commands;
using SongRecognizer.Models;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TdLib;
using static TdLib.TdApi;
using static TdLib.TdApi.AuthorizationState;
using static TdLib.TdApi.ConnectionState;
using static TdLib.TdApi.Update;

namespace SongRecognizer.ViewModels
{
    public class MainViewModel : ViewModel
    {
        private const int ApiId = 1087573;
        private const string ApiHash = "478d65ed651632ca1cb656e2b9013501";
        private const string YaMelodyBotUsername = "YaMelodyBot";

        private const string IdentifyTitle = "Identify";
        private const string FileName = "record.wav";        
        private const double RecordDurationSeconds = 3;
        private const int MinFileSizeBytes = 1024 * 100; // 100KB

        private readonly TdClient _telegramClient = new TdClient();

        private int _botId;
        private Chat _botChat;
        private int _imageId;

        private Song _song;
        public Song Song
        {
            get => _song;
            private set
            {
                _song = value;
                OnPropertyChanged();
            }
        }

        private Uri _imageUri;
        public Uri ImageUri
        {
            get => _imageUri;
            set
            {
                _imageUri = value;
                OnPropertyChanged();
            }
        }

        private string _state = "Connecting...";
        public string State
        {
            get => _state;
            set
            {
                _state = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
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

        private bool _isInProcess = true;
        public bool IsInProcess
        {
            get => _isInProcess;
            set
            {
                _isInProcess = value;
                if (!_isInProcess)
                {
                    State = IdentifyTitle;
                }
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public Duration RecordDuration { get; } = TimeSpan.FromSeconds(RecordDurationSeconds);

        public ICommand IdentifySongCommand { get; }
        public ICommand NavigateLinkCommand { get; }

        #region Login Dialog
        public string PhoneNumber { get; set; }
        public string ReceivedCode { get; set; }

        private bool _isAuthRequired;
        public bool IsAuthRequired
        {
            get => _isAuthRequired;
            set
            {
                _isAuthRequired = value;
                OnPropertyChanged();
            }
        }

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

        public ICommand QueryPhoneCodeCommand { get; }
        public ICommand AuthCommand { get; }
        #endregion

        public MainViewModel()
        {
            IdentifySongCommand = new AsyncCommand(IdentifySong, () => !IsInProcess, OnError);
            NavigateLinkCommand = new RelayCommand(NavigateLink, () => !string.IsNullOrEmpty(Song?.Link?.ToString()));
            QueryPhoneCodeCommand = new AsyncCommand(QueryPhoneCodeAsync, CanQueryPhoneCode, OnError);
            AuthCommand = new AsyncCommand(AuthAsync, CanAuth, OnError);

            InitLogging();
            _telegramClient.UpdateReceived += OnUpdateReceived;
        }

        private void InitLogging()
        {
            TdLog.SetVerbosityLevel((int)Models.LogVerbosityLevel.Warnings);
            TdLog.SetFatalErrorCallback(OnError);
            TdLog.SetFilePath("tgc_log.txt");
        }

        private async void OnUpdateReceived(object sender, Update update)
        {
            switch (update)
            {
                case UpdateAuthorizationState authorizationState when authorizationState.AuthorizationState is AuthorizationStateWaitTdlibParameters:
                    await SetTdLibParametersAsync();
                    break;
                case UpdateAuthorizationState authorizationState when authorizationState.AuthorizationState is AuthorizationStateWaitEncryptionKey:
                    await _telegramClient.CheckDatabaseEncryptionKeyAsync();
                    break;
                case UpdateAuthorizationState authorizationState when authorizationState.AuthorizationState is AuthorizationStateWaitPhoneNumber:
                    IsInProcess = false;
                    IsAuthRequired = true;
                    break;
                case UpdateAuthorizationState authorizationState when authorizationState.AuthorizationState is AuthorizationStateWaitCode:
                    SelectedSlideIndex = 1;
                    break;
                case UpdateAuthorizationState authorizationState when authorizationState.AuthorizationState is AuthorizationStateReady:
                    IsAuthRequired = false;
                    await SearchMusicBotAsync();
                    break;
                case UpdateMessageSendFailed message when message.Message.SenderUserId == _botId:
                    OnError("Message was not sent");
                    break;
                case UpdateMessageSendSucceeded message when message.Message.SenderUserId == _botId:
                    // message successfully sent
                    break;
                case UpdateNewMessage message when message.Message.SenderUserId == _botId:
                    await ParseMessageAsync(message.Message);
                    break;
                case UpdateFile file when file.File.Id == _imageId && file.File.Local.IsDownloadingCompleted:
                    ImageUri = new Uri(file.File.Local.Path);
                    break;
                case UpdateConnectionState connectionState when connectionState.State is ConnectionStateConnecting:
                    break;
                case UpdateConnectionState connectionState when connectionState.State is ConnectionStateReady:
                    break;
            }
        }

        private Task SetTdLibParametersAsync()
        {
            var parameters = new TdlibParameters
            {
                ApiId = ApiId,
                ApiHash = ApiHash,
                ApplicationVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                DeviceModel = "PC",
                SystemLanguageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                SystemVersion = Environment.OSVersion.ToString()
            };
            return _telegramClient.SetTdlibParametersAsync(parameters);
        }

        private async Task ParseMessageAsync(Message message)
        {
            if (message.Content is MessageContent.MessageText messageText)
            {
                string text = messageText.Text.Text;
                if (text.Contains("..."))    // 'Обрабатываю...'
                {
                    State = "Identifying...";
                }
                else if (text.Contains("music.yandex.ru"))
                {
                    Song = new Song(text);

                    // take the biggest size photo
                    var image = messageText.WebPage?.Photo?.Sizes?.LastOrDefault()?.Photo;
                    if (image != null)
                    {
                        _imageId = image.Id;
                        var imageFile = await _telegramClient.DownloadFileAsync(_imageId, priority: 20);
                    }

                    IsInProcess = false;
                }
                else
                {
                    Song = new Song();
                    IsInProcess = false;
                }
            }
            var messageIds = new long[] { message.Id };
            await _telegramClient.ViewMessagesAsync(message.ChatId, messageIds, true);
        }

        private async Task SearchMusicBotAsync()
        {
            _botChat = await _telegramClient.SearchPublicChatAsync(YaMelodyBotUsername);
            _botId = (_botChat.Type as ChatType.ChatTypePrivate).UserId;
            var notifSettings = new ChatNotificationSettings { MuteFor = int.MaxValue };
            await _telegramClient.SetChatNotificationSettingsAsync(_botChat.Id, notifSettings);
            //var startMessage = _client.SendBotStartMessageAsync(_botId, botChat.Id, "a");

            IsInProcess = false;
        }

        private void StartProcess()
        {
            IsInProcess = true;
            ErrorMessage = null;
        }

        private async Task IdentifySong()
        {
            StartProcess();
            State = "Recording...";
            Song = null;
            ImageUri = null;

            var captureInstance = new WasapiLoopbackCapture();
            var audioWriter = new WaveFileWriter(FileName, captureInstance.WaveFormat);

            captureInstance.DataAvailable += (s, e) => audioWriter.Write(e.Buffer, 0, e.BytesRecorded);

            captureInstance.StartRecording();
            await Task.Delay(RecordDuration.TimeSpan);
            captureInstance.StopRecording();

            audioWriter.Dispose();
            captureInstance.Dispose();

            var fileInfo = new FileInfo(FileName);
            if (fileInfo.Exists && fileInfo.Length > MinFileSizeBytes)
            {
                await SendRecord();
            }
            else
            {
                ErrorMessage = "Incorrect record";
                IsInProcess = false;
            }
        }

        private async Task SendRecord()
        {
            State = "Sending Record...";

            var file = new InputFile.InputFileLocal { Path = FileName };
            var audio = new InputMessageContent.InputMessageAudio { Audio = file, Duration = (int)RecordDurationSeconds };
            var message = await _telegramClient.SendMessageAsync(chatId: _botChat.Id, inputMessageContent: audio);
        }

        private void NavigateLink()
        {
            try
            {
                var processInfo = new ProcessStartInfo { FileName = Song.Link.ToString(), UseShellExecute = true };
                Process.Start(processInfo);
            }
            catch (Exception exception)
            {
                OnError(exception);
            }
        }

        #region Login Dialog commands
        private async Task QueryPhoneCodeAsync()
        {
            StartProcess();
            await _telegramClient.SetAuthenticationPhoneNumberAsync(PhoneNumber);
        }

        private bool CanQueryPhoneCode()
        {
            return !string.IsNullOrEmpty(PhoneNumber);
        }

        private async Task AuthAsync()
        {
            StartProcess();
            await _telegramClient.CheckAuthenticationCodeAsync(ReceivedCode);
        }

        private bool CanAuth()
        {
            return !string.IsNullOrEmpty(PhoneNumber) && !string.IsNullOrEmpty(ReceivedCode);
        }
        #endregion

        //private async Task<T> HandleTask<T>(Func<Task<T>> task)
        //{
        //    ErrorMessage = null;
        //    T result = default;

        //    try
        //    {
        //        result = await task();
        //    }
        //    catch (Exception exception)
        //    {
        //        OnError(exception);
        //    }

        //    return result;
        //}

        private void OnError(Exception exception)
        {
            OnError(exception.ToString());
        }

        private void OnError(string error)
        {
            Debug.WriteLine(error);
            ErrorMessage = error;
            IsInProcess = false;
        }
    }
}
