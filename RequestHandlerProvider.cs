using Telegram.Bot.Types;

namespace MessageReader;

public class RequestHandlerProvider
{
    private readonly IConfiguration _whiteList;

    private readonly ScanRequestHandler _scanRequestHandler;
    private readonly HelpRequestHandler _helpRequestHandler;
    private readonly GroupsRequestHandler _groupsRequestHandler;
    private readonly NotAllowedRequestHandler _notAllowedRequestHandler;

    public RequestHandlerProvider(IConfiguration config,
        NotAllowedRequestHandler notAllowedRequestHandler,
        GroupsRequestHandler groupsRequestHandler,
        HelpRequestHandler helpRequestHandler,
        ScanRequestHandler scanRequestHandler)
    {
        _whiteList = config.GetSection("ScanConfig").GetSection("AllowedClients");
        _notAllowedRequestHandler = notAllowedRequestHandler;
        _groupsRequestHandler = groupsRequestHandler;
        _helpRequestHandler = helpRequestHandler;
        _scanRequestHandler = scanRequestHandler;
    }

    public IRequestHandler GetHandleRequest(Message message)
    {
        if (message.Text == null) return _helpRequestHandler;

        var text = message.Text.Trim();

        var whiteList = _whiteList.AsEnumerable().ToDictionary();

        if (!whiteList.ContainsValue(message.From?.Username)) return _notAllowedRequestHandler;

        if (text.StartsWith("/scan"))
        {
            return _scanRequestHandler;
        }

        if (text == "/groups")
        {
            return _groupsRequestHandler;
        }

        return _helpRequestHandler;
    }
}