using Telegram.Bot.Types;
using WTelegram;

namespace MessageReader;

public class GroupsRequestHandler : IRequestHandler
{
    private readonly Bot _bot;
    private readonly IConfiguration _configuration;

    public GroupsRequestHandler(Bot bot, IConfiguration configuration)
    {
        _bot = bot;
        _configuration = configuration;
    }

    public async Task Handle(Chat chat, string requestString)
    {
        var section = _configuration.GetSection("ScanConfig");
        var groupsSection = section.GetSection("Groups").AsEnumerable();

        var groups = string.Empty;

        foreach (var group in groupsSection)
        {
            if (group.Value == null) continue;

            groups += group.Value;
            groups += "\n";
        }

        await _bot.SendTextMessage(chat, groups);
    }
}