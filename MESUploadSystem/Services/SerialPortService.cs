using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;
using MESUploadSystem.Models;

namespace MESUploadSystem.Services
{
    public class SerialPortService : IDisposable
    {
        private Dictionary<string, SerialPort> _ports = new Dictionary<string, SerialPort>();
        private SerialPort _writePort;
        private MesConfig _mesConfig;  // MES配置引用

        public event Action<string, string> OnDataReceived; // portType, data (处理后的SN)

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="mesConfig">MES配置（用于SN处理）</param>
        public SerialPortService(MesConfig mesConfig = null)
        {
            _mesConfig = mesConfig;
        }

        /// <summary>
        /// 设置/更新MES配置（用于SN处理）
        /// </summary>
        public void SetMesConfig(MesConfig mesConfig)
        {
            _mesConfig = mesConfig;
            LogService.Log($"[SerialPortService] MES配置已更新, RemoveSnSuffix={mesConfig?.RemoveSnSuffix}");
        }

        public bool OpenPorts(List<SerialPortConfig> configs)
        {
            try
            {
                foreach (var config in configs)
                {
                    var port = new SerialPort
                    {
                        PortName = config.PortName,
                        BaudRate = config.BaudRate,
                        DataBits = config.DataBits,
                        StopBits = ParseStopBits(config.StopBits),
                        Parity = ParseParity(config.Parity),
                        Encoding = Encoding.ASCII,
                        ReadTimeout = 1000,
                        WriteTimeout = 1000
                    };

                    port.Open();
                    LogService.Log($"{config.Name} ({config.PortName}) 打开成功");

                    if (config.PortType == "写入")
                    {
                        _writePort = port;
                    }
                    else
                    {
                        port.DataReceived += (s, e) => Port_DataReceived(port, config.PortType);
                        _ports[config.PortType] = port;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                LogService.Log($"打开串口失败: {ex.Message}");
                ClosePorts();
                return false;
            }
        }

        private void Port_DataReceived(SerialPort port, string portType)
        {
            try
            {
                Thread.Sleep(100); // 等待数据完整接收
                var data = port.ReadExisting().Trim();
                if (!string.IsNullOrEmpty(data))
                {
                    LogService.Log($"{portType} 接收原始数据: {data}");

                    // 【关键】读取到SN后立即进行处理
                    string processedData = ProcessSn(data, portType);

                    // 传递处理后的SN
                    OnDataReceived?.Invoke(portType, processedData);
                }
            }
            catch (Exception ex)
            {
                LogService.Log($"读取串口数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理SN，根据MES配置决定是否去除后缀
        /// 去除最后一个+号后的内容
        /// 例如：FM71234+56AT+ANB → FM71234+56AT
        /// </summary>
        /// <param name="originalSn">原始SN</param>
        /// <param name="portType">端口类型（L读取/R读取）</param>
        /// <returns>处理后的SN</returns>
        private string ProcessSn(string originalSn, string portType)
        {
            if (string.IsNullOrEmpty(originalSn))
                return originalSn;

            // 检查配置状态并记录日志
            if (_mesConfig == null)
            {
                LogService.Log($"[{portType}] 警告: MES配置未设置，无法处理SN后缀");
                return originalSn;
            }

            if (!_mesConfig.RemoveSnSuffix)
            {
                // 未启用去除后缀功能，返回原始SN
                return originalSn;
            }

            // 查找最后一个+号的位置
            int lastPlusIndex = originalSn.LastIndexOf('+');

            // 如果找到+号，且不是第一个字符，则去除+号及其后面的内容
            if (lastPlusIndex > 0)
            {
                string processedSn = originalSn.Substring(0, lastPlusIndex);
                LogService.Log($"[{portType}] SN后缀已去除: {originalSn} → {processedSn}");
                return processedSn;
            }

            // 没有+号或+号在第一位，返回原始SN
            return originalSn;
        }

        public void WriteData(string data)
        {
            try
            {
                if (_writePort != null && _writePort.IsOpen)
                {
                    _writePort.Write(data);
                    LogService.Log($"写入串口数据: {data}");
                }
            }
            catch (Exception ex)
            {
                LogService.Log($"写入串口数据失败: {ex.Message}");
            }
        }

        public void ClosePorts()
        {
            foreach (var port in _ports.Values)
            {
                try { if (port.IsOpen) port.Close(); } catch { }
            }
            _ports.Clear();

            try { if (_writePort?.IsOpen == true) _writePort.Close(); } catch { }
            _writePort = null;

            LogService.Log("所有串口已关闭");
        }

        private StopBits ParseStopBits(string value)
        {
            switch (value)
            {
                case "None": return StopBits.None;
                case "Two": return StopBits.Two;
                default: return StopBits.One;
            }
        }

        private Parity ParseParity(string value)
        {
            switch (value)
            {
                case "Odd": return Parity.Odd;
                case "Even": return Parity.Even;
                case "Mark": return Parity.Mark;
                case "Space": return Parity.Space;
                default: return Parity.None;
            }
        }

        public void Dispose()
        {
            ClosePorts();
        }
    }
}
