using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace HardwareSim
{
    public class DeviceApiClient : IDeviceApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseApiUrl;

        public DeviceApiClient(string baseApiUrl)
        {
            _baseApiUrl = baseApiUrl.TrimEnd('/');
            _httpClient = new HttpClient();
        }

        public async Task<bool> LinkDeviceAsync(string macAddress, int benhNhanId)
        {
            string url = $"{_baseApiUrl}/api/devices/link";
            Console.WriteLine($"[DeviceApiClient] Sending link request to {url} for MAC {macAddress} and Patient {benhNhanId}...");

            var payload = new
            {
                MacAddress = macAddress,
                PatientId = benhNhanId
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, payload);
                if (response.IsSuccessStatusCode)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[DeviceApiClient] LinkDeviceAsync -> Success (Status: {(int)response.StatusCode} {response.StatusCode})");
                    Console.ResetColor();
                    return true;
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[DeviceApiClient] LinkDeviceAsync -> Failed (Status: {(int)response.StatusCode} {response.StatusCode}): {error}");
                    Console.ResetColor();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[DeviceApiClient] LinkDeviceAsync -> Connection Error: {ex.Message}");
                Console.ResetColor();
                return false;
            }
        }

        public async Task<bool> UploadDataAsync(string macAddress, string fileJson, int nhipTim, double rmssd)
        {
            string url = $"{_baseApiUrl}/api/records/upload";
            Console.WriteLine($"[DeviceApiClient] Uploading measurement data to {url} (Heart Rate: {nhipTim} bpm, RMSSD: {rmssd:F1} ms)...");

            var payload = new
            {
                MacAddress = macAddress,
                FileJson = fileJson,
                NhipTim = nhipTim,
                Rmssd = rmssd
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, payload);
                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[DeviceApiClient] UploadDataAsync -> 201 Created. Success (Status: {(int)response.StatusCode} {response.StatusCode})");
                    Console.ResetColor();
                    return true;
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[DeviceApiClient] UploadDataAsync -> Failed (Status: {(int)response.StatusCode} {response.StatusCode}): {error}");
                    Console.ResetColor();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[DeviceApiClient] UploadDataAsync -> Connection Error: {ex.Message}");
                Console.ResetColor();
                return false;
            }
        }
    }
}
