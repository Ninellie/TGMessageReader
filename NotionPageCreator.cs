using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notion.Client;

namespace MessageReader;

public class NotionPageCreator : BackgroundService
{
    private readonly NotionPageCreateTaskQueue _queue;
    private readonly ILogger<NotionPageCreator> _logger;
    private readonly NotionClient _client;
    private readonly IConfiguration _configuration;

    public NotionPageCreator(IConfiguration config, NotionPageCreateTaskQueue queue, ILogger<NotionPageCreator> logger)
    {
        _queue = queue;
        _logger = logger;
        _configuration = config.GetRequiredSection("NotionConfig");
        _client = NotionClientFactory.Create(new ClientOptions
        {
            AuthToken = _configuration["AuthToken"],
            BaseUrl = _configuration["BaseUrl"],
            NotionVersion = _configuration["2022-06-28"]
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("___________________________________________________\n");
        _logger.LogInformation("Notion Page Creator Service start");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            if (_queue.TryDequeue(out var msg))
            {
                await CreateMessagePage(msg, stoppingToken);
            }
        }
    }

    private async Task CreateMessagePage(MessageData data, CancellationToken stoppingToken)
    {

        var databaseParentInput = new DatabaseParentInput
        {
            DatabaseId = _configuration["DatabaseId"]
        };
        var builder = PagesCreateParametersBuilder.Create(databaseParentInput);
        builder.AddPageContent(GetPageContent(data.Content));
        builder.AddProperty("Date", GetDateProperty(data.Date));
        builder.AddProperty("Group", GetSelectPropertyValue(data.ChatTitle));
        builder.AddProperty("URL", GetUrlPropertyValue($"https://t.me/{data.ChatTitle}/{data.Id}"));
        builder.AddProperty("Tags", GetMultiSelectPropertyValue(data.ExtractHashtags()));
        builder.AddProperty("Price", GetNumberProperty(data.Price));
        
        if (!string.IsNullOrEmpty(data.PostAuthor))
        {
            builder.AddProperty("SenderUrl", GetUrlPropertyValue($"https://t.me/{data.PostAuthor}"));
        }
        var page = builder.Build();
        try
        {
            await _client.Pages.CreateAsync(page, stoppingToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e); 
            throw;
        }
    }

    private ParagraphBlock GetPageContent(string content)
    {
        var richTextList = new List<RichTextBase>();
        const int richTextMaxLength = 2000;

        for (int i = 0; i < content.Length;)
        {
            if (string.IsNullOrEmpty(content))
            {
                break;
            }

            var text = content.Substring(i, int.Min(richTextMaxLength, content.Length - i));

            var richText = new RichTextText
            {
                Text = new Text
                {
                    Content = text
                }
            };

            richTextList.Add(richText);

            i += text.Length;
        }

        var paragraphBlock = new ParagraphBlock
        {
            Paragraph = new ParagraphBlock.Info
            {
                RichText = richTextList
            }
        };

        return paragraphBlock;
    }

    private static DatePropertyValue GetDateProperty(DateTime date)
    {
        var dateProperty = new DatePropertyValue
        {
            Date = new Date
            {
                Start = date.Date
            }
        };
        return dateProperty;
    }

    private static NumberPropertyValue GetNumberProperty(double number)
    {
        var numberProperty = new NumberPropertyValue
        {
            Number = number
        };
        return numberProperty;
    }

    private static SelectPropertyValue GetSelectPropertyValue(string tag)
    {
        var selectProperty = new SelectPropertyValue
        {
            Select = new SelectOption
            {
                Name = tag
            }
        };
        return selectProperty;
    }
    
    private static MultiSelectPropertyValue GetMultiSelectPropertyValue(IEnumerable<string> tags)
    {
        var multiSelectProperty = new MultiSelectPropertyValue
        {
            MultiSelect = new List<SelectOption>()
        };
        foreach (var tag in tags)
        {
            multiSelectProperty.MultiSelect.Add(new SelectOption
            {
                Name = tag
            });
        }

        return multiSelectProperty;
    }

    private static UrlPropertyValue GetUrlPropertyValue(string url)
    {
        var urlProperty = new UrlPropertyValue
        {
            Url = url
        };
        return urlProperty;
    }

    private static object? GetPropValue(PropertyValue p)
    {
        switch (p)
        {
            case TitlePropertyValue titlePropertyValue:
                return titlePropertyValue.Title.FirstOrDefault()!.PlainText;
            case RichTextPropertyValue richTextPropertyValue:
                return richTextPropertyValue.RichText.FirstOrDefault()?.PlainText;
            case NumberPropertyValue numberPropertyValue:
                return numberPropertyValue.Number!.Value;
            case SelectPropertyValue selectPropertyValue:
                return selectPropertyValue.Select.Name;
            case DatePropertyValue datePropertyValue:
                return datePropertyValue.Date.Start!.Value.ToShortDateString();
            case RelationPropertyValue relationPropertyValue:
                var relations = relationPropertyValue.Relation;
                var idList = new List<string>(relations.Count);
                idList.AddRange(relations.Select(objectId => objectId.Id));
                return idList;
            case FormulaPropertyValue formulaPropertyValue:
                return formulaPropertyValue.Formula.String;
            case RollupPropertyValue rollupPropertyValue:
                return rollupPropertyValue.Rollup.Number!.Value;
            default:
                return null;
        }
    }
}