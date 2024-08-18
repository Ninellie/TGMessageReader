using Telegram.Bot.Types;
using WTelegram;

namespace MessageReader;

public class ScanRequestHandler : IRequestHandler
{
    private readonly Bot _bot;
    private readonly ILogger<ScanRequestHandler> _logger;
    private readonly ScanTaskQueue _scanQueue;

    public ScanRequestHandler(Bot bot, ILogger<ScanRequestHandler> logger, ScanTaskQueue scanQueue)
    {
        _bot = bot;
        _logger = logger;
        _scanQueue = scanQueue;
    }

    public async Task Handle(Chat chat, string requestString)
    {
        if (TryParseScanRequest(requestString, out var scanTasks, chat))
        {
            foreach (var task in scanTasks)
            {
                _scanQueue.Enqueue(task);
            }

            await _bot.SendTextMessage(chat, "Scan tasks have been queued. Scans will be completed soon.");
        }
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

    private static string ParseGroupName(string groupName)
    {
        return groupName.StartsWith("@") ? groupName.Remove(0, 1) : groupName;
    }

    private static bool IsValidString(string text)
    {
        return !string.IsNullOrEmpty(text) && !string.IsNullOrWhiteSpace(text);
    }
}