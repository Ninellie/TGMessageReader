using Microsoft.Extensions.Configuration;
using Notion.Client;

namespace MessageReader;

public class NotionPageCreator
{
    private readonly NotionClient _client;
    private readonly IConfiguration _configuration;

    public NotionPageCreator(IConfiguration config)
    {
        _configuration = config.GetRequiredSection("NotionConfig");
        _client = NotionClientFactory.Create(new ClientOptions
        {
            AuthToken = _configuration["AuthToken"],
            BaseUrl = _configuration["BaseUrl"],
            NotionVersion = _configuration["2022-06-28"]
        });
    }
    
    public async Task CreateMessagePage(MessageData data)
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
        if (!string.IsNullOrEmpty(data.PostAuthor))
        {
            builder.AddProperty("SenderUrl", GetUrlPropertyValue($"https://t.me/{data.PostAuthor}"));
        }
        var page = builder.Build();
        await _client.Pages.CreateAsync(page);
    }

    private ParagraphBlock GetPageContent(string content)
    {
        var paragraphBlock = new ParagraphBlock()
        {
            Paragraph = new ParagraphBlock.Info
            {
                RichText = new List<RichTextBase>
                {
                    new RichTextText
                    {
                        Text = new Text
                        {
                            Content = content
                        }
                    }
                }
            }
        };
        return paragraphBlock;
    }

    private DatePropertyValue GetDateProperty(DateTime date)
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

    private SelectPropertyValue GetSelectPropertyValue(string tag)
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
    
    private MultiSelectPropertyValue GetMultiSelectPropertyValue(IEnumerable<string> tags)
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

    private UrlPropertyValue GetUrlPropertyValue(string url)
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