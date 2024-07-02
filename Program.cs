using Microsoft.Extensions.Configuration;

namespace MessageReader;

public class Program
{
    private static TelegramGroupScanner _tgService;
    private static NotionPageCreator _notionService;
    private static IConfiguration _scanConfiguration;

    public static async Task Main(string[] args)
    {
        Console.WriteLine("Configuration");
        var configuration = new ConfigurationBuilder().AddJsonFile($"appsettings.json");
        var config = configuration.Build();
        _scanConfiguration = config.GetRequiredSection("ScanConfig").GetRequiredSection("Groups");
        Console.WriteLine("Creating Telegram Service");
        _tgService = new TelegramGroupScanner(config.GetRequiredSection("TelegramConfig"));
        Console.WriteLine("Logging in telegram client");
        await _tgService.Login();

        Console.WriteLine("Creating Notion Service");
        _notionService = new NotionPageCreator(config.GetRequiredSection("NotionConfig"));

        Console.WriteLine("Scanning");
        await HandleScan();
    }

    private static async Task HandleScan()
    {
        while (true)
        {
            var groupName = GetGroupName();

            var depth = GetScanDepth();

            var messages = await _tgService.GetMessagesFromGroup(groupName, DateTime.Now, DateTime.Now.AddHours(depth));

            if (messages != null && !messages.Any())
            {
                Console.WriteLine("Zero messages found");
                return;
            }

            foreach (var message in messages)
            {
                await _notionService.CreateMessagePage(message);
            }
        }
    }

    private static int GetScanDepth()
    {
        Console.WriteLine("Enter scanning depth");
        int depth = 0;
        while (depth == 0)
        {
            var line = Console.ReadLine();
            depth = Convert.ToInt32(line) * -1;
            if (depth == 0)
            {
                Console.WriteLine("Scanning depth can not be 0. No messages will be scanned");
            }
        }
        return depth;
    }

    private static string GetGroupName()
    {
        var groupName = "";
        var counter = 0;
        for (int i = 0; i < 100; i++)
        {
            var s = _scanConfiguration[i.ToString()];
            if (string.IsNullOrEmpty(s))
            {
                break;
            }
            counter++;
            Console.WriteLine($"{i} - {s}");
        }

        Console.WriteLine($"Enter group number");

        if (counter != 0)
        {
            var line = Console.ReadLine();
            if (line != null) groupName = _scanConfiguration[line];
        }

        while (string.IsNullOrEmpty(groupName) || string.IsNullOrWhiteSpace(groupName))
        {
            Console.WriteLine("Enter telegram group name for scan");
            groupName = Console.ReadLine();
            if (string.IsNullOrEmpty(groupName))
            {
                Console.WriteLine("Can not scan empty group");
            }
        }
        return groupName;
    }
}