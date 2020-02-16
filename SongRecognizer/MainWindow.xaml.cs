using NAudio.Wave;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using TeleSharp.TL;
using TLSharp.Core;
using TLSharp.Core.Utils;

namespace SongRecognizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string YaMelodyBotUsername = "YaMelodyBot";
        private const string FileName = "record.wav";
        private const int RecordDurationSeconds = 5;
        private const int ResponseTimeoutSeconds = 10;
        private const int MinFileSizeBytes = 1024;

        private TelegramClient _telegramClient;
        private TLInputPeerUser _yaMelodyBot;

        private State _state;
        private State State
        {
            get => _state;
            set
            {
                _state = value;
                Status.Text = _state.ToString();
            }
        }

        public async Task InitializeAsync(TelegramClient telegramClient)
        {
            _telegramClient = telegramClient ?? throw new ArgumentNullException(nameof(telegramClient));
            _yaMelodyBot = await _telegramClient.GetPeerUser(YaMelodyBotUsername);

            InitializeComponent();
        }

        private async void OnIdentifySongButtonClick(object sender, RoutedEventArgs e)
        {
            State = State.Recording;
            ClearResult();
            var captureInstance = new WasapiLoopbackCapture();
            var audioWriter = new WaveFileWriter(FileName, captureInstance.WaveFormat);

            captureInstance.DataAvailable += (s, e) => audioWriter.Write(e.Buffer, 0, e.BytesRecorded);

            RecordProgress.IsIndeterminate = true;
            captureInstance.StartRecording();
            await Task.Delay(TimeSpan.FromSeconds(RecordDurationSeconds));
            captureInstance.StopRecording();
            RecordProgress.IsIndeterminate = false;

            audioWriter.Dispose();
            captureInstance.Dispose();

            var fileInfo = new FileInfo(FileName);
            if (fileInfo.Exists && fileInfo.Length > MinFileSizeBytes)
            {
                await SendRecord();
                WaitForResponse();
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
                    SetResult(message.Message);
                    break;
                }
                else
                {
                    State = State.Completed;
                    Result.Text = "Cannot Identify";
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

        private void ClearResult()
        {
            Result.Text = "";
            LinkText.Text = "";
        }

        private void SetResult(string message)
        {
            var result = message.Split('\n');
            string link = result[1];

            Result.Text = result[0];
            Link.NavigateUri = new Uri(link);
            LinkText.Text = link;

            result = result[0].Split('-');
            string artist = result[0].Trim();
            string title = result[1].Trim();
        }

        private void OnLinkRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            //Process.Start(e.Uri.ToString());
        }
    }
}
