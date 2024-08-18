using Bot = WTelegram.Bot;
using System.Text.RegularExpressions;
using TL;

namespace MessageReader;

public class TelegramBotUpdateHandler : BackgroundService
{
    private readonly Bot _bot;
    private readonly ILogger<TelegramBotUpdateHandler> _logger;
    private readonly TGClientFactory _clientFactory;
    private readonly RequestHandlerProvider _handlerProvider;

    public TelegramBotUpdateHandler(Bot bot, ILogger<TelegramBotUpdateHandler> logger,
        TGClientFactory clientFactory, RequestHandlerProvider handlerProvider)
    {
        _bot = bot;
        _logger = logger;
        _clientFactory = clientFactory;
        _handlerProvider = handlerProvider;
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
                    if (update.Message is { Text: { Length: > 0 } text, From.Username: { } } message)
                    {
                        var handler = _handlerProvider.GetHandleRequest(message);
                        await handler.Handle(message.Chat, text);
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
}