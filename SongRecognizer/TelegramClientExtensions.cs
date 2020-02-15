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
        /// Retrieves peer user by it's username from user dialogs.
        /// </summary>
        public static async Task<TLInputPeerUser> GetPeerUser(this TelegramClient client, string username)
        {
            var dialogsResult = (TLDialogs)await client.GetUserDialogsAsync();
            var users = dialogsResult.Users.OfType<TLUser>();
            var bot = users.FirstOrDefault(x => x.Username == username);
            return new TLInputPeerUser { UserId = bot.Id, AccessHash = bot.AccessHash.Value };
        }
    }
}
