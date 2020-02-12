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
        private const int ApiId = 1087573;
        private const string ApiHash = "478d65ed651632ca1cb656e2b9013501";
        private const string YaMelodyBotUsername = "YaMelodyBot";
        private const string FileName = "record.wav";

        private readonly TelegramClient _client = new TelegramClient(ApiId, ApiHash);
        private string _codeHash;
        private TLInputPeerUser _bot;

        private WasapiLoopbackCapture _captureInstance;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void OnConnectButtonClick(object sender, RoutedEventArgs e)
        {
            await _client.ConnectAsync();
            await FindBot();
        }

        private async Task FindBot()
        {
            var dialogsResult = (TLDialogs)await _client.GetUserDialogsAsync();
            var users = dialogsResult.Users.OfType<TLUser>();
            var bot = users.FirstOrDefault(x => x.Username == YaMelodyBotUsername);
            _bot = new TLInputPeerUser { UserId = bot.Id, AccessHash = bot.AccessHash.Value };
        }

        private async void OnGetCodeButtonClick(object sender, RoutedEventArgs e)
        {
            _codeHash = await _client.SendCodeRequestAsync(PhoneNumber.Text);
        }

        private async void OnAuthButtonClick(object sender, RoutedEventArgs e)
        {
            var user = await _client.MakeAuthAsync(PhoneNumber.Text, _codeHash, ReceivedCode.Text);
        }

        private async void OnSendMessageButtonClick(object sender, RoutedEventArgs e)
        {
            var fileResult = await _client.UploadFile(FileName, new StreamReader(FileName));

            var attributes = new TLVector<TLAbsDocumentAttribute>();
            var sendResult = await _client.SendUploadedDocument(_bot, fileResult, "", "audio/vnd.wave", attributes);
        }

        private async void OnUpdateHistoryButtonClick(object sender, RoutedEventArgs e)
        {
            var history = (TLMessages)await _client.GetHistoryAsync(_bot);
            var messages = history.Messages.OfType<TLMessage>()
                .Where(x => x.FromId == _bot.UserId && x.Message.Contains("music.yandex.ru"));

            var result = messages.First().Message.Split('\n');
            string link = result[1];

            Result.Text = result[0];
            Link.NavigateUri = new Uri(link);
            LinkText.Text = link;

            result = result[0].Split('-');
            string artist = result[0].Trim();
            string title = result[1].Trim();
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

        private void OnLinkRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            //Process.Start(e.Uri.ToString());
        }
    }
}
