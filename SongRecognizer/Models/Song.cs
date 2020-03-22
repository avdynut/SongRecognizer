using System;

namespace SongRecognizer.Models
{
    public class Song
    {
        private readonly string _message;

        public string Result { get; private set; }
        public string Artist { get; private set; }
        public string Title { get; private set; }
        public Uri Link { get; private set; }

        public Song()
        {
            Result = "Cannot identify song";
        }

        public Song(string message)
        {
            _message = message ?? throw new ArgumentNullException(nameof(message));
            ParseMessage();
        }

        private void ParseMessage()
        {
            var result = _message.Split('\n');

            Result = result[0];

            string link = result[1];
            Link = new Uri(link);

            result = result[0].Split('-');
            Artist = result[0].Trim();
            Title = result[1].Trim();
        }
    }
}
