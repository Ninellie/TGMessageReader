namespace MessageReader;

public class ScanTaskProvider
{
    /// <summary>
    /// Вовзращает лист задач на сканирование всего вчерашнего дня
    /// </summary>
    /// <param name="group"></param>
    /// <returns></returns>
    public List<ScanTask> GetYesterdayTasks(string group)
    {
        var tasks = ScanTask.GetScanTasks(group, 24, DateTime.Today);
        return tasks;
    }

    /// <summary>
    /// Возвращает задачи на сканирование временного периода текущего часа вчерашнего дня.
    /// </summary>
    /// <param name="group"></param>
    /// <returns></returns>
    public List<ScanTask> GetCurrentHourYesterdayTasks(string group)
    {
        var dateFrom = DateTime.Today.AddDays(-1).AddHours(DateTime.Now.Hour);
        var tasks = ScanTask.GetScanTasks(group, 1, dateFrom);
        return tasks;
    }
}