using System.Globalization;
using Microsoft.Extensions.Logging;
using TL;
using WTelegram;

namespace MessageReader;

public class TelegramGroupHistoryGetter
{
    private readonly Client _client;
    private readonly ILogger<TelegramGroupHistoryGetter> _logger;
    private const int UserGetterDelay = 2;
    private const int MessageGetterDelay = 5;

    public TelegramGroupHistoryGetter(Client client, ILogger<TelegramGroupHistoryGetter> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<List<MessageData>> GetMessagesInDateRange(string groupMainUsername, DateTime olderThan, DateTime newerThan, int? oldestId = null)
    {
        _logger.LogInformation("___________________________________________________\n");
        _logger.LogInformation($"Getting messages from {groupMainUsername}");
        var messageDataList = new List<MessageData>();
        var chats = await _client.Messages_GetAllChats();
        if (chats == null)
        {
            _logger.LogWarning($"Chats of user {_client.User.username} not found");
            return messageDataList;
        }

        var groups = chats.chats.Values.Where(x => x.IsGroup).ToArray();
        var group = groups.FirstOrDefault(x => x.MainUsername == groupMainUsername);

        if (group == null)
        {
            _logger.LogWarning($"Group with main username: {groupMainUsername} not found in chat list of user {_client.User.username}");
            return messageDataList;
        }

        _logger.LogInformation($"Scanning group: \n" +
                               $"Group info:\n" +
                               $"Title: {group.Title}\n" +
                               $"MainUsername: {group.MainUsername}\n" +
                               $"ID: {group.ID}");
        
        var messagesBaseOlderThan = await _client.Messages_GetHistory(group, limit: 1, offset_date: olderThan);
        var newestId = messagesBaseOlderThan.Messages[0].ID;
        var messagesBaseNewerThan = await _client.Messages_GetHistory(group, limit: 1, offset_date: newerThan);
        oldestId ??= messagesBaseNewerThan.Messages[0].ID;
        var expected = newestId - oldestId.Value;

        _logger.LogInformation($"OldestId: {oldestId}. NewestId: {newestId}, Excepting {newestId-oldestId} base messages");

        if (expected == 0)
        {
            return messageDataList;
        }

        while (true)
        {
            var groupMessages = await _client.Messages_GetHistory(group, min_id: oldestId.Value, offset_date: olderThan);
            var first = groupMessages.Messages.FirstOrDefault();
            var firstDate = "";

            if (first != null)
            {
                firstDate = first.Date.ToString(CultureInfo.InvariantCulture);
            }

            _logger.LogInformation($"Received {groupMessages.Messages.Length} messages from {group.MainUsername}. " +
                                   $"offset id: {oldestId.Value}, " +
                                   $"First date: {firstDate}");

            var inputPeer = group.ToInputPeer();
            var end = false;
            foreach (var messageBase in groupMessages.Messages)
            {
                if (messageBase.ID >= newestId)
                {
                    end = true;
                    continue;
                }

                if (messageBase is not Message validMessage || string.IsNullOrEmpty(validMessage.message)) continue;

                await Task.Delay(TimeSpan.FromSeconds(UserGetterDelay));
                var userName = await GetUser(validMessage, inputPeer);
                messageDataList.Add(new MessageData(validMessage, groupMainUsername, userName));
                oldestId = validMessage.ID;
            }
            
            if (end)
            {
                break;
            }
            await Task.Delay(TimeSpan.FromSeconds(MessageGetterDelay));
        }
        _logger.LogInformation($"Valid messages received: {messageDataList.Count}");
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