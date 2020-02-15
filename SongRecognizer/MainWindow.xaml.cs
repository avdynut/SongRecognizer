using NAudio.Wave;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
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

        private TelegramClient _telegramClient;
        private WasapiLoopbackCapture _captureInstance;

        public TLInputPeerUser YaMelodyBot;

        public async Task InitializeAsync(TelegramClient telegramClient)
        {
            _telegramClient = telegramClient ?? throw new ArgumentNullException(nameof(telegramClient));
            YaMelodyBot = await _telegramClient.GetPeerUser(YaMelodyBotUsername);

            InitializeComponent();
        }

        private void OnStartRecordButtonClick(object sender, RoutedEventArgs e)
        {
            _captureInstance = new WasapiLoopbackCapture();
            var audioWriter = new WaveFileWriter(FileName, _captureInstance.WaveFormat);

            _captureInstance.DataAvailable += (s, a) => audioWriter.Write(a.Buffer, 0, a.BytesRecorded);

            _captureInstance.RecordingStopped += (s, a) =>
            {
                audioWriter.Dispose();
                _captureInstance.Dispose();
            };

            _captureInstance.StartRecording();
        }

        private void OnStopRecordButtonClick(object sender, RoutedEventArgs e)
        {
            _captureInstance?.StopRecording();
        }

        private async void OnSendMessageButtonClick(object sender, RoutedEventArgs e)
        {
            var fileResult = await _telegramClient.UploadFile(FileName, new StreamReader(FileName));

            var attributes = new TLVector<TLAbsDocumentAttribute>();
            var sendResult = await _telegramClient.SendUploadedDocument(YaMelodyBot, fileResult, "", "audio/vnd.wave", attributes);
        }

        private async void OnUpdateHistoryButtonClick(object sender, RoutedEventArgs e)
        {
            var history = (TLMessages)await _telegramClient.GetHistoryAsync(YaMelodyBot);
            var messages = history.Messages.OfType<TLMessage>()
                .Where(x => x.FromId == YaMelodyBot.UserId && x.Message.Contains("music.yandex.ru"));

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
