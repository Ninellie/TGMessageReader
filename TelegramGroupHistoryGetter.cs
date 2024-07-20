using TL;
using WTelegram;

namespace MessageReader;

public class TelegramGroupHistoryGetter
{
    private readonly Client _client;

    public TelegramGroupHistoryGetter(Client client)
    {
        _client = client;
    }

    public async Task<IEnumerable<MessageData>> GetMessagesInDateRange(string groupMainUsername, DateTime olderThan, DateTime newerThan)
    {
        Console.WriteLine($"Getting messages from {groupMainUsername}");
        var messageDataList = new HashSet<MessageData>();
        var chats = await _client.Messages_GetAllChats();
        if (chats == null)
        {
            Console.WriteLine($"Chats of user {_client.User.username} not found");
            return messageDataList;
        }

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
        
        var validMessages = new HashSet<Message>();
        var messageBaseList = new HashSet<MessageBase>();
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
            var userName = await GetUser(msg, group.ToInputPeer());
            var data = new MessageData(msg.message, msg.ID, msg.Date, userName, groupMainUsername); 
            messageDataList.Add(data);
        }
        
        Console.WriteLine($"Messages received: {messageBaseList.Count}. Valid messages: {validMessages.Count}");
        return messageDataList;
    }

    //private void HandleMessagesChatsLogs(Messages_Chats chats)
    //{
    //    Console.WriteLine($"Number of chats found: {chats.chats.Count}");
    //    var channelsCount = chats.chats.Count(x => x.Value.IsChannel);
    //    var groupsCount = chats.chats.Count(x => x.Value.IsGroup);
    //    var activeChatsCount = chats.chats.Count(x => x.Value.IsActive);
    //    Console.WriteLine($"Number of channels: {channelsCount}");
    //    Console.WriteLine($"Number of groups: {groupsCount}");
    //    Console.WriteLine($"Number of active chats: {activeChatsCount}");
    //    foreach (var (key, chatBase) in chats.chats)
    //    {
    //        Console.WriteLine($"Chat Title: {chatBase.Title}," +
    //                          $"Chat ID: {chatBase.ID}, " +
    //                          $"Chat Main Username: {chatBase.MainUsername}, " +
    //                          $"IsGroup: {chatBase.IsGroup}, " +
    //                          $"IsChannel: {chatBase.IsChannel}, " +
    //                          $"IsActive: {chatBase.IsActive}");
    //    }
    //}

    private async Task<string> GetUser(Message message, InputPeer chatId)
    {
        var inputUserFromMessage = new InputUserFromMessage
        {
            msg_id = message.id,
            peer = chatId,
            user_id = message.from_id.ID
        };
        var userInfo = await _client.Users_GetFullUser(inputUserFromMessage);
        return userInfo.users.FirstOrDefault().Value.MainUsername;
    }
}