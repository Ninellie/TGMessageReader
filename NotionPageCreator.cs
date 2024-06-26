using Notion.Client;
using TL;

namespace MessageReader;

public class NotionPageCreator
{
    public string DatabaseID { get; set; }
        
    private readonly NotionClient _client;

    public NotionPageCreator(string databaseId)
    {
        DatabaseID = databaseId;
        _client = NotionClientFactory.Create(new ClientOptions
        {
            AuthToken = Environment.GetEnvironmentVariable("NotionOptions__AuthToken"),
            BaseUrl = "https://api.notion.com/v1/pages/",
            NotionVersion = "2022-06-28"
        });
    }
    
    public async Task CreateMessagePage(MessageData data)
    {
        var databaseParentInput = new DatabaseParentInput
        {
            DatabaseId = DatabaseID
        };
        var builder = PagesCreateParametersBuilder.Create(databaseParentInput);
        var createdPage = await _client.Pages.CreateAsync(builder.Build());

        var updatedProperties = createdPage.Properties;
        var date = updatedProperties["Date"] as DatePropertyValue;
        var group = updatedProperties["Group"] as SelectPropertyValue;
        var tags = updatedProperties["Tags"] as MultiSelectPropertyValue;
        var url = updatedProperties["URL"] as UrlPropertyValue;
        if (group != null) group.Select = new SelectOption() { Name = data.ChatTitle };
        if (date != null)
        {
            var d = new Date
            {
                Start = data.Date
            };
            date.Date = d;
        }
        if (tags != null)
        {
            foreach (var hashTag in data.ExtractHashtags())
            {
                var selectOption = new SelectOption
                {
                    Name = hashTag
                };
                tags.MultiSelect.Add(selectOption);
            }
        }

        if (url != null)
        {
            url.Url = $"https://t.me/{data.ChatTitle}/{data.Id}";
        }
            
        await _client.Pages.UpdatePropertiesAsync(createdPage.Id, updatedProperties);
    }

    private static RichTextBase GetRichTextBase(string text)
    {
        return new RichTextBase
        {
            PlainText = text,
            Annotations = new Annotations(),
            Href = "",
            Type = RichTextType.Text
        };
    }
        
    private static ParagraphBlock GetParagraphBlock(string text)
    {
        var block = new ParagraphBlock()
        {
            Paragraph = new ParagraphBlock.Info
            {
                RichText = new[]{ GetRichTextBase(text) }
            }
        };

        return block;
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