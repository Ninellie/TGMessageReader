using Telegram.Bot.Types;

namespace MessageReader;

public class ScanTask
{
    public Chat? Chat { get; set; }
    public string? GroupName { get; set; }
    public DateTime OlderThan { get; set; }
    public DateTime NewerThan { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="groupName">Name of telegram group for scanning</param>
    /// <param name="interval">Total interval of list of scan tasks in hours</param>
    /// <param name="dateFrom">Returned scan tasks will be only before that date</param>
    /// <param name="scanTaskInterval">In hours</param>
    /// <param name="chat">Telegram responding chat</param>
    /// <returns></returns>
    public static List<ScanTask> GetScanTasks(string groupName, int interval, DateTime dateFrom, int scanTaskInterval = 1, Chat? chat = null)
    {
        var tasks = new List<ScanTask>();
        if (groupName.StartsWith("@"))
        {
            groupName = groupName.Remove(0, 1);
        }
        for (int i = 0; i < interval; i++)
        {
            var scanTask = new ScanTask
            {
                GroupName = groupName,
                NewerThan = dateFrom.AddHours(i - interval),
                OlderThan = dateFrom.AddHours(i + scanTaskInterval - interval),
                Chat = chat
            };
            tasks.Add(scanTask);
        }

        return tasks;
    }
}