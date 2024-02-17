using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ServiceWorker.Services
{
    public interface IAudioTranscriptionService
    {
        Task<string> TranscribeAudioAsync(string audioFilePath);
    }

}
