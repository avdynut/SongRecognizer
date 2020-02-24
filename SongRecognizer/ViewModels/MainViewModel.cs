using NAudio.Wave;
using SongRecognizer.Commands;
using SongRecognizer.Models;
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
        private const string YaMelodyBotUsername = "YaMelodyBot";
        private const string FileName = "record.wav";
        private const double RecordDurationSeconds = 3;
        private const int ResponseTimeoutSeconds = 10;
        private const int MinFileSizeBytes = 1024 * 100;

        private readonly TelegramClient _telegramClient;
        private TLInputPeerUser _yaMelodyBot;

        private State _state;
        public State State
        {
            get => _state;
            set
            {
                _state = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private Song song;
        public Song Song
        {
            get => song;
            private set
            {
                song = value;
                OnPropertyChanged();
            }
        }

        public Duration RecordDuration { get; } = TimeSpan.FromSeconds(RecordDurationSeconds);

        public ICommand IdentifySongCommand { get; }
        public ICommand NavigateLinkCommand { get; }

        public MainViewModel(TelegramClient telegramClient)
        {
            _telegramClient = telegramClient ?? throw new ArgumentNullException(nameof(telegramClient));

            IdentifySongCommand = new AsyncCommand(IdentifySong, errorHandler: OnError);
            NavigateLinkCommand = new RelayCommand(NavigateLink, () => !string.IsNullOrEmpty(Song?.Link?.ToString()));
        }

        public async Task InitializeAsync()
        {
            _yaMelodyBot = await _telegramClient.GetPeerUser(YaMelodyBotUsername);
        }

        private async Task IdentifySong()
        {
            State = State.Recording;
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
                State = State.Failed;
            }
        }

        private async Task SendRecord()
        {
            State = State.SendingRecord;
            var fileResult = await _telegramClient.UploadFile(FileName, new StreamReader(FileName));

            var attributes = new TLVector<TLAbsDocumentAttribute> { new TLDocumentAttributeFilename { FileName = FileName } };
            var sendResult = await _telegramClient.SendUploadedDocument(
                _yaMelodyBot, fileResult, "", "audio/vnd.wave", attributes);
        }

        private async void WaitForResponse()
        {
            State = State.WaitingForResponse;
            var startTime = DateTime.Now;

            while (true)
            {
                var message = await _telegramClient.GetLastMessage(_yaMelodyBot);

                if (message.Message.Contains("..."))    // 'Обрабатываю...'
                {
                    State = State.Identifying;
                }
                else if (message.Message.Contains("music.yandex.ru"))
                {
                    State = State.Completed;
                    Song = new Song(message.Message);
                    break;
                }
                else
                {
                    State = State.Completed;
                    Song = new Song();
                    break;
                }

                if ((DateTime.Now - startTime).Seconds > ResponseTimeoutSeconds)
                {
                    State = State.Failed;
                    break;
                }
                await Task.Delay(200);
            }
        }

        private void NavigateLink()
        {
            var processInfo = new ProcessStartInfo { FileName = Song.Link.ToString(), UseShellExecute = true };
            Process.Start(processInfo);
        }

        private void OnError(Exception exception)
        {
            Debug.WriteLine(exception);
            State = State.Failed;
        }
    }
}
