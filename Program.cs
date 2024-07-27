using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WTelegram;

namespace MessageReader;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var fileLoggerProvider = new FileLoggerProvider("log.txt");
        builder.Logging.AddProvider(fileLoggerProvider);

        var bot = CreateBot(builder.Configuration);
        var client = await CreateClient(builder.Configuration);
        builder.Services.AddSingleton(bot);
        builder.Services.AddSingleton(client);
        builder.Services.AddSingleton<ScanTaskQueue>();
        builder.Services.AddSingleton<NotionPageCreateTaskQueue>();
        builder.Services.AddSingleton<TelegramGroupHistoryGetter>();

        builder.Services.AddHostedService<NotionPageCreator>();
        builder.Services.AddHostedService<TelegramUpdateGetterBot>();
        builder.Services.AddHostedService<TelegramScanTaskHandlerService>();
        builder.Services.AddHostedService<ScanTimerService>();
        builder.Services.AddMemoryCache();

        var app = builder.Build();
        await app.RunAsync();
    }

    private static async Task<Client> CreateClient(IConfiguration configuration)
    {
        var telegramConfig = configuration.GetRequiredSection("TelegramConfig");
        var client = new Client(what =>
        {
            if (what != "verification_code") return telegramConfig[what];
            Console.Write("Code: ");
            return Console.ReadLine();
        });
        var myself = await client.LoginUserIfNeeded();
        Console.WriteLine($"Logged-in as {myself} (id {myself.id})");
        return client;
    }
    private static Bot CreateBot(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");
        var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
        var telegramConfig = configuration.GetRequiredSection("TelegramConfig");
        var botToken = telegramConfig["bot_token"]!;
        var apiId = int.Parse(telegramConfig["api_id"]!);
        var apiHash = telegramConfig["api_hash"]!;
        return new Bot(botToken, apiId, apiHash, connection);
    }
}