using System.Threading.Tasks;
using System.Windows.Input;

namespace SongRecognizer.Commands
{
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync();
        bool CanExecute();
    }
}
