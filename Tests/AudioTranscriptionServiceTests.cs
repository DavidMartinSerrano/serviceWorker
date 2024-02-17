using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ServiceWorker.Configuration;
using ServiceWorker.Services;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class AudioTranscriptionServiceTests
    {

        private readonly Mock<ILogger<AudioTranscriptionService>> mockLogger = new Mock<ILogger<AudioTranscriptionService>>();
        private const string testFilePath = "test.mp3";
        private readonly Mock<IOptions<AppConfig>> _configMock;

        public AudioTranscriptionServiceTests()
        {
            _configMock = new Mock<IOptions<AppConfig>>();
            _configMock.Setup(x => x.Value).Returns(new AppConfig
            {           
                FilePath = "C:\\Test\\"
            });
        }


        [Fact]
        public async Task TranscribeAudioAsync_SuccessfulTranscription()
        {
            var mockHttpService = new Mock<IHttpService>();
            var mockLogger = new Mock<ILogger<AudioTranscriptionService>>();
            mockHttpService.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("Transcription result")
                });

            var service = new AudioTranscriptionService(mockHttpService.Object, mockLogger.Object, _configMock.Object);

            var result = await service.TranscribeAudioAsync(testFilePath);

            Assert.Equal("Transcription result", result);
        }

        [Fact]
        public async Task TranscribeAudioAsync_HandlesHttpClientError()
        {
            var mockHttpService = new Mock<IHttpService>();
            var mockLogger = new Mock<ILogger<AudioTranscriptionService>>();

            mockHttpService.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
                           .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

            var service = new AudioTranscriptionService(mockHttpService.Object, mockLogger.Object, _configMock.Object);

            await Assert.ThrowsAsync<HttpRequestException>(() => service.TranscribeAudioAsync(testFilePath));

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.AtLeastOnce);
        }


        [Fact]
        public async Task TranscribeAudioAsync_LogsError_OnException()
        {
            var mockHttpService = new Mock<IHttpService>();
            mockHttpService.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
                           .ThrowsAsync(new InvalidOperationException("Simulated exception"));

            var mockLogger = new Mock<ILogger<AudioTranscriptionService>>();
            mockLogger.Setup(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()));

            var service = new AudioTranscriptionService(mockHttpService.Object, mockLogger.Object, _configMock.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.TranscribeAudioAsync(testFilePath));

            mockLogger.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task TranscribeAudioAsync_LogsError_OnInvalidFilePath()
        {
            var mockHttpService = new Mock<IHttpService>();
            var mockLogger = new Mock<ILogger<AudioTranscriptionService>>();

            var service = new AudioTranscriptionService(mockHttpService.Object, mockLogger.Object, _configMock.Object);

            await Assert.ThrowsAsync<FileNotFoundException>(() => service.TranscribeAudioAsync("invalid/testFilePath"));
        }

        [Fact]
        public async Task TranscribeAudioAsync_HandlesTimeoutException()
        {
            var mockHttpService = new Mock<IHttpService>();
            mockHttpService.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
                           .ThrowsAsync(new TaskCanceledException("The request was canceled due to a timeout."));

            var mockLogger = new Mock<ILogger<AudioTranscriptionService>>();
            mockLogger.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()));

            var service = new AudioTranscriptionService(mockHttpService.Object, mockLogger.Object, _configMock.Object);

            await Assert.ThrowsAsync<TaskCanceledException>(() => service.TranscribeAudioAsync(testFilePath));

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

    }
}
