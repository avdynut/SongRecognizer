﻿using MaterialDesignThemes.Wpf;
using NAudio.Wave;
using SongRecognizer.Commands;
using SongRecognizer.Models;
using SongRecognizer.Views;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
using System.Windows.Threading;
using TdLib;
using static TdLib.TdApi;
using static TdLib.TdApi.AuthorizationState;
using static TdLib.TdApi.Update;

namespace SongRecognizer.ViewModels
{
    public class MainViewModel : ViewModel
    {
        private const int ApiId = 1087573;
        private const string ApiHash = "478d65ed651632ca1cb656e2b9013501";
        private const string YaMelodyBotUsername = "YaMelodyBot";
        private const string FileName = "record.wav";
        private const string IdentifyTitle = "Identify";
        private const double RecordDurationSeconds = 3;
        private const int ResponseTimeoutSeconds = 10;
        private const int MinFileSizeBytes = 1024 * 100; // 100KB

        private readonly TaskbarItemInfo _taskBarInfo;
        private readonly TdClient _telegramClient = new TdClient();
        private int _botId;
        private Chat _botChat;
        private int _imageId;
        private bool _isReady;

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

        private string _state = "Loading...";
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
                _taskBarInfo.ProgressState = _isInProcess ? TaskbarItemProgressState.Indeterminate : TaskbarItemProgressState.None;
                OnPropertyChanged();
            }
        }

        public Duration RecordDuration { get; } = TimeSpan.FromSeconds(RecordDurationSeconds);

        public ICommand IdentifySongCommand { get; }
        public ICommand NavigateLinkCommand { get; }

        public MainViewModel(TaskbarItemInfo taskBarInfo)
        {
            _taskBarInfo = taskBarInfo ?? throw new ArgumentNullException(nameof(taskBarInfo));

            IdentifySongCommand = new AsyncCommand(IdentifySong, () => _isReady, OnError);
            NavigateLinkCommand = new RelayCommand(NavigateLink, () => !string.IsNullOrEmpty(Song?.Link?.ToString()));

            InitTdLog();
            _telegramClient.UpdateReceived += OnUpdateReceived;
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
                    await OpenAuthDialogAsync();
                    break;
                case UpdateAuthorizationState authorizationState when authorizationState.AuthorizationState is AuthorizationStateWaitCode:
                    // call in dialog
                    break;
                case UpdateAuthorizationState authorizationState when authorizationState.AuthorizationState is AuthorizationStateReady:
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
                case UpdateFile file when file.File.Id == _imageId:
                    OnFileReceived(file.File.Local);
                    break;
                case UpdateConnectionState connectionState:
                    break;
            }
        }

        private async Task OpenAuthDialogAsync()
        {
            await Dispatcher.CurrentDispatcher.BeginInvoke(async () =>
            {
                var loginDialog = new LoginDialog { DataContext = new LoginViewModel(_telegramClient) };
                await DialogHost.Show(loginDialog);
            });
        }

        private void OnFileReceived(LocalFile file)
        {
            if (file.IsDownloadingCompleted)
            {
                //var processInfo = new ProcessStartInfo
                //{
                //    FileName = file.Path,
                //    UseShellExecute = true
                //};
                //Process.Start(processInfo);

                // todo: add image
            }
        }

        private async Task ParseMessageAsync(Message message)
        {
            if (message.Content is MessageContent.MessageText messageText)
            {
                var image = messageText.WebPage?.Photo?.Sizes?.LastOrDefault()?.Photo;
                if (image != null)
                {
                    _imageId = image.Id;
                    var imageFile = await _telegramClient.DownloadFileAsync(_imageId, priority: 20);
                }
                else
                {

                }

                //if (message.Message.Contains("..."))    // 'Обрабатываю...'
                //{
                //    State = "Identifying...";
                //}
                //else if (message.Message.Contains("music.yandex.ru"))
                //{
                //    Song = new Song(message.Message);
                //}
                //else
                //{
                //    Song = new Song();
                //}
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

            _isReady = true;
            CommandManager.InvalidateRequerySuggested();
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

        private void InitTdLog()
        {
            TdLog.SetVerbosityLevel(2);
            TdLog.SetFatalErrorCallback(OnError);
            TdLog.SetFilePath("tgc_log.txt");
        }

        private async Task IdentifySong()
        {
            IsInProcess = true;
            State = "Recording...";
            Song = null;
            ErrorMessage = null;

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
            }

            State = IdentifyTitle;
            IsInProcess = false;
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
            Debug.WriteLine(exception);
            ErrorMessage = exception.Message;
            State = IdentifyTitle;
        }

        private void OnError(string error)
        {
            Debug.WriteLine(error);
            ErrorMessage = error;
            State = IdentifyTitle;
        }
    }
}
