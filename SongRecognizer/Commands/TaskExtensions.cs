using System;
using System.Threading.Tasks;

namespace SongRecognizer.Commands
{
    public static class TaskExtensions
    {
        //#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
        public static async void FireAndForgetSafeAsync(this Task task, ErrorHandler errorHandler = null)
        //#pragma warning restore RECS0165
        {
            try
            {
                await task;
            }
            catch (Exception exception)
            {
                errorHandler?.Invoke(exception);
            }
        }
    }
}
