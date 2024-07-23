using System.Text.RegularExpressions;
using TL;
using Microsoft.Recognizers.Text.NumberWithUnit;
using Microsoft.Recognizers.Text;

namespace MessageReader;

public class MessageData
{
    public string Content => Message.message ?? "";
    public int Id => Message.ID;
    public DateTime Date => Message.Date;
    public string PostAuthor { get; }
    public string ChatTitle { get; }
    public Message Message { get; }
    public double Price { get; }

    public MessageData(Message message, string chatTitle, string postAuthor, bool writeLogs = false)
    {
        Message = message;
        ChatTitle = chatTitle;
        PostAuthor = postAuthor;
        Price = ExtractPrice(Content);
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

    public double ExtractPrice(string text)
    {
        double price = 0;
        var results = NumberWithUnitRecognizer.RecognizeCurrency(text, Culture.English);
        results.AddRange(NumberWithUnitRecognizer.RecognizeCurrency(text, Culture.Bulgarian));
        if (results.Count == 0)
        {
            var pricesRegex = ExtractPricesRegex(text);

            foreach (var stringPrice in pricesRegex)
            {
                if (!double.TryParse(stringPrice, out var priceValue)) continue;
                if (priceValue > price)
                {
                    price = priceValue;
                }
            }

            return price;
        }

        foreach (var result in results)
        {
            if (result.Resolution == null)
            {
                continue;
            }

            if (result.Resolution["value"] == null)
            {
                continue;
            }
            var value = result.Resolution["value"].ToString();
            if (!double.TryParse(value, out var val)) continue;
            if (val > price)
            {
                price = val;
            }
        }

        return price;
    }

    public static IEnumerable<string> ExtractPricesRegex(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        var t = text.Trim().Trim('.').Trim(',');

        // Регулярное выражение для поиска цен
        var pricePattern = new Regex(@"(\d+)\s?[:\-]?\s?(?:€|eur|euro|евро)\b", RegexOptions.IgnoreCase);

        // Найти все цены в тексте
        var matches = pricePattern.Matches(t);

        var prices = new List<string>();

        foreach (Match match in matches)
        {
            var numericPart = match.Groups[1].Value;
            prices.Add(numericPart);
        }

        return prices;
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
            var hashtag = match.Value;
            hashtag = hashtag.Trim('#');

            if (hashtag.Length < 3)
            {
                continue;
            }

            if (int.TryParse(hashtag, out var number))
            {
                continue;
            }

            var formattedHashtag = hashtag[..1].ToUpperInvariant() + hashtag[1..].ToLowerInvariant();
            if (formattedHashtag.StartsWith("Прода"))
            {
                formattedHashtag = "Продажа";
            }if (formattedHashtag.StartsWith("Аренд"))
            {
                formattedHashtag = "Аренда";
            }if (formattedHashtag.StartsWith("Сда"))
            {
                formattedHashtag = "Аренда";
            }if (formattedHashtag.StartsWith("Рент"))
            {
                formattedHashtag = "Аренда";
            }if (formattedHashtag.StartsWith("Сни"))
            {
                formattedHashtag = "Сниму";
            }

            uniqueHashtags.Add(formattedHashtag);
        }

        return uniqueHashtags.ToArray();
    }
}