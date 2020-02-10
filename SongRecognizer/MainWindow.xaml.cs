using System.Linq;
using System.Windows;
using TeleSharp.TL;
using TeleSharp.TL.Messages;
using TLSharp.Core;

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

        private readonly TelegramClient _client = new TelegramClient(ApiId, ApiHash);
        private string _codeHash;
        private TLUser _user;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void OnConnectButtonClick(object sender, RoutedEventArgs e)
        {
            await _client.ConnectAsync();
        }

        private async void OnGetCodeButtonClick(object sender, RoutedEventArgs e)
        {
            _codeHash = await _client.SendCodeRequestAsync(PhoneNumber.Text);
        }

        private async void OnAuthButtonClick(object sender, RoutedEventArgs e)
        {
            _user = await _client.MakeAuthAsync(PhoneNumber.Text, _codeHash, ReceivedCode.Text);
        }

        private async void OnSendMessageButtonClick(object sender, RoutedEventArgs e)
        {
            var dialogsResult = (TLDialogs)await _client.GetUserDialogsAsync();
            var users = dialogsResult.Users.OfType<TLUser>();
            var bot = users.FirstOrDefault(x => x.Username == YaMelodyBotUsername);

            var peer = new TLInputPeerUser { UserId = bot.Id, AccessHash = bot.AccessHash.Value };
            var result = await _client.SendMessageAsync(peer, "message");
        }
    }
}
