using System.Collections.Concurrent;

namespace MessageReader;

public class ScanTaskQueue
{
    private readonly ConcurrentQueue<ScanTask> _queue = new();

    public void Enqueue(ScanTask task)
    {
        _queue.Enqueue(task);
    }

    public void Enqueue(List<ScanTask> tasks)
    {
        foreach (var task in tasks)
        {
            _queue.Enqueue(task);
        }
    }

    public bool TryDequeue(out ScanTask scanTask)
    {
        return _queue.TryDequeue(out scanTask!);
    }
}