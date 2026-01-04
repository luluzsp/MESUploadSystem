using System;

namespace MESUploadSystem.Models
{
    [Serializable]
    public class UdpConfig
    {
        public int Index { get; set; }
        public string Name => $"UDP{Index}";
        public string CommType { get; set; } = "写入";  // L读取, R读取, 写入
        public string IpAddress { get; set; } = "192.168.1.10";
        public int Port { get; set; } = 9600;
        public byte PlcNode { get; set; } = 10;
        public byte PcNode { get; set; } = 18;
    }
}