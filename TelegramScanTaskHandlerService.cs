using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WTelegram;

namespace MessageReader;

public class TelegramScanTaskHandlerService : BackgroundService
{
    private readonly Bot _bot;
    private readonly TelegramGroupHistoryGetter _client;
    private readonly ScanTaskQueue _scanQueue;
    private readonly NotionPageCreateTaskQueue _notionQueue;
    private readonly ILogger<TelegramScanTaskHandlerService> _logger;

    public TelegramScanTaskHandlerService(Bot bot, TelegramGroupHistoryGetter client, ScanTaskQueue scanQueue,
        NotionPageCreateTaskQueue notionQueue, ILogger<TelegramScanTaskHandlerService> logger)
    {
        _bot = bot;
        _client = client;
        _scanQueue = scanQueue;
        _notionQueue = notionQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("___________________________________________________\n");
        _logger.LogInformation("Scan Task Handler Service just start");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            if (!_scanQueue.TryDequeue(out var task)) continue;
            var isReporting = task.Chat != null;
            if (isReporting)
            {
                if (task.Chat != null)
                {
                    await _bot.SendTextMessage(task.Chat, $"Scanning group {task.GroupName} just started.");
                }
            }
            await TryScan(task);
        }
    }

    private async Task TryScan(ScanTask task)
    {
        var groupName = task.GroupName;
        if (groupName == null)
        {
            return;
        }

        var messagesFromGroup = await _client.GetMessagesInDateRange(groupName, task.OlderThan, task.NewerThan);

        if (!messagesFromGroup.Any())
        {
            if (task.Chat != null)
            {
                await _bot.SendTextMessage(task.Chat, $"No messages were found in group {groupName} " +
                                                      $"between {task.NewerThan.ToShortTimeString()} " +
                                                      $"and {task.OlderThan.ToShortTimeString()}");
            }
            return;
        }

        if (task.Chat != null)
        {
            await _bot.SendTextMessage(task.Chat,
                $"{messagesFromGroup.Count()} messages were found in group {groupName} " +
                $"between {task.NewerThan.ToShortTimeString()} " +
                $"and {task.OlderThan.ToShortTimeString()}");

            await _bot.SendTextMessage(task.Chat, $"Sending {messagesFromGroup.Count()} messages to Notion");
        }

        foreach (var msg in messagesFromGroup)
        {
            _notionQueue.Enqueue(msg);
        }

        if (task.Chat != null)
        {
            await _bot.SendTextMessage(task.Chat, $"Success");
        }
    }
}