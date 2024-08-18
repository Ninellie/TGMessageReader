using Telegram.Bot.Types;
using WTelegram;

namespace MessageReader;

public class NotAllowedRequestHandler : IRequestHandler
{
    private readonly Bot _bot;

    public NotAllowedRequestHandler(Bot bot)
    {
        _bot = bot;
    }

    public async Task Handle(Chat chat, string requestString)
    {
        await _bot.SendTextMessage(chat, $"Hello, {chat.Username}! You are not allowed");
    }
}