using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceWorker.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ITranscriptionQueueManager _transcriptionQueueManager;

        public Worker(ILogger<Worker> logger, ITranscriptionQueueManager transcriptionQueueManager)
        {            
            _logger = logger;
            _transcriptionQueueManager = transcriptionQueueManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextRun = new DateTime(now.Year, now.Month, now.Day, 00, 00, 00).AddDays(1);
                var delay = nextRun - now;
                if (delay.TotalMilliseconds > 0)
                {
                    _logger.LogInformation($"Service will next run at: {nextRun}");
                    await Task.Delay(delay, stoppingToken);
                }

                await _transcriptionQueueManager.ProcessFilesAsync(stoppingToken);
            }
        }

    }

}
