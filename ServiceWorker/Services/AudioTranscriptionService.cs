using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using ServiceWorker.Configuration;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ServiceWorker.Services
{
    public class AudioTranscriptionService : IAudioTranscriptionService
    {
        private readonly AppConfig _config;
        private readonly IHttpService _httpService;
        private readonly ILogger<AudioTranscriptionService> _logger;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private const string userLoginProfile = "FakedUserLoginProfile";

        public AudioTranscriptionService(IHttpService httpService, ILogger<AudioTranscriptionService> logger, IOptions<AppConfig> config)
        {
            _config = config.Value;
            _httpService = httpService ?? throw new ArgumentNullException(nameof(httpService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning("Request failed with {ExceptionType}. Retrying {RetryCount}...", exception.GetType().Name, retryCount);
                    });
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

                // Add the audio file
                using var fileStream = File.OpenRead(audioFilePath);
                content.Add(new StreamContent(fileStream), "file", Path.GetFileName(audioFilePath));

                // Add metadata -- This would be the user login for the voice profile (could be get from an injected login service or whatever, done this way for simplicity,
                // and avoid adding more mocks now to unit tests, etc..)
                content.Add(new StringContent(userLoginProfile), "metadata");

                var response = await _retryPolicy.ExecuteAsync(() => _httpService.PostAsync(_config.InvoxMedicalServiceEndpoint, content));

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
