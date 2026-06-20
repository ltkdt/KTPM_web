using System;
using System.IO;
using System.Text.Json;

namespace HardwareSim
{
    public class SimulatorConfig
    {
        public string MacAddress { get; set; } = "00:1A:2B:3C:4D:5E";
        public int PatientId { get; set; } = 1;
        public int MeasureIntervalSeconds { get; set; } = 10;
        public int UploadIntervalSeconds { get; set; } = 60;
        public string BaseApiUrl { get; set; } = "http://localhost:5000";

        public static SimulatorConfig Load(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var config = JsonSerializer.Deserialize<SimulatorConfig>(json);
                    if (config != null)
                    {
                        Console.WriteLine($"[Config] Loaded settings from: {filePath}");
                        return config;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[Config] Warning: Failed to load config from {filePath}: {ex.Message}. Using defaults.");
                Console.ResetColor();
            }

            // Fallback and generate new config file
            var defaultConfig = new SimulatorConfig();
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(defaultConfig, options);
                File.WriteAllText(filePath, json);
                Console.WriteLine($"[Config] Generated default config file at: {filePath}");
            }
            catch (Exception writeEx)
            {
                Console.WriteLine($"[Config] Failed to save default config: {writeEx.Message}");
            }
            return defaultConfig;
        }
    }
}
