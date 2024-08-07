﻿namespace MessageReader;

public class ScanTimerService : BackgroundService
{
    private readonly ScanTaskQueue _scanTaskQueue;
    private readonly ILogger<ScanTimerService> _logger;
    private readonly IConfiguration _groups;
    private const int ScanIntervalInDays = 1;

    public ScanTimerService(IConfiguration config, ScanTaskQueue scanTaskQueue, ILogger<ScanTimerService> logger)
    {
        _scanTaskQueue = scanTaskQueue;
        _logger = logger;
        _groups = config.GetSection("ScanConfig").GetSection("Groups");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("___________________________________________________\n");
        _logger.LogInformation("Scan Timer Service start");
        while (!stoppingToken.IsCancellationRequested)
        {
            EnqueueDailyScanTasks();
            var timeToWait = DateTime.Today.AddDays(1) - DateTime.Now;
            await Task.Delay(timeToWait, stoppingToken);
        }
    }

    private void EnqueueDailyScanTasks()
    {
        foreach (var (key, value) in _groups.AsEnumerable())
        {
            if (value == null) continue;
            var tasks = ScanTask.GetScanTasks(value, ScanIntervalInDays * 24, DateTime.Today);
            foreach (var task in tasks)
            {
                _scanTaskQueue.Enqueue(task);
            }
        }
    }
}

public class ScanTaskProvider
{
    public List<ScanTask> GetYesterdayTasks(string group)
    {
        var tasks = ScanTask.GetScanTasks(group, 24, DateTime.Today);
        return tasks;
    }

    public List<ScanTask> GetHourTasksFromNowDayAgo(string group)
    {
        var tasks = ScanTask.GetScanTasks(group, 24, DateTime.Today);
        return tasks;
    }


}