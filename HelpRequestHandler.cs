using Telegram.Bot.Types;
using WTelegram;

namespace MessageReader;

public class HelpRequestHandler : IRequestHandler
{
    private readonly Bot _bot;

    public HelpRequestHandler(Bot bot)
    {
        _bot = bot;
    }

    public async Task Handle(Chat chat, string requestString)
    {
        await _bot.SendTextMessage(chat, $"/help - get list of commands\n" +
                                         $"/scan/@groupName/number of hours from now - Send group name to scan for number of hours.\n " +
                                         $"/groups - list of groups for scan");
    }
}