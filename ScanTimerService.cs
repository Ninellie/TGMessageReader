﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MessageReader;

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