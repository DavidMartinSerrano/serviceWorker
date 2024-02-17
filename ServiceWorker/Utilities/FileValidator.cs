using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ServiceWorker.Utilities
{
    public class FileValidator : IFileValidator
    {
        public bool IsValidFile(string filePath, int minSizeKb, int maxSizeKb)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Extension.ToLower() != ".mp3") return false;
            var sizeInKb = fileInfo.Length / 1024;
            return sizeInKb >= minSizeKb && sizeInKb <= maxSizeKb;
        }
    }
}
