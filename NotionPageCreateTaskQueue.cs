using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MessageReader;

public class NotionPageCreateTaskQueue
{
    private readonly ConcurrentQueue<MessageData> _queue = new();
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<NotionPageCreateTaskQueue> _logger;
    private readonly IConfiguration _configuration;
    private const int SlidingExpirationHours = 2;
    private const int AbsoluteExpirationHours = 4;

    public NotionPageCreateTaskQueue(IMemoryCache memoryCache, ILogger<NotionPageCreateTaskQueue> logger, IConfiguration configuration)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _configuration = configuration;
    }

    public void Enqueue(MessageData messageData)
    {
        if (_memoryCache.TryGetValue(messageData.Content, out var existing))
        {
            if (existing is not int id) return;
            if (id == messageData.Id)
            {
                _logger.LogWarning("Попытка отправить в Notion сообщение с контентом," +
                                  " который уже был недавно добавлен. " +
                                  "Однако, Id у этих сообщений разные, что может указывать на то," +
                                  " что это одинаковое сообщение из разных групп или " +
                                  "повторяющееся сообщение из одной группы");
            }
            else
            {
                _logger.LogWarning("Попытка отправить в Notion сообщение с контентом," +
                                   " который уже был недавно добавлен.");
            }
            return;
        }

        _queue.Enqueue(messageData);

        var slidingExpirationHours = SlidingExpirationHours;
        var absoluteExpirationHours = AbsoluteExpirationHours;

        if (int.TryParse(_configuration.GetSection("Cache")["SlidingExpirationHours"], out var resultSliding))
        {
            slidingExpirationHours = resultSliding;
        }
        if (int.TryParse(_configuration.GetSection("Cache")["AbsoluteExpirationHours"], out var resultAbsolute))
        {
            absoluteExpirationHours = resultAbsolute;
        }

        _memoryCache.Set(messageData.Content, messageData.Id, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(slidingExpirationHours),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(absoluteExpirationHours),
            Priority = CacheItemPriority.NeverRemove
        });
    }

    public bool TryDequeue(out MessageData messageData)
    {
        return _queue.TryDequeue(out messageData!);
    }
}