using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiceWorker.Configuration;
using ServiceWorker.Services;
using ServiceWorker.Utilities;

namespace ServiceWorker
{
    public class Program
    {
        public static void Main (string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
       Host.CreateDefaultBuilder(args)
           .ConfigureAppConfiguration((hostingContext, config) =>
           {
               config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
           })
           .ConfigureServices((hostContext, services) =>
           {
               services.Configure<AppConfig>(hostContext.Configuration.GetSection("AppConfig"));
               services.AddHostedService<Worker>();
               services.AddHttpClient<IAudioTranscriptionService, AudioTranscriptionService>();
               services.AddSingleton<IHttpService, HttpService>();
               services.AddSingleton<IFileValidator, FileValidator>();
               services.AddSingleton<ITranscriptionQueueManager, TranscriptionQueueManager>();
               services.AddSingleton<IAudioTranscriptionService, AudioTranscriptionService>();

           });
    }
}

