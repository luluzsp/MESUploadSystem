using System;

namespace MESUploadSystem.Models
{
    [Serializable]
    public class SerialPortConfig
    {
        public int Index { get; set; }
        public string Name => $"串口{Index}";
        public string PortType { get; set; } = "L读取";    // L读取,R读取,写入
        public string PortName { get; set; } = "COM1";
        public int DataBits { get; set; } = 8;
        public string StopBits { get; set; } = "One";
        public int BaudRate { get; set; } = 115200;
        public string Parity { get; set; } = "None";
    }
}