using NAudio.Wave;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;
using TeleSharp.TL;
using TeleSharp.TL.Messages;
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
        private const int RecordDurationSeconds = 7;

        private TelegramClient _telegramClient;
        private TLInputPeerUser _yaMelodyBot;

        public async Task InitializeAsync(TelegramClient telegramClient)
        {
            _telegramClient = telegramClient ?? throw new ArgumentNullException(nameof(telegramClient));
            _yaMelodyBot = await _telegramClient.GetPeerUser(YaMelodyBotUsername);

            InitializeComponent();
            RecordProgress.Maximum = RecordDurationSeconds;
        }

        private void OnIdentifySongButtonClick(object sender, RoutedEventArgs e)
        {
            var captureInstance = new WasapiLoopbackCapture();
            var audioWriter = new WaveFileWriter(FileName, captureInstance.WaveFormat);

            captureInstance.DataAvailable += (s, e) => audioWriter.Write(e.Buffer, 0, e.BytesRecorded);

            captureInstance.RecordingStopped += async (s, e) =>
            {
                audioWriter.Dispose();
                captureInstance.Dispose();
                await SendRecord();
            };

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (s, e) =>
            {
                RecordProgress.Value++;
                if (RecordProgress.Value == RecordProgress.Maximum)
                {
                    timer.Stop();
                    captureInstance.StopRecording();
                }
            };

            RecordProgress.Value = 0;
            timer.Start();
            captureInstance.StartRecording();
        }

        private async Task SendRecord()
        {
            var fileResult = await _telegramClient.UploadFile(FileName, new StreamReader(FileName));

            var attributes = new TLVector<TLAbsDocumentAttribute>();
            var sendResult = await _telegramClient.SendUploadedDocument(
                _yaMelodyBot, fileResult, FileName, "audio/vnd.wave", attributes);
        }

        private async void OnUpdateHistoryButtonClick(object sender, RoutedEventArgs e)
        {
            var history = (TLMessages)await _telegramClient.GetHistoryAsync(_yaMelodyBot);
            var messages = history.Messages.OfType<TLMessage>()
                .Where(x => x.FromId == _yaMelodyBot.UserId && x.Message.Contains("music.yandex.ru"));

            var result = messages.First().Message.Split('\n');
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
