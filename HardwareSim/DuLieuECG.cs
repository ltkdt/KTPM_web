using System;

namespace HardwareSim
{
    public class DuLieuECG
    {
        public int Id { get; set; }
        public string MacAddress { get; set; } = string.Empty;
        public int BenhNhanId { get; set; }
        public string FileJson { get; set; } = string.Empty;
        public int NhipTim { get; set; }
        public DateTime RecordedAt { get; set; }
    }
}
