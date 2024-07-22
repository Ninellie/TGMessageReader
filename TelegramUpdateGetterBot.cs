using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Telegram.Bot.Types.Enums;
using Bot = WTelegram.Bot;

namespace MessageReader;

public class TelegramUpdateGetterBot : BackgroundService
{
    private readonly Bot _bot;
    private readonly ScanTaskQueue _scanQueue;
    private readonly string[]? _whiteList;

    public TelegramUpdateGetterBot(Bot bot, IConfiguration config, ScanTaskQueue scanScanQueue)
    {
        _bot = bot;
        _whiteList = config.GetRequiredSection("AllowedHosts").Value?.Split(",");
        _scanQueue = scanScanQueue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("___________________________________________________\n");
        Console.WriteLine("Update Getter Bot start receiving bot updates");

        await _bot.DropPendingUpdates();
        _bot.WantUnknownTLUpdates = true;
        int offset = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            var updates = await _bot.GetUpdates(offset, 100, 1, Bot.AllUpdateTypes, stoppingToken);
            foreach (var update in updates)
            {
                try
                {
                    if (update.Message is { Text: { Length: > 0 } text } message)
                    {
                        if (_whiteList != null && _whiteList.Contains(message.From?.Username))
                        {
                            if (text == "/hello")
                            {
                                await _bot.SendTextMessage(message.Chat, $"Hello, {message.From}!");
                            }
                            else if (text.StartsWith("/scan"))
                            {
                                if (TryParseScanRequest(text, out var scanTask, message.Chat))
                                {
                                    // Если пользователь отправил команду скана, то отправить задачу в очередь на сканирование
                                    _scanQueue.Enqueue(scanTask);
                                }
                            }
                            else if (text == "/help")
                            {
                                await _bot.SendTextMessage(message.Chat, $"/help - get list of commands\n" +
                                                                         $"/scan/@groupName/number of hours from now - Send group name to scan for number of hours.\n)");
                            }
                            else
                            {
                                await _bot.SendTextMessage(message.Chat,
                                    $"Hello, {message.From}! Write /help for available commands.");
                            }
                        }
                        else
                        {
                            await _bot.SendTextMessage(message.Chat, $"Hello, {message.From}! You are not allowed");
                        }
                    }
                    else if (update.Type == UpdateType.Unknown)
                    {
                        //---> Show some update types that are unsupported by Bot API but can be handled via TLUpdate
                        if (update.TLUpdate is TL.UpdateDeleteChannelMessages udcm)
                            Console.WriteLine(
                                $"{udcm.messages.Length} message(s) deleted in {_bot.Chat(udcm.channel_id)?.Title}");
                        else if (update.TLUpdate is TL.UpdateDeleteMessages udm)
                            Console.WriteLine(
                                $"{udm.messages.Length} message(s) deleted in user chat or small private group");
                        else if (update.TLUpdate is TL.UpdateReadChannelOutbox urco)
                            Console.WriteLine(
                                $"Someone read {_bot.Chat(urco.channel_id)?.Title} up to message {urco.max_id}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occured: " + ex.Message);
                }

                offset = updates[^1].Id + 1;
            }
        }
    }

    private static bool IsValidString(string text)
    {
        return !string.IsNullOrEmpty(text) && !string.IsNullOrWhiteSpace(text);
    }

    private bool TryParseScanRequest(string requestString, out ScanTask scanTask, Telegram.Bot.Types.Chat? chat = null)
    {
        scanTask = new ScanTask();

        if (!IsValidString(requestString))
        {
            if (chat != null)
            {
                _bot.SendTextMessage(chat, "request must be valid");
            }
            return false;
        }

        var request = requestString.Trim().Split('/');
        request[1] = request[1].ToLower();
        if (!request[1].StartsWith("scan"))
        {
            if (chat != null)
            {
                _bot.SendTextMessage(chat, "request must starts with /scan");
            }
            return false;
        }

        var scanDepth = -1;
        var olderThan = DateTime.Now;

        var groupName = request[2];
        if (groupName.StartsWith("@"))
        {
            groupName = groupName.Remove(0, 1);
        }

        if (!IsValidString(groupName))
        {
            if (chat != null)
            {
                _bot.SendTextMessage(chat, $"Group name is empty");
            }
            return false;
        }

        //todo добавить туть проверку на существование группы

        if (request.Length >= 3)
        {
            var depth = request[3];
            if (!string.IsNullOrEmpty(depth) && !string.IsNullOrWhiteSpace(depth))
            {
                scanDepth = int.Parse(depth) * -1;
            }

            if (scanDepth == 0)
            {
                if (chat != null)
                {
                    _bot.SendTextMessage(chat, $"Scanning depth can not be 0. No messages will be scanned");
                }
                return false;
            }
        }

        scanTask.GroupName = groupName;
        scanTask.OlderThan = olderThan;
        scanTask.NewerThan = olderThan.AddHours(scanDepth);
        scanTask.Chat = chat;

        return true;
    }
}