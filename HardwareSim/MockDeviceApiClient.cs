using System;
using System.Threading.Tasks;

namespace HardwareSim
{
    public class MockDeviceApiClient : IDeviceApiClient
    {
        public async Task<bool> LinkDeviceAsync(string macAddress, int benhNhanId)
        {
            Console.WriteLine($"[MockDeviceApiClient] Linking device (MAC: {macAddress}) to Patient ID: {benhNhanId}...");
            await Task.Delay(500); // Simulate network roundtrip
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[MockDeviceApiClient] LinkDeviceAsync -> Success (Device linked successfully)");
            Console.ResetColor();
            return true;
        }

        public async Task<bool> UploadDataAsync(string macAddress, string fileJson, int nhipTim, double rmssd)
        {
            Console.WriteLine($"[MockDeviceApiClient] Uploading data for MAC {macAddress} (Heart Rate: {nhipTim} bpm, RMSSD: {rmssd:F1} ms)...");
            await Task.Delay(1000); // Simulate network latency
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[MockDeviceApiClient] UploadDataAsync -> 201 Created. Uploading data for MAC {macAddress}... Success");
            Console.ResetColor();
            return true;
        }
    }
}
