﻿using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using WTelegram;

namespace MessageReader;

public class TelegramScanTaskHandlerService : IHostedService
{
    private readonly Bot _bot;
    private readonly TelegramGroupHistoryGetter _client;
    private readonly NotionPageCreator _notionClient;
    private readonly ConcurrentQueue<ScanTask> _queue = new();

    public TelegramScanTaskHandlerService(Bot bot, TelegramGroupHistoryGetter client, NotionPageCreator notionClient)
    {
        _bot = bot;
        _client = client;
        _notionClient = notionClient;
    }

    public void Enqueue(ScanTask task)
    {
        _queue.Enqueue(task);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        for (;;)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            if (!_queue.TryDequeue(out var task)) continue;
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

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
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
                await _bot.SendTextMessage(task.Chat, $"No messages were found in group {groupName} " +
                                                      $"between {task.NewerThan.ToShortTimeString()} " +
                                                      $"and {task.OlderThan.ToShortTimeString()}");
            
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
            await _notionClient.CreateMessagePage(msg);
        }

        if (task.Chat != null)
        {
            await _bot.SendTextMessage(task.Chat, $"Success");
        }
    }
}