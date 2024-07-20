using System.Collections.Concurrent;

namespace MessageReader;

public class NotionPageCreateTaskQueue
{
    private readonly ConcurrentQueue<MessageData> _queue = new();
    private readonly HashSet<string> _contentSet = new(500);
    private readonly Queue<string> _contentQueue = new(500);
    private const int MaxHashSetCount = 500;
    public void Enqueue(MessageData messageData)
    {
        if (_contentSet.Contains(messageData.Content)) return;
        _queue.Enqueue(messageData);
        _contentSet.Add(messageData.Content);
        _contentQueue.Enqueue(messageData.Content);
    }

    public bool TryDequeue(out MessageData messageData)
    {
        if (!_queue.TryDequeue(out messageData!)) return false;
        if (_contentSet.Count == MaxHashSetCount)
        {
            _contentSet.Remove(_contentQueue.Dequeue());
        }
        return true;
    }
}