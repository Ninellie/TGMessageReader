using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WTelegram;

namespace MessageReader;

public class Program
{
    private static TelegramGroupScannerClient _tgClient;
    private static NotionPageCreator _notionService;
    private static TelegramGroupScannerBot _tgBotService;
    private static IConfiguration _configuration;
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var connection = new Microsoft.Data.Sqlite.SqliteConnection(@"Data Source=WTelegramBot.sqlite");
        var bot = new Bot(Config("bot_token")!, int.Parse(Config("api_id")!), Config("api_hash")!, connection);
        builder.Services.AddSingleton(bot);
        builder.Services.AddHostedService<TelegramGroupScannerBot>();
        builder.Services.AddHostedService<ScanTaskHandler>();
        var app = builder.Build();
        await app.RunAsync();
        

        Console.WriteLine("Configuration");
        var configuration = new ConfigurationBuilder().AddJsonFile($"appsettings.json");
        var config = configuration.Build();

        // Получение конфигурации для телеграма
        _configuration = config.GetRequiredSection("TelegramConfig");
        var whiteList = config.GetRequiredSection("AllowedHosts").Value?.Split(",")!;

        _tgClient = new TelegramGroupScannerClient(_configuration);
        await _tgClient.Login();

        _notionService = new NotionPageCreator(config.GetRequiredSection("NotionConfig"));
    }

    private static string? Config(string what)
    {
        switch (what)
        {
            case "api_id": return _configuration["api_id"];
            case "bot_token": return _configuration["bot_token"];
            case "api_hash": return _configuration["api_hash"];
            case "phone_number": return _configuration["phone_number"];
            case "verification_code": Console.Write("Code: "); return Console.ReadLine();
            case "first_name": return _configuration["first_name"];      // if sign-up is required
            case "last_name": return _configuration["last_name"];        // if sign-up is required
            case "password": return _configuration["password"];     // if user has enabled 2FA
            default: return null;                  // let WTelegramClient decide the default config
        }
    }
}