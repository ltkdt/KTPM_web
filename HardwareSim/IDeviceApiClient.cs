using System.Threading.Tasks;

namespace HardwareSim
{
    public interface IDeviceApiClient
    {
        Task<bool> LinkDeviceAsync(string macAddress, int benhNhanId);
        Task<bool> UploadDataAsync(string macAddress, string fileJson, int nhipTim, double rmssd);
    }
}
