using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;
using ServiceWorker.Configuration;
using ServiceWorker.Services;
using Microsoft.Extensions.Logging;

namespace ServiceWorker.Utilities
{  

    public class TranscriptionQueueManager : ITranscriptionQueueManager
    {
        private readonly AppConfig _config;
        private readonly IAudioTranscriptionService _transcriptionService;
        private readonly ILogger<TranscriptionQueueManager> _logger;
        private readonly IFileValidator _fileValidator;
        private SemaphoreSlim _semaphore = new SemaphoreSlim(3); // Allows up to 3 concurrent tasks

        public TranscriptionQueueManager(AppConfig config, IAudioTranscriptionService transcriptionService, ILogger<TranscriptionQueueManager> logger, IFileValidator fileValidator)
        {
            _config = config;
            _transcriptionService = transcriptionService;
            _logger = logger;
            _fileValidator = fileValidator;
        }

        public async Task ProcessFilesAsync(CancellationToken cancellationToken)
        {
            var filePaths = Directory.EnumerateFiles(_config.FilePath, "*.mp3")
                                      .Where(file => _fileValidator.IsValidFile(file, _config.MinFileSize, _config.MaxFileSize))
                                      .ToList();

            // Log not valid files?

            var tasks = filePaths.Select(filePath => ProcessFileAsync(filePath, cancellationToken)).ToList();

            while (tasks.Any())
            {
                Task finishedTask = await Task.WhenAny(tasks);
                tasks.Remove(finishedTask);
            }
        }

        private async Task ProcessFileAsync(string filePath, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            
            try
            {
                var transcriptionResult = await _transcriptionService.TranscribeAudioAsync(filePath);
                await SaveTranscriptionResult(filePath, transcriptionResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing file: {filePath}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task SaveTranscriptionResult(string audioFilePath, string transcription)
        {
            var textFilePath = Path.ChangeExtension(audioFilePath, ".txt");
            await File.WriteAllTextAsync(textFilePath, transcription);
        }
    }
}
