using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WTelegram;

namespace MessageReader;

public class Program
{
    private static IConfiguration _configuration;
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonFile($"appsettings.json").Build();
        _configuration = builder.Configuration.GetRequiredSection("TelegramConfig");

        var connection = new Microsoft.Data.Sqlite.SqliteConnection(@"Data Source=WTelegramBot.sqlite");
        var bot = new Bot(Config("bot_token")!, int.Parse(Config("api_id")!), Config("api_hash")!, connection);

        var client = new Client(Config);
        var myself = await client.LoginUserIfNeeded();
        Console.WriteLine($"Logged-in as {myself} (id {myself.id})");

        builder.Services.AddSingleton(bot);
        builder.Services.AddSingleton(client);
        builder.Services.AddSingleton<ScanTaskQueue>();
        builder.Services.AddSingleton<NotionPageCreateTaskQueue>();
        builder.Services.AddSingleton<TelegramGroupHistoryGetter>();
        builder.Services.AddHostedService<NotionPageCreator>();
        builder.Services.AddHostedService<TelegramUpdateGetterBot>();
        builder.Services.AddHostedService<TelegramScanTaskHandlerService>();
        builder.Services.AddHostedService<ScanTimerService>();

        var app = builder.Build();
        await app.RunAsync();
    }

    private static string? Config(string what)
    {
        if (what != "verification_code") return _configuration[what];
        Console.Write("Code: ");
        return Console.ReadLine();
    }
}