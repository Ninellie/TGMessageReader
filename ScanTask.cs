using Telegram.Bot.Types;

namespace MessageReader;

public class ScanTask
{
    public Chat? Chat { get; set; }
    public string? GroupName { get; set; }
    public DateTime OlderThan { get; set; }
    public DateTime NewerThan { get; set; }
}