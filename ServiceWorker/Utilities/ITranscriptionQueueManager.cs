using System.Threading;
using System.Threading.Tasks;

namespace ServiceWorker.Utilities
{
    public interface ITranscriptionQueueManager
    {
        Task ProcessFilesAsync(CancellationToken cancellationToken);
    }
}