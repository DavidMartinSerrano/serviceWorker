namespace ServiceWorker.Utilities
{
    public interface IFileValidator
    {
        bool IsValidFile(string filePath, int minSizeKb, int maxSizeKb);
    }
}