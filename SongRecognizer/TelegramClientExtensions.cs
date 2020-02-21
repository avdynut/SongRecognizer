using System.Linq;
using System.Threading.Tasks;
using TeleSharp.TL;
using TeleSharp.TL.Messages;
using TLSharp.Core;

namespace SongRecognizer
{
    public static class TelegramClientExtensions
    {
        /// <summary>
        /// Searches peer user by it's username.
        /// </summary>
        public static async Task<TLInputPeerUser> GetPeerUser(this TelegramClient client, string username)
        {
            var result = await client.SearchUserAsync(username);
            var users = result.Users.OfType<TLUser>();
            var bot = users.FirstOrDefault(x => x.Username == username);
            return new TLInputPeerUser { UserId = bot.Id, AccessHash = bot.AccessHash.Value };
        }

        public static async Task<TLMessage> GetLastMessage(this TelegramClient client, TLInputPeerUser peer)
        {
            var history = (TLMessagesSlice)await client.GetHistoryAsync(peer);
            var message = history.Messages.OfType<TLMessage>().First(x => x.FromId == peer.UserId);
            return message;
        }
    }
}
