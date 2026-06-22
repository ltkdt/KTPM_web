using System;
using System.IO;

namespace HardwareSim
{
    public class StorageService
    {
        private const string CacheDirectory = "measurements_cache";

        public void SaveRawData(string jsonData)
        {
            try
            {
                if (!Directory.Exists(CacheDirectory))
                {
                    Directory.CreateDirectory(CacheDirectory);
                }

                // Generate a unique timestamped file name
                string fileName = $"ecg_{DateTime.Now:yyyyMMdd_HHmmss_fff}.json";
                string filePath = Path.Combine(CacheDirectory, fileName);
                
                File.WriteAllText(filePath, jsonData);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[StorageService ERROR] Failed to save raw data: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}
