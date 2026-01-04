using System;

namespace MESUploadSystem.Models
{
    [Serializable]
    public class TcpConfig
    {
        public int Index { get; set; }
        public string Name => $"TCP{Index}";
        public string CommType { get; set; } = "写入";  // L读取, R读取, 写入
        public string IpAddress { get; set; } = "192.168.1.10";
        public int Port { get; set; } = 9600;
    }
}