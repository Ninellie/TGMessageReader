using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace MessageReader;

public class ScanTimerService : BackgroundService
{
    private readonly ScanTaskQueue _scanTaskQueue;
    private readonly IConfiguration _groups;
    private const int MaxTaskScanInterval = 1;

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
            var now = DateTime.Now;
            foreach (var group in _groups.AsEnumerable())
            {
                var groupName = group.Value;
                if (groupName == null)
                {
                    continue;
                }
                if (groupName.StartsWith("@"))
                {
                    groupName = groupName.Remove(0, 1);
                }
                for (int i = 0; i < 24; i++)
                { 
                    var scanTask = new ScanTask
                    {
                        GroupName = groupName,
                        NewerThan = now.AddDays(-2).AddHours(i),
                        OlderThan = now.AddDays(-2).AddHours(i+MaxTaskScanInterval)
                    };
                    _scanTaskQueue.Enqueue(scanTask);
                }
            }
            //await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}