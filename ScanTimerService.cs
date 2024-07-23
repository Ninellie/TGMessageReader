using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace MessageReader;

public class ScanTimerService : BackgroundService
{
    private readonly ScanTaskQueue _scanTaskQueue;
    private readonly IConfiguration _groups;
    private const int ScanIntervalInDays = 1;

    public ScanTimerService(IConfiguration config, ScanTaskQueue scanTaskQueue)
    {
        _scanTaskQueue = scanTaskQueue;
        _groups = config.GetSection("ScanConfig").GetSection("Groups");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("___________________________________________________\n");
        Console.WriteLine("Scan Timer Service start");
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var group in _groups.AsEnumerable())
            {
                if (group.Value == null) continue;
                var tasks = ScanTask.GetScanTasks(group.Value, ScanIntervalInDays * 24, DateTime.Today);
                foreach (var task in tasks)
                {
                    _scanTaskQueue.Enqueue(task);
                }
            }
            await Task.Delay(TimeSpan.FromDays(ScanIntervalInDays), stoppingToken);
        }
    }

}