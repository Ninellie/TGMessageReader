using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace MessageReader;

public class ScanTimerService : IHostedService
{
    private readonly ScanTaskQueue _scanTaskQueue;
    private readonly IConfiguration _groups;

    public ScanTimerService(IConfiguration config, ScanTaskQueue scanTaskQueue)
    {
        _scanTaskQueue = scanTaskQueue;
        _groups = config.GetSection("ScanConfig").GetSection("Groups");
    }


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(async () => await SetTimedScan(cancellationToken), cancellationToken);
    }

    private async Task SetTimedScan(CancellationToken cancellationToken)
    {
        for (;;)
        {
            foreach (var group in _groups.AsEnumerable())
            {
                var scanTask = new ScanTask();
                var groupName = group.Value;
                if (groupName == null)
                {
                    continue;
                }
                if (groupName.StartsWith("@"))
                {
                    groupName = groupName.Remove(0, 1);
                }
                scanTask.GroupName = groupName;
                scanTask.NewerThan = DateTime.Now.AddHours(-2 - 12).AddMinutes(-10).AddSeconds(10);
                scanTask.OlderThan = DateTime.Now.AddHours(-2 - 12);
                _scanTaskQueue.Enqueue(scanTask);
            }
            await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}