using System;
using System.IO;
using System.Threading.Tasks;

namespace HardwareSim
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("====================================================");
            Console.WriteLine("🔬 IoT ECG EDGE DEVICE HARDWARE SIMULATOR (REAL API)");
            Console.WriteLine("====================================================");

            // Load configuration from config.json (checks current directory first, then base directory)
            string configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
            if (!File.Exists(configPath))
            {
                configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            }
            SimulatorConfig config = SimulatorConfig.Load(configPath);

            // Khởi tạo các lớp dịch vụ và module
            // Dùng Real DeviceApiClient kết nối trực tiếp đến Web API
            IDeviceApiClient apiClient = new DeviceApiClient(config.BaseApiUrl);
            EcgMeasureModule measureModule = new EcgMeasureModule();
            StorageService storageService = new StorageService();

            // Khởi tạo MainSimulator với config đã load
            MainSimulator simulator = new MainSimulator(
                config, 
                apiClient, 
                measureModule, 
                storageService
            );

            // Chạy Simulator (Bắt đầu kết nối thiết bị và chạy các Timer ngầm)
            await simulator.Run();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n[Program] Simulator is running. Timers are active.");
            Console.WriteLine($"[Program] Local configuration path: {configPath}");
            Console.WriteLine("[Program] Type 'exit' and press Enter to stop the simulator.\n");
            Console.ResetColor();

            // Vòng lặp Console.ReadLine() giữ chương trình không kết thúc
            while (true)
            {
                string? input = Console.ReadLine();
                if (input != null && input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("[Program] Shutting down simulator...");
                    break;
                }
                else
                {
                    Console.WriteLine("[Program] Simulator is running. Type 'exit' to quit.");
                }
            }
        }
    }
}