using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Telegram.Bot.Types;
using WTelegram;

namespace MessageReader;

public class ScanTaskHandler : IHostedService
{
    private readonly Bot _bot;
    private readonly Client _client;
    private readonly ConcurrentQueue<BotScanTask> _queue = new();
    private Task _current;
    public ScanTaskHandler(Bot bot, Client client)
    {
        _bot = bot;
        _client = client;
    }

    public void Enqueue(BotScanTask task)
    {
        _queue.Enqueue(task);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        for (;;)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            if (_queue.TryDequeue(out var task))
            {
                await _bot.SendTextMessage(task.Message.Chat, "Hola");
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void ParseScanRequest(string requestString, BotScanTask scanTask, DateTime olderWhen)
    {
        var scanDepth = -1;
        var request = requestString.Split('/');
        var groupName = request[1];
        var depth = request[2];
        if (!string.IsNullOrEmpty(depth) || !string.IsNullOrWhiteSpace(depth))
        {
            scanDepth = int.Parse(depth) * -1;
        }

        scanTask.GroupName = groupName;
        scanTask.NewerThan
    }

    private async Task TryScan(string text, Message message)
    {
        var scanDepth = 1;
        var commandText = text.Remove(0, 5);
        var command = commandText.Split(" ");
        var groupName = command[0];
        var depth = int.Parse(command[1]) * -1;
        if (groupName.StartsWith("@"))
        {
            groupName = groupName.Remove(0, 1);
        }

        if (string.IsNullOrWhiteSpace(groupName))
        {
            await _bot.SendTextMessage(message.Chat, $"Group is empty");
            await _bot.DropPendingUpdates();
            return;
        }

        if (depth == 0)
        {
            await _bot.SendTextMessage(message.Chat, $"Scanning depth can not be 0. No messages will be scanned");
            await _bot.DropPendingUpdates();
            return;
        }

        var olderThan = DateTime.Now;
        var newerThan = DateTime.Now.AddHours(depth);
        var messagesFromGroup = await _client.GetMessagesFromGroup(groupName, olderThan, newerThan);

        if (messagesFromGroup != null && !messagesFromGroup.Any())
        {
            await _bot.SendTextMessage(message.Chat, $"No messages were found in group {groupName} " +
                                                     $"between {newerThan.ToShortTimeString()} " +
                                                     $"and {olderThan.ToShortTimeString()}");
            await _bot.DropPendingUpdates();
            return;
        }

        await _bot.SendTextMessage(message.Chat, $"{messagesFromGroup.Count()} messages were found in group {groupName} " +
                                                 $"between {newerThan.ToShortTimeString()} " +
                                                 $"and {olderThan.ToShortTimeString()}");

        await _bot.SendTextMessage(message.Chat, $"Sending {messagesFromGroup.Count()} messages to Notion");

        foreach (var msg in messagesFromGroup)
        {
            await _notionClient.CreateMessagePage(msg);
        }

        await _bot.SendTextMessage(message.Chat, $"Success");
    }
}