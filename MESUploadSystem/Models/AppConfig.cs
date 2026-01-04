using System;
using System.Collections.Generic;

namespace MESUploadSystem.Models
{
    [Serializable]
    public class AppConfig
    {
        public MesConfig MesConfig { get; set; } = new MesConfig();
        public PlcSignalConfig PlcSignalConfig { get; set; } = PlcSignalConfig.GetDefault();
        public List<BatchMaterial> BatchMaterials { get; set; } = new List<BatchMaterial>();
        public List<SerialPortConfig> SerialPorts { get; set; } = new List<SerialPortConfig>();
        public List<UdpConfig> UdpConfigs { get; set; } = new List<UdpConfig>();
        public List<TcpConfig> TcpConfigs { get; set; } = new List<TcpConfig>();
    }
}