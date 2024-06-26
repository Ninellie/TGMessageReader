namespace MessageReader
{
    public class Program
    {
        private const string DataBaseId = "436346c3c70b415fa6e2c863deded62e";
        private const string GroupName = "Montenegro_sell_rent";
//Montenegro_sell_rent
//saleme_realty
        
        public static async Task Main(string[] args)
        {
            var tgService = new TelegramGroupScanner();
            await tgService.Login();
            var messages = await tgService.GetMessagesFromGroup(GroupName, DateTime.Now, DateTime.Now.AddHours(-1));
            //Console.WriteLine($"Send all messages to Notion? Y for continue");
            return;
            var isSendToNotion = Console.ReadLine();
            if (isSendToNotion != "Y")
            {
                return;
            }
            var notionService = new NotionPageCreator(DataBaseId);

            foreach (var message in messages)
            {
                await notionService.CreateMessagePage(message);
            }
        }
    }
}
