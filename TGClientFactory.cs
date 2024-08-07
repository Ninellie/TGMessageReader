using WTelegram;

namespace MessageReader;

public class TGClientFactory
{
    private readonly SemaphoreSlim _loginEvent = new(0, 2);

    private readonly IConfiguration _telegramConfig;
    private Client _client;
    private Bot _bot;
    private bool _isLogging;
    private bool _isLoggedIn;

    private readonly Dictionary<string, string?> _whiteList;

    public TGClientFactory(IConfiguration configuration, Bot bot)
    {
        _whiteList = configuration.GetSection("ScanConfig").GetSection("AllowedClients").AsEnumerable().ToDictionary();
        _telegramConfig = configuration.GetRequiredSection("TelegramConfig");
        _bot = bot;
    }

    public Client Create()
    {
        _client = new Client(What);
        return _client;
    }

    public Task WaitAsync()
    {
        if (_isLoggedIn)
        {
            return Task.Delay(100);
        }
        if (_isLogging)
        {
            return _loginEvent.WaitAsync();
        }

        _isLogging = true;

        return Login();
    }

    public async Task Login()
    {
        if (_isLoggedIn)
        {
            return;
        }

        await _client.LoginUserIfNeeded();
        _loginEvent.Release(2);
        _isLoggedIn = true;
    }

    private string What(string what)
    {
        if (what != "verification_code")
        {
            return _telegramConfig[what];
        }

        return GetCode().Result;
    }

    private async Task<string> GetCode()
    {
        await _bot.DropPendingUpdates();

        _bot.WantUnknownTLUpdates = true;

        var offset = 0;

        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            var updates = await _bot.GetUpdates(offset, 100, 1, Bot.AllUpdateTypes);
            foreach (var update in updates)
            {
                try
                {
                    if (update.Message is not { Text: { Length: > 0 } text } message) continue;

                    var username = message.From?.Username;

                    if (username == null || !_whiteList.ContainsValue(username)) continue;

                    if (!text.StartsWith("init")) continue;

                    var code = text.Remove(0, 4);

                    await _bot.SendTextMessage(message.Chat, $"Hello, {message.From}! Code received. {code}");
                    return code;
                }
                catch (Exception)
                {
                    // ignored
                }
                offset = updates[^1].Id + 1;
            }
        }
    }
}