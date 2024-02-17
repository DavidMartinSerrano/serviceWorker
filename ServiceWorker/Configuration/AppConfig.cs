using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceWorker.Configuration
{
    public class AppConfig
    {
        public string FilePath { get; set; }
        public int MinFileSize { get; set; }
        public int MaxFileSize { get; set; }
        public string InvoxMedicalServiceEndpoint { get; set; }
    }

}
