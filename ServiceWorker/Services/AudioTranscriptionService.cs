using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ServiceWorker.Services
{
    public class AudioTranscriptionService : IAudioTranscriptionService
    {
        private readonly IHttpService _httpService;
        private readonly ILogger<AudioTranscriptionService> _logger;

        public AudioTranscriptionService(IHttpService httpService, ILogger<AudioTranscriptionService> logger)
        {
            _httpService = httpService ?? throw new ArgumentNullException(nameof(httpService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> TranscribeAudioAsync(string audioFilePath)
        {
            if (!File.Exists(audioFilePath))
            {
                throw new FileNotFoundException($"The file {audioFilePath} was not found.");
            }

            try
            {
                var content = new MultipartFormDataContent();
                using var fileStream = File.OpenRead(audioFilePath);
                content.Add(new StreamContent(fileStream), "file", Path.GetFileName(audioFilePath));

                var response = await _httpService.PostAsync("YOUR_TRANSCRIPTION_SERVICE_ENDPOINT", content);
                response.EnsureSuccessStatusCode();

                var transcriptionResult = await response.Content.ReadAsStringAsync();
                return transcriptionResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transcribing file: {FilePath}", audioFilePath);
                throw;
            }
        }

    }
}
