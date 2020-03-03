using MaterialDesignThemes.Wpf;
using NAudio.Wave;
using SongRecognizer.Commands;
using SongRecognizer.Models;
using SongRecognizer.Views;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TeleSharp.TL;
using TLSharp.Core;
using TLSharp.Core.Utils;

namespace SongRecognizer.ViewModels
{
    public class MainViewModel : ViewModel
    {
        private const int ApiId = 1087573;
        private const string ApiHash = "478d65ed651632ca1cb656e2b9013501";
        private const string YaMelodyBotUsername = "YaMelodyBot";
        private const string FileName = "record.wav";
        private const double RecordDurationSeconds = 3;
        private const int ResponseTimeoutSeconds = 10;
        private const int MinFileSizeBytes = 1024 * 100;

        private TelegramClient _telegramClient;
        private TLInputPeerUser _yaMelodyBot;

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

        private string _state;
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

        private bool _isConnecting;
        public bool IsConnecting
        {
            get => _isConnecting;
            set
            {
                _isConnecting = value;
                OnPropertyChanged();
            }
        }

        private bool _isReady;

        public Duration RecordDuration { get; } = TimeSpan.FromSeconds(RecordDurationSeconds);

        public ICommand IdentifySongCommand { get; }
        public ICommand NavigateLinkCommand { get; }

        public MainViewModel()
        {
            IdentifySongCommand = new AsyncCommand(IdentifySong, () => _isReady, OnError);
            NavigateLinkCommand = new RelayCommand(NavigateLink, () => !string.IsNullOrEmpty(Song?.Link?.ToString()));
        }

        public async Task InitializeAsync()
        {
            IsConnecting = true;

            bool connected = await HandleTask(() => ConnectAsync());

            if (connected)
            {
                if (!_telegramClient.IsUserAuthorized())
                {
                    var loginDialog = new LoginDialog { DataContext = new LoginViewModel(_telegramClient) };
                    await DialogHost.Show(loginDialog);
                }

                _yaMelodyBot = await HandleTask(() => _telegramClient.GetPeerUser(YaMelodyBotUsername));
                _isReady = _yaMelodyBot != null;
            }

            IsConnecting = false;
        }

        private async Task<bool> ConnectAsync()
        {
            _telegramClient = new TelegramClient(ApiId, ApiHash);
            await _telegramClient.ConnectAsync();
            return true;
        }

        private async Task IdentifySong()
        {
            State = "Recording";
            Song = null;

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
                WaitForResponse();
            }
            else
            {
                ErrorMessage = "Incorrect record";
            }

            State = null;
        }

        private async Task SendRecord()
        {
            State = "Sending Record";
            var fileResult = await HandleTask(() =>
                _telegramClient.UploadFile(FileName, new StreamReader(FileName)));

            var attributes = new TLVector<TLAbsDocumentAttribute> { new TLDocumentAttributeFilename { FileName = FileName } };
            var sendResult = await HandleTask(() =>
                _telegramClient.SendUploadedDocument(_yaMelodyBot, fileResult, "", "audio/vnd.wave", attributes));
        }

        private async void WaitForResponse()
        {
            State = "Waiting for Response";
            var startTime = DateTime.Now;

            while (true)
            {
                var message = await HandleTask(() => _telegramClient.GetLastMessage(_yaMelodyBot));
                if (message is null)
                    continue;

                if (message.Message.Contains("..."))    // 'Обрабатываю...'
                {
                    State = "Identifying";
                }
                else if (message.Message.Contains("music.yandex.ru"))
                {
                    Song = new Song(message.Message);
                    break;
                }
                else
                {
                    Song = new Song();
                    break;
                }

                if ((DateTime.Now - startTime).Seconds > ResponseTimeoutSeconds)
                {
                    State = "Responce is not received";
                    break;
                }
                await Task.Delay(200);
            }

            State = null;
        }

        private void NavigateLink()
        {
            var processInfo = new ProcessStartInfo { FileName = Song.Link.ToString(), UseShellExecute = true };
            Process.Start(processInfo);
        }

        private async Task<T> HandleTask<T>(Func<Task<T>> task)
        {
            ErrorMessage = null;
            T result = default;

            try
            {
                result = await task();
            }
            catch (Exception exception)
            {
                OnError(exception);
            }

            return result;
        }

        private void OnError(Exception exception)
        {
            Debug.WriteLine(exception);
            ErrorMessage = exception.Message;
            State = "Error";
        }
    }
}
