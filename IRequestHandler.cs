using Telegram.Bot.Types;

namespace MessageReader;

public interface IRequestHandler
{
    public Task Handle(Chat chat, string requestString);
}