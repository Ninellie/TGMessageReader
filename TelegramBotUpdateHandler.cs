using Telegram.Bot.Types.Enums;
using WTelegram;
using Chat = Telegram.Bot.Types.Chat;

namespace MessageReader;

public abstract class BotRequestHandler
{
    protected string RequestString { get; set; }

    protected readonly Bot bot;
    protected readonly Chat chat;

    protected BotRequestHandler(Bot bot, Chat chat, string requestString)
    {
        this.bot = bot;
        this.chat = chat;
        RequestString = requestString;
    }

    public abstract Task Handle(CancellationToken stoppingToken);
}

public class ScanHandler : BotRequestHandler
{
    public ScanHandler(Bot bot, Chat chat, string requestString) : base(bot, chat, requestString)
    {
    }

    public override Task Handle(CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }
}

public class TelegramBotUpdateHandler : BackgroundService
{
    private readonly Bot _bot;
    private readonly ScanTaskQueue _scanQueue;
    private readonly ILogger<TelegramBotUpdateHandler> _logger;
    private readonly Dictionary<string, string?> _whiteList;

    private readonly TGClientFactory _clientFactory;

    //private readonly List<BotRequestHandler> _requestHandlers = new();

    public TelegramBotUpdateHandler(Bot bot, IConfiguration config, ScanTaskQueue scanScanQueue,
        ILogger<TelegramBotUpdateHandler> logger, TGClientFactory clientFactory)
    {
        _bot = bot;
        _whiteList = config.GetSection("ScanConfig").GetSection("AllowedClients").AsEnumerable().ToDictionary();
        _scanQueue = scanScanQueue;
        _logger = logger;
        _clientFactory = clientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("___________________________________________________\n");
        _logger.LogInformation("Update Getter Bot start receiving bot updates");

        await _clientFactory.WaitAsync();

        await _bot.DropPendingUpdates();
        _bot.WantUnknownTLUpdates = true;
        var offset = 0;
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
                        var username = message.From?.Username;
                        if (username != null && _whiteList.ContainsValue(username))
                        {
                            if (text == "/hello")
                            {
                                await _bot.SendTextMessage(message.Chat, $"Hello, {message.From}!");
                            }
                            else if (text.StartsWith("/scan"))
                            {
                                if (TryParseScanRequest(text, out var scanTasks, message.Chat))
                                {
                                    // Если пользователь отправил команду скана, то отправить задачи в очередь на сканирование
                                    foreach (var task in scanTasks)
                                    {
                                        _scanQueue.Enqueue(task);
                                    }
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
                            _logger.LogInformation(
                                $"{udcm.messages.Length} message(s) deleted in {_bot.Chat(udcm.channel_id)?.Title}");
                        else if (update.TLUpdate is TL.UpdateDeleteMessages udm)
                            _logger.LogInformation(
                                $"{udm.messages.Length} message(s) deleted in user chat or small private group");
                        else if (update.TLUpdate is TL.UpdateReadChannelOutbox urco)
                            _logger.LogInformation(
                                $"Someone read {_bot.Chat(urco.channel_id)?.Title} up to message {urco.max_id}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("An error occured: " + ex.Message);
                }

                offset = updates[^1].Id + 1;
            }
        }
    }

    private static bool IsValidString(string text)
    {
        return !string.IsNullOrEmpty(text) && !string.IsNullOrWhiteSpace(text);
    }

    private bool TryParseScanRequest(string requestString, out List<ScanTask> scanTasks, Chat? chat = null)
    {
        scanTasks = new List<ScanTask>();

        if (!IsValidString(requestString))
        {
            if (chat != null)
            {
                _bot.SendTextMessage(chat, "request must be valid");
            }

            return false;
        }

        var request = requestString.Trim().Split('/');

        if (!IsValidScanCommand(request[1], chat)) return false;
        
        var groupName = ParseGroupName(request[2]);

        if (!IsValidGroupName(groupName, chat)) return false;

        //todo добавить туть проверку на существование группы... Но как?

        var scanDepth = -1;

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

        scanTasks = ScanTask.GetScanTasks(groupName, scanDepth, DateTime.Now, chat: chat);
        return true;
    }


    private bool IsValidScanCommand(string requestString, Chat? chat)
    {
        requestString.ToLower();
        if (requestString.StartsWith("scan")) return true;
        if (chat != null)
        {
            _logger.LogWarning("Scan request must starts with /scan");
        }

        return false;
    }

    private bool IsValidGroupName(string groupName, Chat? chat)
    {
        if (IsValidString(groupName)) return true;

        if (chat != null)
        {
            _bot.SendTextMessage(chat, $"Group name is empty");
        }

        return false;
    }

    private string ParseGroupName(string groupName)
    {
        return groupName.StartsWith("@") ? groupName.Remove(0, 1) : groupName;
    }
}