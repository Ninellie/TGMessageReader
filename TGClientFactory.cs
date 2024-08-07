using WTelegram;

namespace MessageReader;

public class TGClientFactory
{
    private readonly SemaphoreSlim _loginEvent = new(0, 1);

    private readonly IConfiguration _telegramConfig;
    private string _verificationCode = "";
    private Client _client;
    private readonly bool _firstLaunch;

    private bool _isLoggedIn;

    public TGClientFactory(IConfiguration configuration)
    {
        _telegramConfig = configuration.GetRequiredSection("TelegramConfig");
        _firstLaunch = (bool)configuration.GetValue(typeof(bool), "FirstLaunch")!;
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
            return Task.Delay(10);
        }
        if (!_firstLaunch)
        {
            return Login();
        }
        return _loginEvent.WaitAsync();
    }

    public async Task SetVerificationCode(string code)
    {
        if (!_firstLaunch)
        {
            return;
        }
        _verificationCode = code;
        await Login();
    }

    public async Task Login()
    {
        if (_isLoggedIn)
        {
            return;
        }

        await _client.LoginUserIfNeeded();
        _loginEvent.Release(1);
        _isLoggedIn = true;
    }

    private string What(string what)
    {
        if (what != "verification_code")
        {
            return _telegramConfig[what];
        }
        return _verificationCode;
    }
}