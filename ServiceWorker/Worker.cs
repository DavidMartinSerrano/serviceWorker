using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceWorker.Configuration;
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
        private readonly IOptions<AppConfig> _appConfig;
        private readonly bool _startImmediately;

        public Worker(ILogger<Worker> logger, ITranscriptionQueueManager transcriptionQueueManager, IOptions<AppConfig> appConfig)
        {            
            _logger = logger;
            _transcriptionQueueManager = transcriptionQueueManager;
            _appConfig = appConfig;
            _startImmediately = _appConfig.Value.StartImmediately;

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_startImmediately)
            {
                _logger.LogInformation("Starting processing files immediately.");
                await _transcriptionQueueManager.ProcessFilesAsync(stoppingToken);
            }

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
