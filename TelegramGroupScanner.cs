using TL;
using WTelegram;

namespace MessageReader;

public class TelegramGroupScanner
{
    private readonly Client _client;

    public TelegramGroupScanner()
    {
        _client = new Client(Config);
    }

    public async Task Login()
    {
        var myself = await _client.LoginUserIfNeeded();
        Console.WriteLine($"We are logged-in as {myself} (id {myself.id})");
    }

    public async Task<IEnumerable<MessageData>> GetMessagesFromGroup(string groupMainUsername, DateTime olderThan, DateTime newerThan)
    {
        var messageDataList = new List<MessageData>();
        var chats = await _client.Messages_GetAllChats();
        if (chats == null)
        {
            Console.WriteLine($"Chats of user {_client.User.username} not found");
            return messageDataList;
        }
        HandleMessagesChatsLogs(chats);
        var groups = chats.chats.Values.Where(x => x.IsGroup).ToArray();
        var group = groups.FirstOrDefault(x => x.MainUsername == groupMainUsername);
        if (group == null)
        { 
            Console.WriteLine($"Group with main username: {groupMainUsername} not found in chat list of user {_client.User.username}");
            return messageDataList;
        }
        Console.WriteLine($"Scanning group: {group.Title} ({group.MainUsername}) started");
        var messagesBaseOlderThan = await _client.Messages_GetHistory(group, limit: 1, offset_date: olderThan);
        var newestId = messagesBaseOlderThan.Messages[0].ID;
        var messagesBaseNewerThan = await _client.Messages_GetHistory(group, limit: 1, offset_date: newerThan);
        var oldestId = messagesBaseNewerThan.Messages[0].ID;
        
        var validMessages = new List<Message>();
        var messageBaseList = new List<MessageBase>();
        Console.WriteLine($"OldestId: {oldestId}. NewestId: {newestId}, Excepting {newestId-oldestId} base messages");
        var count = 0;
        while (oldestId < newestId)
        {
            count++;
            Console.WriteLine(count);
            var groupMessages = await _client.Messages_GetHistory(group, limit: 100, offset_id: oldestId, add_offset: -100);
            if (groupMessages.Messages.Length == 0)
            { 
                Console.WriteLine($"GroupMessages count: {groupMessages.Messages.Length}");
                return messageDataList;
            }
            Console.WriteLine($"GroupMessages count: {groupMessages.Count}");
            Console.WriteLine($"Messages count: {groupMessages.Messages.Length}");
            foreach (var messageBase in groupMessages.Messages.Reverse())
            {
                Console.WriteLine($"Current oldest ID: {oldestId}");
                Console.WriteLine($"Current message base ID: {messageBase.ID}");
                Console.WriteLine($"Current message base FROM Id: {messageBase.From.ID}");
                messageBaseList.Add(messageBase);
                oldestId = messageBase.ID;
                if (messageBase.ID > newestId) break;
            }
        }

        foreach (var messageBase in messageBaseList)
        {
            if (messageBase is not Message validMessage || string.IsNullOrEmpty(validMessage.message)) continue;
            validMessages.Add(validMessage);
        }
        
        foreach (var msg in validMessages)
        {
            var data = new MessageData(msg.message, msg.ID, msg.Date, msg.From.ID.ToString(), groupMainUsername); 
            messageDataList.Add(data);
        }
        
        Console.WriteLine($"Messages received: {messageBaseList.Count}. Valid messages: {validMessages.Count}");
        return messageDataList;
    }

    private void HandleMessagesChatsLogs(Messages_Chats chats)
    {
        Console.WriteLine($"Number of chats found: {chats.chats.Count}");
        var channelsCount = chats.chats.Count(x => x.Value.IsChannel);
        var groupsCount = chats.chats.Count(x => x.Value.IsGroup);
        var activeChatsCount = chats.chats.Count(x => x.Value.IsActive);
        Console.WriteLine($"Number of channels: {channelsCount}");
        Console.WriteLine($"Number of groups: {groupsCount}");
        Console.WriteLine($"Number of active chats: {activeChatsCount}");
        foreach (var (key, chatBase) in chats.chats)
        {
            Console.WriteLine($"Chat Title: {chatBase.Title}," +
                              $"Chat ID: {chatBase.ID}, " +
                              $"Chat Main Username: {chatBase.MainUsername}, " +
                              $"IsGroup: {chatBase.IsGroup}, " +
                              $"IsChannel: {chatBase.IsChannel}, " +
                              $"IsActive: {chatBase.IsActive}");
        }
    }

    private static string? Config(string what)
    {
        switch (what)
        {
            case "api_id": return "24307614";
            case "api_hash": return "59f352bb8429550d94687a56e6ab7af5";
            case "phone_number": return "+382 68132535";
            case "verification_code": Console.Write("Code: "); return Console.ReadLine();
            case "first_name": return "Alexander";      // if sign-up is required
            case "last_name": return "Pavlov";        // if sign-up is required
            case "password": return "secret!";     // if user has enabled 2FA
            default: return null;                  // let WTelegramClient decide the default config
        }
    }
}