namespace MessageReader;

public enum ScanMode
{
    EachDay,
    EachHour
}

public class ScanTimerService : BackgroundService
{
    private readonly ScanTaskQueue _scanTaskQueue;
    private readonly ILogger<ScanTimerService> _logger;
    private readonly List<string> _groups = new();
    private readonly ScanMode _mode;
    private readonly ScanTaskProvider _scanTaskProvider = new();
    private readonly bool _scanAtLaunch;
    public ScanTimerService(IConfiguration config, ScanTaskQueue scanTaskQueue, ILogger<ScanTimerService> logger)
    {
        _scanTaskQueue = scanTaskQueue;
        _logger = logger;

        var section = config.GetSection("ScanConfig");

        foreach (var group in section.GetSection("Groups").AsEnumerable())
        {
            if (group.Value != null) _groups.Add(group.Value);
        }

        var mode = section.GetValue<string>("Mode");

        if (mode == ScanMode.EachDay.ToString())
        {
            _mode = ScanMode.EachDay;
        }
        else
        {
            _mode = ScanMode.EachHour;
        }

        _scanAtLaunch = section.GetValue<bool>("ScanAtLaunch");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("___________________________________________________\n");
        _logger.LogInformation("Scan Timer Service start");

        if (_scanAtLaunch)
        {
            EnqueueScanTasks();
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var timeToWait = GetDelayTime();
            await Task.Delay(timeToWait, stoppingToken);
            EnqueueScanTasks();
        }
    }

    private void EnqueueScanTasks()
    {
        foreach (var groupName in _groups)
        {
            List<ScanTask> tasks;
            if (_mode == ScanMode.EachDay)
            {
                tasks = _scanTaskProvider.GetYesterdayTasks(groupName);
            }
            else
            {
                tasks = _scanTaskProvider.GetCurrentHourYesterdayTasks(groupName);
            }
            _scanTaskQueue.Enqueue(tasks);
        }
    }

    private TimeSpan GetDelayTime()
    {
        if (_mode == ScanMode.EachDay)
        {
            return DateTime.Today.AddDays(1) - DateTime.Now;
        }
        return DateTime.Today.AddHours(1) - DateTime.Now;
    }
}