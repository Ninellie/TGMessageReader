using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Bot = WTelegram.Bot;

namespace MessageReader;

public class BotScanTask
{
    public Message Message { get; set; }
    public string GroupName { get; set; }
    public DateTime OlderThan { get; set; }
    public DateTime NewerThan { get; set; }
}

public class TelegramGroupScannerBot : IHostedService
{
    private readonly Bot _bot;
    private readonly TelegramGroupScannerClient _client;
    private readonly NotionPageCreator _notionClient;
    private readonly ScanTaskHandler _scanTaskHandler;
    private readonly string[] _whiteList;

    public TelegramGroupScannerBot(Bot bot, TelegramGroupScannerClient client, NotionPageCreator notionClient, string[] whiteList, ScanTaskHandler scanTaskHandler)
    {
        _bot = bot;
        _client = client;
        _notionClient = notionClient;
        _whiteList = whiteList;
        _scanTaskHandler = scanTaskHandler;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("___________________________________________________\n");
        Console.WriteLine("I'm listening now. Send me a command in private or in a group where I am... Or press Escape to exit");
        await _bot.DropPendingUpdates();
        _bot.WantUnknownTLUpdates = true;

        for (int offset = 0; ;)
        {
            var updates = await _bot.GetUpdates(offset, 100, 1, Bot.AllUpdateTypes);
            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape) break;
            foreach (var update in updates)
            {
                try
                {
                    if (update.Message is { Text: { Length: > 0 } text } message)
                    {
                        if (_whiteList.Contains(message.From?.Username))
                        {
                            // commands accepted:
                            if (text == "/hello")
                            {
                                await _bot.SendTextMessage(message.Chat, $"Hello, {message.From}!");
                            }
                            else if (text.StartsWith("/scan"))
                            {
                                var botScanTask = new BotScanTask
                                {
                                    Message = message,
                                };
                                _scanTaskHandler.Enqueue(botScanTask);
                            }
                            else if (text == "/help")
                            {
                                await _bot.SendTextMessage(message.Chat, $"/help - get list of commands\n" +
                                                                         $"/scan/@groupName/number of hours from now - Send group name to scan for number of hours.\n)");
                            }
                            else
                            {
                                await _bot.SendTextMessage(message.Chat, $"Hello, {message.From}! Write /help for available commands.");
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
                            Console.WriteLine($"{udcm.messages.Length} message(s) deleted in {_bot.Chat(udcm.channel_id)?.Title}");
                        else if (update.TLUpdate is TL.UpdateDeleteMessages udm)
                            Console.WriteLine($"{udm.messages.Length} message(s) deleted in user chat or small private group");
                        else if (update.TLUpdate is TL.UpdateReadChannelOutbox urco)
                            Console.WriteLine($"Someone read {_bot.Chat(urco.channel_id)?.Title} up to message {urco.max_id}");
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

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}