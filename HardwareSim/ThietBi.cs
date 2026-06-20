using System;

namespace HardwareSim
{
    public class ThietBi
    {
        public int Id { get; set; }
        public string MacAddress { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public int? BenhNhanId { get; set; }
        public DateTime? ConnectedAt { get; set; }
    }
}
