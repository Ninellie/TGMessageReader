using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace MessageReader;

public class NotionPageCreateTaskQueue
{
    private readonly ConcurrentQueue<MessageData> _queue = new();
    private readonly IMemoryCache _memoryCache;

    public NotionPageCreateTaskQueue(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public void Enqueue(MessageData messageData)
    {
        if (_memoryCache.TryGetValue(messageData.Content, out var existing))
        {
            if (existing is not int id) return;
            if (id == messageData.Id)
            {
                Console.WriteLine("Попытка отправить в Notion сообщение с контентом," +
                                  " который уже был недавно добавлен. " +
                                  "Однако, Id у этих сообщений разные, что может указывать на то," +
                                  " что это одинаковое сообщение из разных групп или " +
                                  "повторяющееся сообщение из одной группы");
            }
            else
            {
                Console.WriteLine("Попытка отправить в Notion сообщение с контентом," +
                                  " который уже был недавно добавлен.");
            }
            return;
        }

        _queue.Enqueue(messageData);

        _memoryCache.Set(messageData.Content, messageData.Id, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(2),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4),
            Priority = CacheItemPriority.NeverRemove
        });
    }

    public bool TryDequeue(out MessageData messageData)
    {
        return _queue.TryDequeue(out messageData!);
    }
}