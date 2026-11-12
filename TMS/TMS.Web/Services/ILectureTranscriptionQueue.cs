using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TMS.Web.Services
{
    public interface ILectureTranscriptionQueue
    {
        void Queue(int lectureMaterialId);
        ValueTask<int> DequeueAsync(CancellationToken cancellationToken);
        bool IsQueued(int lectureMaterialId);
    }

    public class LectureTranscriptionQueue : ILectureTranscriptionQueue
    {
        private readonly Channel<int> _queue;
        private readonly ConcurrentDictionary<int, byte> _activeIds = new();

        public LectureTranscriptionQueue()
        {
            var options = new BoundedChannelOptions(500)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<int>(options);
        }

        public void Queue(int lectureMaterialId)
        {
            if (lectureMaterialId <= 0) return;

            // Deduplicate: only add if not already in queue
            if (_activeIds.TryAdd(lectureMaterialId, 0))
            {
                if (!_queue.Writer.TryWrite(lectureMaterialId))
                {
                    _activeIds.TryRemove(lectureMaterialId, out _);
                }
            }
        }

        public async ValueTask<int> DequeueAsync(CancellationToken cancellationToken)
        {
            int id = await _queue.Reader.ReadAsync(cancellationToken);
            _activeIds.TryRemove(id, out _);
            return id;
        }

        public bool IsQueued(int lectureMaterialId)
        {
            return _activeIds.ContainsKey(lectureMaterialId);
        }
    }
}
