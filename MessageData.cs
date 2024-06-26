using System.Text.RegularExpressions;

namespace MessageReader;

public class MessageData
{
    public string Content { get; }
    public int Id { get; }
    public DateTime Date { get; }
    public string PostAuthor { get; }
    public string ChatTitle { get; }

    public MessageData(string content, int id, DateTime date, string postAuthor, string chatTitle, bool writeLogs = true)
    {
        Content = content;
        Id = id;
        Date = date;
        PostAuthor = postAuthor;
        ChatTitle = chatTitle;
        if (writeLogs)
        {
            HandleCreatedMessageLogs();
        }
    }

    private void HandleCreatedMessageLogs()
    {
        Console.WriteLine("Created message with: ");
        Console.WriteLine($"ID: {Id}");
        Console.WriteLine($"Date: {Date}");
        Console.WriteLine($"Author: {PostAuthor}");
        Console.WriteLine($"From chat: {ChatTitle}");
        Console.WriteLine($"Content: {Content}");
    }
    
    public IEnumerable<string> ExtractHashtags()
    {
        if (string.IsNullOrWhiteSpace(Content))
        {
            return Array.Empty<string>();
        }

        // Регулярное выражение для поиска хештегов
        var hashtagPattern = new Regex(@"#\w+");

        // Найти все хештеги в сообщении
        var matches = hashtagPattern.Matches(Content);

        // Использовать HashSet для удаления повторяющихся значений
        var uniqueHashtags = new HashSet<string>();

        foreach (Match match in matches)
        {
            uniqueHashtags.Add(match.Value);
        }

        return uniqueHashtags.ToArray();
    }
}