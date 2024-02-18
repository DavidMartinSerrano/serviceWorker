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

            // Arrange
            var mockHttpService = new Mock<IHttpService>();
            var mockLogger = new Mock<ILogger<AudioTranscriptionService>>();
            mockHttpService.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("Transcription result")
                });

            
            var service = new AudioTranscriptionService(mockHttpService.Object, mockLogger.Object, _configMock.Object);

            // Act
            var result = await service.TranscribeAudioAsync(testFilePath);

            // Assert
            Assert.Equal("Transcription result", result);
        }

        [Fact]
        public async Task TranscribeAudioAsync_HandlesHttpClientError()
        {
            // Arrange

            var mockHttpService = new Mock<IHttpService>();
            var mockLogger = new Mock<ILogger<AudioTranscriptionService>>();

            mockHttpService.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
                           .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

            var service = new AudioTranscriptionService(mockHttpService.Object, mockLogger.Object, _configMock.Object);

            // Act & Assert
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
        public async Task TranscribeAudioAsync_ThrowsFileNotFoundException_WhenFileDoesNotExist()
        {
            // Arrange

            var mockHttpService = new Mock<IHttpService>();
            var service = new AudioTranscriptionService(mockHttpService.Object, mockLogger.Object, _configMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => service.TranscribeAudioAsync("nonexistentfile.mp3"));
        }

        [Fact]
        public async Task TranscribeAudioAsync_RetriesOnTransientHttpError_AndEventuallySucceeds()
        {
            // Arrange
            var mockHttpService = new Mock<IHttpService>();
            int callCount = 0;
            mockHttpService.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
                           .ReturnsAsync(() =>
                           {
                               callCount++;
                               return callCount >= 3
                                   ? new HttpResponseMessage(HttpStatusCode.OK)
                                   {
                                       Content = new StringContent("Successful after retry")
                                   }
                                   : new HttpResponseMessage(HttpStatusCode.InternalServerError);
                           });

            var service = new AudioTranscriptionService(mockHttpService.Object, mockLogger.Object, _configMock.Object);

            // Act 
            var result = await service.TranscribeAudioAsync(testFilePath);

            // Assert
            Assert.Equal("Successful after retry", result);
            Assert.True(callCount == 3, "Expected 3 calls including retries");
        }

    }
}
