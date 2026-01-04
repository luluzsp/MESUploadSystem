
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MESUploadSystem.Models;

namespace MESUploadSystem.Services
{
    public class PlcCommService : IDisposable
    {
        private List<UdpClient> _udpClients = new List<UdpClient>();
        private List<TcpClient> _tcpClients = new List<TcpClient>();
        private List<SerialPort> _serialPorts = new List<SerialPort>();

        private Dictionary<string, object> _writeClients = new Dictionary<string, object>();

        private List<UdpConfig> _udpConfigs;
        private List<TcpConfig> _tcpConfigs;
        private List<SerialPortConfig> _serialConfigs;

        /// <summary>
        /// PLC响应超时时间（毫秒）
        /// </summary>
        public int ResponseTimeout { get; set; } = 1000;

        /// <summary>
        /// PLC响应事件（用于通知界面）
        /// </summary>
        public event Action<string, bool, string> OnPlcResponse;

        public PlcCommService(List<UdpConfig> udpConfigs, List<TcpConfig> tcpConfigs, List<SerialPortConfig> serialConfigs)
        {
            _udpConfigs = udpConfigs ?? new List<UdpConfig>();
            _tcpConfigs = tcpConfigs ?? new List<TcpConfig>();
            _serialConfigs = serialConfigs ?? new List<SerialPortConfig>();
        }

        /// <summary>
        /// 建立所有通信连接
        /// </summary>
        public async Task<bool> ConnectAllAsync()
        {
            bool allSuccess = true;

            // 连接写入类型的串口
            foreach (var config in _serialConfigs.Where(s => s.PortType == "写入"))
            {
                try
                {
                    var port = new SerialPort
                    {
                        PortName = config.PortName,
                        BaudRate = config.BaudRate,
                        DataBits = config.DataBits,
                        StopBits = ParseStopBits(config.StopBits),
                        Parity = ParseParity(config.Parity),
                        ReadTimeout = 3000,
                        WriteTimeout = 3000,
                        Encoding = Encoding.ASCII
                    };

                    port.Open();
                    _serialPorts.Add(port);
                    _writeClients[$"Serial_{config.Index}"] = new SerialWriteContext
                    {
                        Port = port,
                        Config = config
                    };

                    LogService.Info($"✓ {config.Name} ({config.PortName}) 打开成功");
                }
                catch (Exception ex)
                {
                    LogService.Error($"✗ {config.Name} 打开失败: {ex.Message}");
                    allSuccess = false;
                }
            }

            // 连接UDP
            foreach (var config in _udpConfigs)
            {
                try
                {
                    var client = new UdpClient();
                    var endPoint = new IPEndPoint(IPAddress.Parse(config.IpAddress), config.Port);

                    _udpClients.Add(client);

                    if (config.CommType == "写入")
                    {
                        _writeClients[$"UDP_{config.Index}"] = new UdpWriteContext
                        {
                            Client = client,
                            EndPoint = endPoint,
                            Config = config
                        };
                    }

                    LogService.Info($"✓ {config.Name} 初始化成功 ({config.IpAddress}:{config.Port})");
                }
                catch (Exception ex)
                {
                    LogService.Error($"✗ {config.Name} 初始化失败: {ex.Message}");
                    allSuccess = false;
                }
            }

            // 连接TCP
            foreach (var config in _tcpConfigs)
            {
                try
                {
                    var client = new TcpClient();
                    client.ReceiveTimeout = 5000;
                    client.SendTimeout = 5000;

                    await client.ConnectAsync(config.IpAddress, config.Port);
                    _tcpClients.Add(client);

                    if (await FinsTcpHandshakeAsync(client, config))
                    {
                        LogService.Info($"✓ {config.Name} 连接成功 ({config.IpAddress}:{config.Port})");
                    }
                    else
                    {
                        allSuccess = false;
                    }
                }
                catch (Exception ex)
                {
                    LogService.Error($"✗ {config.Name} 连接失败: {ex.Message}");
                    allSuccess = false;
                }
            }

            return allSuccess;
        }

        /// <summary>
        /// TCP FINS握手
        /// </summary>
        private async Task<bool> FinsTcpHandshakeAsync(TcpClient client, TcpConfig config)
        {
            try
            {
                var stream = client.GetStream();

                byte[] handshake = {
                    0x46, 0x49, 0x4E, 0x53,
                    0x00, 0x00, 0x00, 0x0C,
                    0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00
                };

                await stream.WriteAsync(handshake, 0, handshake.Length);

                byte[] response = new byte[24];
                int bytesRead = await stream.ReadAsync(response, 0, response.Length);

                if (bytesRead >= 24 && response[15] == 0x00)
                {
                    byte pcNode = response[19];
                    byte plcNode = response[23];

                    if (config.CommType == "写入")
                    {
                        _writeClients[$"TCP_{config.Index}"] = new TcpWriteContext
                        {
                            Client = client,
                            Config = config,
                            PcNode = pcNode,
                            PlcNode = plcNode
                        };
                    }

                    LogService.Info($"  FINS握手成功 - PC节点:{pcNode}, PLC节点:{plcNode}");
                    return true;
                }

                LogService.Error("  FINS握手失败");
                return false;
            }
            catch (Exception ex)
            {
                LogService.Error($"  握手异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送PLC信号（统一入口）
        /// 发送完成后立即返回，不等待PLC响应
        /// PLC响应结果通过日志和事件通知，不影响程序运行
        /// </summary>
        public async Task SendSignalAsync(PlcSignalItem signal)
        {
            if (signal == null)
            {
                LogService.Info("信号配置为空，跳过发送");
                return;
            }

            bool valueToSend = signal.Value;
            string valueDisplay = valueToSend ? "ON" : "OFF";
            string dataDisplay = valueToSend ? "01" : "00";

            LogService.Info($"📤 发送信号: {signal.Trigger} → {signal.Address} = {valueDisplay} (数据:{dataDisplay})");

            if (!ParseWAddress(signal.Address, out int wordAddr, out int bitAddr))
            {
                LogService.Info($"⚠ 无效的PLC地址: {signal.Address}，跳过发送");
                NotifyResponse(signal.Trigger, false, "无效地址");
                return;
            }

            if (_writeClients.Count == 0)
            {
                LogService.Info("⚠ 没有可用的写入通道，跳过发送");
                NotifyResponse(signal.Trigger, false, "无写入通道");
                return;
            }

            // 遍历所有写入通道发送（只等待发送完成，不等待响应）
            foreach (var kvp in _writeClients)
            {
                try
                {
                    if (kvp.Value is SerialWriteContext serialCtx)
                    {
                        await SendSerialFinsAsync(serialCtx, wordAddr, bitAddr, valueToSend, signal.Trigger);
                    }
                    else if (kvp.Value is UdpWriteContext udpCtx)
                    {
                        await SendUdpFinsAsync(udpCtx, wordAddr, bitAddr, valueToSend, signal.Trigger);
                    }
                    else if (kvp.Value is TcpWriteContext tcpCtx)
                    {
                        await SendTcpFinsAsync(tcpCtx, wordAddr, bitAddr, valueToSend, signal.Trigger);
                    }
                }
                catch (Exception ex)
                {
                    LogService.Info($"⚠ 发送异常 [{kvp.Key}]: {ex.Message}");
                    NotifyResponse(signal.Trigger, false, $"发送异常: {ex.Message}");
                }
            }

            // 方法在此返回，PLC响应在后台异步处理
        }

        /// <summary>
        /// 通知响应结果（触发事件和日志）
        /// </summary>
        private void NotifyResponse(string trigger, bool success, string message)
        {
            try
            {
                OnPlcResponse?.Invoke(trigger, success, message);
            }
            catch { }
        }

        #region 串口通信

        /// <summary>
        /// 串口方式发送FINS命令
        /// 发送完成后立即返回，响应在后台异步处理
        /// </summary>
        private Task SendSerialFinsAsync(SerialWriteContext ctx, int wordAddr, int bitAddr, bool value, string trigger)
        {
            try
            {
                string command = BuildHostLinkCommand(wordAddr, bitAddr, value);

                LogService.Info($"→ 串口发送 [{ctx.Config.Name}]: {command.TrimEnd('\r')}");

                ctx.Port.DiscardInBuffer();
                ctx.Port.DiscardOutBuffer();
                ctx.Port.Write(command);

                LogService.Info($"✓ 已发送: {ctx.Config.Name} → W{wordAddr}.{bitAddr} = {(value ? "01" : "00")}");

                // 后台异步等待响应（不阻塞当前方法）
                _ = WaitForSerialResponseAsync(ctx, wordAddr, bitAddr, value, trigger);
            }
            catch (Exception ex)
            {
                LogService.Info($"⚠ 串口发送失败 [{ctx.Config.Name}]: {ex.Message}");
                NotifyResponse(trigger, false, $"串口发送失败: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 后台等待串口响应（仅记录，不影响程序运行）
        /// </summary>
        private async Task WaitForSerialResponseAsync(SerialWriteContext ctx, int wordAddr, int bitAddr, bool value, string trigger)
        {
            try
            {
                await Task.Delay(Math.Min(ResponseTimeout, 200)); // 串口响应通常较快

                string response = ctx.Port.ReadExisting().Trim();

                if (!string.IsNullOrEmpty(response))
                {
                    LogService.Info($"← 串口响应 [{ctx.Config.Name}]: {response}");

                    if (response.Contains("00"))
                    {
                        LogService.Info($"✓ PLC确认成功: W{wordAddr}.{bitAddr} = {(value ? "01" : "00")}");
                        NotifyResponse(trigger, true, "写入成功");
                    }
                    else
                    {
                        LogService.Info($"⚠ PLC响应异常: {response}");
                        NotifyResponse(trigger, false, $"响应异常: {response}");
                    }
                }
                else
                {
                    LogService.Info($"ℹ 串口无响应 [{ctx.Config.Name}]（已发送，未收到确认）");
                    NotifyResponse(trigger, true, "已发送，无响应");
                }
            }
            catch (Exception ex)
            {
                LogService.Info($"⚠ 读取串口响应异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 构建HostLink格式的FINS命令（串口）
        /// 【修复】SID固定为0x10，字地址3位十六进制
        /// 格式：@00FA + 网络头(10个0) + SID(固定10) + 2310 + 字地址(3位) + 位地址(2位) + 数量(0001) + 数据(01/00) + FCS + *
        /// </summary>
        private string BuildHostLinkCommand(int wordAddr, int bitAddr, bool value)
        {
            var sb = new StringBuilder();

            // 构建命令体
            sb.Append("@00FA");                        // 头代码
            sb.Append("0000000000");                   // 网络头（10个0）
            sb.Append("10");                           // 【修复】SID固定为10
            sb.Append("2310");                         // 写位命令（已包含W区信息）
            sb.Append(wordAddr.ToString("X3"));        // 字地址：3位十六进制
            sb.Append(bitAddr.ToString("X2"));         // 位地址：2位十六进制
            sb.Append("0001");                         // 数量
            sb.Append(value ? "01" : "00");            // 数据：ON=01, OFF=00

            // 计算FCS（对整个命令体进行XOR）
            string commandBody = sb.ToString();
            byte fcs = CalculateFCS(commandBody);

            sb.Append(fcs.ToString("X2"));             // FCS校验码
            sb.Append("*\r");                          // 结束符

            return sb.ToString();
        }

        /// <summary>
        /// 计算FCS校验码（对所有字符进行XOR）
        /// </summary>
        private byte CalculateFCS(string data)
        {
            byte fcs = 0;
            foreach (char c in data)
            {
                fcs ^= (byte)c;
            }
            return fcs;
        }

        #endregion

        #region UDP通信

        /// <summary>
        /// UDP方式发送FINS命令
        /// 发送完成后立即返回，响应在后台异步处理
        /// </summary>
        private async Task SendUdpFinsAsync(UdpWriteContext ctx, int wordAddr, int bitAddr, bool value, string trigger)
        {
            try
            {
                byte[] finsFrame = BuildUdpFinsFrame(ctx.Config.PcNode, ctx.Config.PlcNode, wordAddr, bitAddr, value);

                LogService.Info($"→ UDP发送 [{ctx.Config.Name}]: {BitConverter.ToString(finsFrame).Replace("-", "")}");

                await ctx.Client.SendAsync(finsFrame, finsFrame.Length, ctx.EndPoint);

                LogService.Info($"✓ 已发送: {ctx.Config.Name} → W{wordAddr}.{bitAddr} = {(value ? "01" : "00")}");

                // 后台异步等待响应（不阻塞当前方法）
                _ = WaitForUdpResponseAsync(ctx, wordAddr, bitAddr, value, trigger);
            }
            catch (Exception ex)
            {
                LogService.Info($"⚠ UDP发送失败 [{ctx.Config.Name}]: {ex.Message}");
                NotifyResponse(trigger, false, $"UDP发送失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 后台等待UDP响应（仅记录，不影响程序运行）
        /// </summary>
        private async Task WaitForUdpResponseAsync(UdpWriteContext ctx, int wordAddr, int bitAddr, bool value, string trigger)
        {
            try
            {
                var receiveTask = ctx.Client.ReceiveAsync();
                if (await Task.WhenAny(receiveTask, Task.Delay(ResponseTimeout)) == receiveTask)
                {
                    var result = receiveTask.Result;
                    LogService.Info($"← UDP响应 [{ctx.Config.Name}]: {BitConverter.ToString(result.Buffer).Replace("-", "")}");

                    if (result.Buffer.Length >= 14)
                    {
                        ushort responseCode = (ushort)((result.Buffer[12] << 8) | result.Buffer[13]);
                        if (responseCode == 0)
                        {
                            LogService.Info($"✓ PLC确认成功: W{wordAddr}.{bitAddr} = {(value ? "01" : "00")}");
                            NotifyResponse(trigger, true, "写入成功");
                        }
                        else
                        {
                            LogService.Info($"⚠ PLC响应错误码: {responseCode:X4}");
                            NotifyResponse(trigger, false, $"错误码: {responseCode:X4}");
                        }
                    }
                    else
                    {
                        LogService.Info($"⚠ UDP响应数据不完整");
                        NotifyResponse(trigger, false, "响应数据不完整");
                    }
                }
                else
                {
                    LogService.Info($"ℹ UDP响应超时 [{ctx.Config.Name}]（已发送，{ResponseTimeout}ms内未收到响应）");
                    NotifyResponse(trigger, true, $"已发送，超时{ResponseTimeout}ms");
                }
            }
            catch (Exception ex)
            {
                LogService.Info($"⚠ 读取UDP响应异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 构建UDP FINS帧（二进制格式）
        /// 格式：FINS头(10字节) + SID + MRC/SRC + 内存区域 + 地址 + 数量 + 数据
        /// </summary>
        private byte[] BuildUdpFinsFrame(byte pcNode, byte plcNode, int wordAddr, int bitAddr, bool value)
        {
            return new byte[]
            {
                // FINS头
                0x80,                          // ICF: 需要响应
                0x00,                          // RSV: 保留
                0x02,                          // GCT: 网关计数
                0x00,                          // DNA: 目的网络地址
                plcNode,                       // DA1: 目的节点地址
                0x00,                          // DA2: 目的单元地址
                0x00,                          // SNA: 源网络地址
                pcNode,                        // SA1: 源节点地址
                0x00,                          // SA2: 源单元地址
                0x10,                          // SID: 服务ID（固定0x10）
                
                // 命令
                0x01, 0x02,                    // MRC=01, SRC=02: 写位命令
                
                // 数据
                0x31,                          // 内存区域: W区
                (byte)(wordAddr >> 8),         // 字地址高位
                (byte)(wordAddr & 0xFF),       // 字地址低位
                (byte)bitAddr,                 // 位地址
                0x00, 0x01,                    // 数量: 1位
                (byte)(value ? 0x01 : 0x00)    // 数据: ON=01, OFF=00
            };
        }

        #endregion

        #region TCP通信

        /// <summary>
        /// TCP方式发送FINS命令
        /// 发送完成后立即返回，响应在后台异步处理
        /// </summary>
        private async Task SendTcpFinsAsync(TcpWriteContext ctx, int wordAddr, int bitAddr, bool value, string trigger)
        {
            try
            {
                var stream = ctx.Client.GetStream();

                byte[] finsFrame = BuildUdpFinsFrame(ctx.PcNode, ctx.PlcNode, wordAddr, bitAddr, value);
                byte[] tcpFrame = WrapTcpHeader(finsFrame);

                LogService.Info($"→ TCP发送 [{ctx.Config.Name}]: {BitConverter.ToString(tcpFrame).Replace("-", "")}");

                await stream.WriteAsync(tcpFrame, 0, tcpFrame.Length);

                LogService.Info($"✓ 已发送: {ctx.Config.Name} → W{wordAddr}.{bitAddr} = {(value ? "01" : "00")}");

                // 后台异步等待响应（不阻塞当前方法）
                _ = WaitForTcpResponseAsync(ctx, stream, wordAddr, bitAddr, value, trigger);
            }
            catch (Exception ex)
            {
                LogService.Info($"⚠ TCP发送失败 [{ctx.Config.Name}]: {ex.Message}");
                NotifyResponse(trigger, false, $"TCP发送失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 后台等待TCP响应（仅记录，不影响程序运行）
        /// </summary>
        private async Task WaitForTcpResponseAsync(TcpWriteContext ctx, NetworkStream stream, int wordAddr, int bitAddr, bool value, string trigger)
        {
            try
            {
                byte[] response = new byte[128];

                var cts = new CancellationTokenSource(ResponseTimeout);
                try
                {
                    int bytesRead = await stream.ReadAsync(response, 0, response.Length, cts.Token);

                    LogService.Info($"← TCP响应 [{ctx.Config.Name}]: {BitConverter.ToString(response, 0, bytesRead).Replace("-", "")}");

                    if (bytesRead >= 30)
                    {
                        ushort responseCode = (ushort)((response[28] << 8) | response[29]);
                        if (responseCode == 0)
                        {
                            LogService.Info($"✓ PLC确认成功: W{wordAddr}.{bitAddr} = {(value ? "01" : "00")}");
                            NotifyResponse(trigger, true, "写入成功");
                        }
                        else
                        {
                            LogService.Info($"⚠ PLC响应错误码: {responseCode:X4}");
                            NotifyResponse(trigger, false, $"错误码: {responseCode:X4}");
                        }
                    }
                    else
                    {
                        LogService.Info($"⚠ TCP响应数据不完整");
                        NotifyResponse(trigger, false, "响应数据不完整");
                    }
                }
                catch (OperationCanceledException)
                {
                    LogService.Info($"ℹ TCP响应超时 [{ctx.Config.Name}]（已发送，{ResponseTimeout}ms内未收到响应）");
                    NotifyResponse(trigger, true, $"已发送，超时{ResponseTimeout}ms");
                }
            }
            catch (Exception ex)
            {
                LogService.Info($"⚠ 读取TCP响应异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 添加TCP FINS头
        /// </summary>
        private byte[] WrapTcpHeader(byte[] finsFrame)
        {
            int len = finsFrame.Length + 8;
            byte[] tcpFrame = new byte[16 + finsFrame.Length];

            // FINS/TCP头
            tcpFrame[0] = 0x46;  // 'F'
            tcpFrame[1] = 0x49;  // 'I'
            tcpFrame[2] = 0x4E;  // 'N'
            tcpFrame[3] = 0x53;  // 'S'
            tcpFrame[4] = (byte)(len >> 24);   // 长度
            tcpFrame[5] = (byte)(len >> 16);
            tcpFrame[6] = (byte)(len >> 8);
            tcpFrame[7] = (byte)len;
            tcpFrame[8] = 0x00;   // 命令
            tcpFrame[9] = 0x00;
            tcpFrame[10] = 0x00;
            tcpFrame[11] = 0x02;  // FINS帧发送
            tcpFrame[12] = 0x00;  // 错误码
            tcpFrame[13] = 0x00;
            tcpFrame[14] = 0x00;
            tcpFrame[15] = 0x00;

            Array.Copy(finsFrame, 0, tcpFrame, 16, finsFrame.Length);
            return tcpFrame;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 解析W区地址
        /// </summary>
        private bool ParseWAddress(string address, out int wordAddress, out int bitAddress)
        {
            wordAddress = 0;
            bitAddress = 0;

            if (string.IsNullOrEmpty(address)) return false;

            address = address.Trim().ToUpper();
            if (address.StartsWith("W")) address = address.Substring(1);

            var parts = address.Split('.');
            if (parts.Length != 2) return false;

            return int.TryParse(parts[0], out wordAddress) &&
                   int.TryParse(parts[1], out bitAddress) &&
                   bitAddress >= 0 && bitAddress <= 15;
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

        /// <summary>
        /// 获取本机MAC地址
        /// </summary>
        public static string GetLocalMacAddress()
        {
            try
            {
                var nic = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up &&
                                         n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

                if (nic != null)
                {
                    var mac = nic.GetPhysicalAddress().ToString();
                    return string.Join(":", Enumerable.Range(0, 6).Select(i => mac.Substring(i * 2, 2)));
                }
            }
            catch { }

            return "00:00:00:00:00:00";
        }

        #endregion

        #region 连接管理

        /// <summary>
        /// 断开所有连接
        /// </summary>
        public void DisconnectAll()
        {
            foreach (var port in _serialPorts)
            {
                try { if (port.IsOpen) port.Close(); } catch { }
            }
            _serialPorts.Clear();

            foreach (var client in _udpClients)
            {
                try { client.Close(); } catch { }
            }
            _udpClients.Clear();

            foreach (var client in _tcpClients)
            {
                try { client.Close(); } catch { }
            }
            _tcpClients.Clear();

            _writeClients.Clear();
            LogService.Info("所有PLC连接已断开");
        }

        public void Dispose() => DisconnectAll();

        #endregion

        #region 内部类

        private class SerialWriteContext
        {
            public SerialPort Port { get; set; }
            public SerialPortConfig Config { get; set; }
        }

        private class UdpWriteContext
        {
            public UdpClient Client { get; set; }
            public IPEndPoint EndPoint { get; set; }
            public UdpConfig Config { get; set; }
        }

        private class TcpWriteContext
        {
            public TcpClient Client { get; set; }
            public TcpConfig Config { get; set; }
            public byte PcNode { get; set; }
            public byte PlcNode { get; set; }
        }

        #endregion
    }
}
