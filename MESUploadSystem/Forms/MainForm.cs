using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using MESUploadSystem.Controls;
using MESUploadSystem.Models;
using MESUploadSystem.Services;

namespace MESUploadSystem.Forms
{
    public partial class MainForm : Form
    {
        private AppConfig _config;
        private SerialPortService _serialService;
        private MesApiService _mesService;
        private PlcCommService _plcService;
        private bool _isRunning;

        private Button btnStart, btnStop, btnSettings, btnMesSettings, btnPlcSettings;
        private Panel pnlStatus;
        private Label lblStatus;
        private FlowLayoutPanel pnlComponents;
        private RichTextBox txtLog;

        // 执行结果显示控件
        private Panel pnlResult;
        private Label lblResult;
        private Label lblResultSn;
        private Label lblResultTitle;

        private List<BatchMaterialControl> _materialControls = new List<BatchMaterialControl>();
        private List<SerialPortControl> _portControls = new List<SerialPortControl>();
        private List<UdpControl> _udpControls = new List<UdpControl>();
        private List<TcpControl> _tcpControls = new List<TcpControl>();

        private readonly Color PrimaryColor = Color.FromArgb(66, 133, 244);
        private readonly Color SuccessColor = Color.FromArgb(52, 168, 83);
        private readonly Color DangerColor = Color.FromArgb(234, 67, 53);
        private readonly Color WarningColor = Color.FromArgb(251, 188, 4);
        private readonly Color BackgroundColor = Color.FromArgb(248, 249, 250);

        public MainForm()
        {
            InitializeComponent();
            LoadConfig();

            LogService.OnLog += (msg, isError) =>
            {
                if (txtLog.InvokeRequired)
                    txtLog.Invoke(new Action(() => AppendLog(msg, isError)));
                else
                    AppendLog(msg, isError);
            };

            UpdateButtonStates();
        }

        private void InitializeComponent()
        {
            this.Text = "批次物料绑定过站软件";
            this.Size = new Size(1400, 850);  // 增加高度
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Microsoft YaHei", 9F);
            this.BackColor = BackgroundColor;
            this.MinimumSize = new Size(1100, 700);

            // 顶部面板
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.White,
                Padding = new Padding(20, 0, 20, 0)
            };
            pnlTop.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(230, 230, 230), 1))
                    e.Graphics.DrawLine(pen, 0, pnlTop.Height - 1, pnlTop.Width, pnlTop.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = "📦 批次物料绑定过站软件",
                Font = new Font("Microsoft YaHei", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(32, 33, 36),
                Location = new Point(20, 20),
                AutoSize = true
            };
            pnlTop.Controls.Add(lblTitle);

            // 状态指示器
            pnlStatus = new Panel
            {
                Size = new Size(120, 32),
                Location = new Point(380, 19),
                BackColor = Color.FromArgb(245, 245, 245)
            };
            pnlStatus.Paint += PnlStatus_Paint;

            lblStatus = new Label
            {
                Text = "● 已停止",
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(128, 128, 128),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            pnlStatus.Controls.Add(lblStatus);
            pnlTop.Controls.Add(pnlStatus);

            // 按钮
            int btnX = 530;
            btnStart = CreateButton("▶ 启动", btnX, 17, SuccessColor, 90);
            btnStart.Click += BtnStart_Click;
            pnlTop.Controls.Add(btnStart);

            btnStop = CreateButton("■ 停止", btnX + 100, 17, DangerColor, 90);
            btnStop.Click += BtnStop_Click;
            pnlTop.Controls.Add(btnStop);

            btnSettings = CreateButton("⚙ 软件设置", btnX + 200, 17, PrimaryColor, 100);
            btnSettings.Click += BtnSettings_Click;
            pnlTop.Controls.Add(btnSettings);

            btnMesSettings = CreateButton("🔧 MES设置", btnX + 310, 17, PrimaryColor, 100);
            btnMesSettings.Click += BtnMesSettings_Click;
            pnlTop.Controls.Add(btnMesSettings);

            btnPlcSettings = CreateButton("📡 PLC设置", btnX + 420, 17, PrimaryColor, 100);
            btnPlcSettings.Click += BtnPlcSettings_Click;
            pnlTop.Controls.Add(btnPlcSettings);

            this.Controls.Add(pnlTop);

            // 中部组件面板
            var pnlMiddle = new Panel
            {
                Dock = DockStyle.Top,
                Height = 380,
                Padding = new Padding(15, 10, 15, 10),
                BackColor = BackgroundColor
            };

            pnlComponents = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = BackgroundColor,
                Padding = new Padding(5),
                WrapContents = true
            };
            pnlMiddle.Controls.Add(pnlComponents);
            this.Controls.Add(pnlMiddle);

            // ==================== 底部区域：执行结果 + 日志（独立布局）====================
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15, 5, 15, 15),
                BackColor = BackgroundColor
            };

            // 使用TableLayoutPanel实现独立布局
            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = BackgroundColor
            };

            // 设置列宽比例：执行结果区域25%，日志区域75%
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75F));

            // 设置行高：标题行固定高度，内容行填充剩余空间
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // ---- 执行结果标题 ----
            var pnlResultTitle = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10, 8, 10, 8)
            };
            lblResultTitle = new Label
            {
                Text = "📊 执行结果",
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(32, 33, 36),
                AutoSize = true,
                Location = new Point(10, 8)
            };
            pnlResultTitle.Controls.Add(lblResultTitle);
            tableLayout.Controls.Add(pnlResultTitle, 0, 0);

            // ---- 日志标题 ----
            var pnlLogTitle = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10, 8, 10, 8)
            };
            var lblLogTitle = new Label
            {
                Text = "📋 运行日志",
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(32, 33, 36),
                AutoSize = true,
                Location = new Point(10, 8)
            };
            pnlLogTitle.Controls.Add(lblLogTitle);
            tableLayout.Controls.Add(pnlLogTitle, 1, 0);

            // ---- 执行结果区域 ----
            pnlResult = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 5, 0)
            };
            pnlResult.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(218, 220, 224), 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlResult.Width - 1, pnlResult.Height - 1);
            };

            // 执行结果标签（OK/NG）
            lblResult = new Label
            {
                Text = "待执行",
                Font = new Font("Microsoft YaHei", 48F, FontStyle.Bold),
                ForeColor = Color.FromArgb(128, 128, 128),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            pnlResult.Controls.Add(lblResult);

            // SN显示标签
            lblResultSn = new Label
            {
                Text = "",
                Font = new Font("Consolas", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Bottom,
                Height = 40
            };
            pnlResult.Controls.Add(lblResultSn);

            tableLayout.Controls.Add(pnlResult, 0, 1);

            // ---- 日志区域 ----
            var logContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10),
                Margin = new Padding(5, 0, 0, 0)
            };
            logContainer.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(218, 220, 224), 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, logContainer.Width - 1, logContainer.Height - 1);
            };

            txtLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("Consolas", 9.5F),
                BorderStyle = BorderStyle.None
            };
            logContainer.Controls.Add(txtLog);

            tableLayout.Controls.Add(logContainer, 1, 1);

            pnlBottom.Controls.Add(tableLayout);
            this.Controls.Add(pnlBottom);

            // 设置控件层级顺序
            this.Controls.SetChildIndex(pnlBottom, 0);
            this.Controls.SetChildIndex(pnlMiddle, 1);
            this.Controls.SetChildIndex(pnlTop, 2);
        }

        private void PnlStatus_Paint(object sender, PaintEventArgs e)
        {
            var rect = new Rectangle(0, 0, pnlStatus.Width - 1, pnlStatus.Height - 1);
            using (var path = GetRoundedRectPath(rect, 16))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(pnlStatus.BackColor))
                    e.Graphics.FillPath(brush, path);
            }
        }

        private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }

        private Button CreateButton(string text, int x, int y, Color color, int width = 120)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 36),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btn.Width, btn.Height, 8, 8));
            return btn;
        }

        [System.Runtime.InteropServices.DllImport("Gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

        private void UpdateButtonStates()
        {
            if (_isRunning)
            {
                btnStart.Enabled = false;
                btnStart.BackColor = Color.FromArgb(180, 180, 180);
                btnStop.Enabled = true;
                btnStop.BackColor = DangerColor;
                btnSettings.Enabled = false;
                btnSettings.BackColor = Color.FromArgb(180, 180, 180);
                btnMesSettings.Enabled = false;
                btnMesSettings.BackColor = Color.FromArgb(180, 180, 180);
                btnPlcSettings.Enabled = false;
                btnPlcSettings.BackColor = Color.FromArgb(180, 180, 180);

                pnlStatus.BackColor = Color.FromArgb(232, 245, 233);
                lblStatus.Text = "● 运行中";
                lblStatus.ForeColor = SuccessColor;
            }
            else
            {
                btnStart.Enabled = true;
                btnStart.BackColor = SuccessColor;
                btnStop.Enabled = false;
                btnStop.BackColor = Color.FromArgb(180, 180, 180);
                btnSettings.Enabled = true;
                btnSettings.BackColor = PrimaryColor;
                btnMesSettings.Enabled = true;
                btnMesSettings.BackColor = PrimaryColor;
                btnPlcSettings.Enabled = true;
                btnPlcSettings.BackColor = PrimaryColor;

                pnlStatus.BackColor = Color.FromArgb(245, 245, 245);
                lblStatus.Text = "● 已停止";
                lblStatus.ForeColor = Color.FromArgb(128, 128, 128);
            }
            pnlStatus.Invalidate();
        }

        private void LoadConfig()
        {
            _config = ConfigService.Load();
            RefreshComponents();
        }

        private void RefreshComponents()
        {
            pnlComponents.Controls.Clear();
            _materialControls.Clear();
            _portControls.Clear();
            _udpControls.Clear();
            _tcpControls.Clear();

            foreach (var m in _config.BatchMaterials)
            {
                var ctrl = new BatchMaterialControl(m, false);
                _materialControls.Add(ctrl);
                pnlComponents.Controls.Add(ctrl);
            }

            foreach (var p in _config.SerialPorts)
            {
                var ctrl = new SerialPortControl(p, false);
                _portControls.Add(ctrl);
                pnlComponents.Controls.Add(ctrl);
            }

            foreach (var u in _config.UdpConfigs)
            {
                var ctrl = new UdpControl(u, false);
                _udpControls.Add(ctrl);
                pnlComponents.Controls.Add(ctrl);
            }

            foreach (var t in _config.TcpConfigs)
            {
                var ctrl = new TcpControl(t, false);
                _tcpControls.Add(ctrl);
                pnlComponents.Controls.Add(ctrl);
            }

            if (pnlComponents.Controls.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = "暂无配置，请点击「软件设置」添加配置",
                    Font = new Font("Microsoft YaHei", 11F),
                    ForeColor = Color.FromArgb(128, 128, 128),
                    AutoSize = true,
                    Padding = new Padding(50, 100, 50, 100)
                };
                pnlComponents.Controls.Add(lblEmpty);
            }
        }

        private void AppendLog(string message, bool isError = false)
        {
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionColor = isError ? DangerColor : Color.FromArgb(32, 33, 36);
            txtLog.AppendText(message + Environment.NewLine);
            txtLog.ScrollToCaret();
        }

        /// <summary>
        /// 更新执行结果显示
        /// </summary>
        private void UpdateResultDisplay(string status, string sn)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateResultDisplay(status, sn)));
                return;
            }

            lblResultSn.Text = sn;

            switch (status.ToUpper())
            {
                case "OK":
                    lblResult.Text = "OK";
                    lblResult.ForeColor = SuccessColor;
                    pnlResult.BackColor = Color.FromArgb(232, 245, 233);
                    break;
                case "NG":
                    lblResult.Text = "NG";
                    lblResult.ForeColor = DangerColor;
                    pnlResult.BackColor = Color.FromArgb(255, 235, 238);
                    break;
                case "PROCESSING":
                    lblResult.Text = "处理中";
                    lblResult.ForeColor = WarningColor;
                    pnlResult.BackColor = Color.FromArgb(255, 248, 225);
                    lblResult.Font = new Font("Microsoft YaHei", 24F, FontStyle.Bold);
                    break;
                default:
                    lblResult.Text = "待执行";
                    lblResult.ForeColor = Color.FromArgb(128, 128, 128);
                    pnlResult.BackColor = Color.White;
                    break;
            }

            if (status.ToUpper() == "OK" || status.ToUpper() == "NG")
            {
                lblResult.Font = new Font("Microsoft YaHei", 48F, FontStyle.Bold);
            }

            pnlResult.Invalidate();
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            using (var form = new SettingsForm(_config))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    ConfigService.Save(_config);
                    RefreshComponents();
                    LogService.Info("✓ 软件设置已保存");
                }
            }
        }

        private void BtnMesSettings_Click(object sender, EventArgs e)
        {
            using (var form = new MesSettingsForm(_config.MesConfig))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    ConfigService.Save(_config);

                    if (_serialService != null)
                    {
                        _serialService.SetMesConfig(_config.MesConfig);
                        LogService.Info("✓ MES设置已保存并更新到串口服务");
                    }
                    else
                    {
                        LogService.Info("✓ MES设置已保存");
                    }
                }
            }
        }

        private void BtnPlcSettings_Click(object sender, EventArgs e)
        {
            using (var form = new PlcSettingsForm(_config.PlcSignalConfig))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    ConfigService.Save(_config);
                    LogService.Info("✓ PLC设置已保存");
                }
            }
        }

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            if (_isRunning) return;

            SyncControlsToConfig();

            if (!ValidateConfig()) return;

            try
            {
                _mesService = new MesApiService(_config.MesConfig);

                // 初始化串口
                var readPorts = _config.SerialPorts.Where(p => p.PortType != "写入").ToList();
                if (readPorts.Count > 0)
                {
                    _serialService = new SerialPortService(_config.MesConfig);
                    _serialService.OnDataReceived += SerialService_OnDataReceived;

                    if (!_serialService.OpenPorts(readPorts))
                    {
                        MessageBox.Show("读取串口打开失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                // 初始化PLC通信
                _plcService = new PlcCommService(_config.UdpConfigs, _config.TcpConfigs, _config.SerialPorts);
                await _plcService.ConnectAllAsync();

                _isRunning = true;
                UpdateButtonStates();
                UpdateResultDisplay("WAITING", "");
                LogService.Info("═══════════════════════════════════════");
                LogService.Info("✓ 软件启动成功");
                if (_config.MesConfig.BindAllMaterialsToSameProduct)
                {
                    LogService.Info("📦 批次物料绑定模式：所有物料绑定同一产品");
                }
                else
                {
                    LogService.Info("📦 批次物料绑定模式：单物料绑定");
                }
                LogService.Info("═══════════════════════════════════════");
            }
            catch (Exception ex)
            {
                LogService.Error($"启动失败: {ex.Message}");
                MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SyncControlsToConfig()
        {
            foreach (var ctrl in _materialControls) ctrl.SaveData();
        }

        private bool ValidateConfig()
        {
            if (_materialControls.Count == 0)
            {
                ShowValidationError("请至少配置一个批次物料");
                return false;
            }

            foreach (var ctrl in _materialControls)
            {
                if (string.IsNullOrWhiteSpace(ctrl.GetPackageCode()))
                {
                    ShowValidationError($"{ctrl.Material.Name} 的包装编号不能为空");
                    return false;
                }
            }

            var mes = _config.MesConfig;
            if (string.IsNullOrWhiteSpace(mes.MesUrl) || string.IsNullOrWhiteSpace(mes.MesToken))
            {
                ShowValidationError("请完善MES设置信息");
                return false;
            }

            return true;
        }

        private void ShowValidationError(string message)
        {
            LogService.Error($"验证失败: {message}");
            MessageBox.Show(message, "配置检查", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private async void SerialService_OnDataReceived(string portType, string sn)
        {
            if (!_isRunning) return;

            try
            {
                await ProcessSnAsync(portType, sn);
            }
            catch (Exception ex)
            {
                LogService.Error($"处理异常: {ex.Message}");
                UpdateResultDisplay("NG", sn);
                await SendPlcSignalAsync(portType, false);
            }
        }

        private async Task ProcessSnAsync(string portType, string sn)
        {
            bool isLeft = portType == "L读取";
            string position = isLeft ? "L" : "R";
            string stationName = isLeft ? _config.MesConfig.LStationName : _config.MesConfig.RStationName;
            string deviceCode = isLeft ? _config.MesConfig.LDeviceCode : _config.MesConfig.RDeviceCode;

            LogService.Info($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            LogService.Info($"开始处理 [{portType}] 产品: {sn}");
            UpdateResultDisplay("PROCESSING", sn);

            // 判断是否启用"批次物料绑定同一产品"模式
            bool bindAllMaterials = _config.MesConfig.BindAllMaterialsToSameProduct;

            // 根据模式获取需要处理的物料控件
            List<BatchMaterialControl> materialsToProcess;
            if (bindAllMaterials)
            {
                // 获取所有批次物料
                materialsToProcess = _materialControls.ToList();
                LogService.Info($"绑定模式：所有批次物料绑定同一产品，共 {materialsToProcess.Count} 个物料");
            }
            else
            {
                // 仅获取对应位置的批次物料
                var materialCtrl = _materialControls.FirstOrDefault(m => m.Material.Position == position);
                if (materialCtrl == null)
                {
                    LogService.Error($"未找到位置 {position} 的批次物料配置");
                    UpdateResultDisplay("NG", sn);
                    await SendPlcSignalAsync(portType, false);
                    return;
                }
                materialsToProcess = new List<BatchMaterialControl> { materialCtrl };
            }

            // 1. 查询产品信息
            var snResult = await _mesService.GetSnInfoAsync(sn);

            string shoporder = null;

            if (snResult.RESULT != "PASS")
            {
                // 检查是否启用产品收录
                if (_config.MesConfig.EnableProductRecruit &&
                    snResult.MESSAGE != null && snResult.MESSAGE.Contains("没有查询到数据"))
                {
                    LogService.Info("SN不存在，尝试产品收录...");

                    string macAddress = PlcCommService.GetLocalMacAddress();
                    LogService.Info($"本机MAC地址: {macAddress}");

                    var shoporderResult = await _mesService.GetSettingShoporderAsync(stationName, macAddress);
                    if (shoporderResult.RESULT != "PASS")
                    {
                        LogService.Error($"获取工单失败: {shoporderResult.MESSAGE}");
                        UpdateResultDisplay("NG", sn);
                        await SendPlcSignalAsync(portType, false);
                        return;
                    }

                    shoporder = shoporderResult.DATA?.shoporder;
                    LogService.Info($"✓ 获取工单成功: {shoporder}");

                    var recruitResult = await _mesService.RecruitShoporderSnAsync(sn, stationName, shoporder);
                    if (recruitResult.RESULT != "PASS")
                    {
                        LogService.Error($"产品收录失败: {recruitResult.MESSAGE}");
                        UpdateResultDisplay("NG", sn);
                        await SendPlcSignalAsync(portType, false);
                        return;
                    }
                    LogService.Info("✓ 产品收录成功");
                }
                else
                {
                    LogService.Error($"产品查询失败: {snResult.MESSAGE}");
                    UpdateResultDisplay("NG", sn);
                    await SendPlcSignalAsync(portType, false);
                    return;
                }
            }
            else
            {
                shoporder = snResult.DATA.shoporder;
                LogService.Info($"✓ 产品查询成功, 工单: {shoporder}");
            }

            // 2. 产品检查 (Start)
            var startResult = await _mesService.StartAsync(sn, shoporder, stationName, deviceCode);
            if (startResult.RESULT != "PASS")
            {
                LogService.Error($"产品检查失败: {startResult.MESSAGE}");
                UpdateResultDisplay("NG", sn);
                await SendPlcSignalAsync(portType, false);
                return;
            }
            LogService.Info("✓ 产品检查通过");

            // 3. 物料绑定
            MesAssemblyResponse assemblyResult;

            if (bindAllMaterials)
            {
                // 所有批次物料绑定同一产品模式
                var compSnList = materialsToProcess.Select(m => m.GetPackageCode()).ToList();
                LogService.Info($"准备绑定 {compSnList.Count} 个批次物料: {string.Join(", ", compSnList)}");

                assemblyResult = await _mesService.AssemblyMultiCompSnAsync(
                    sn,
                    deviceCode,
                    stationName,
                    compSnList);
            }
            else
            {
                // 单物料绑定模式（原有逻辑）
                string packageCode = materialsToProcess[0].GetPackageCode();
                assemblyResult = await _mesService.AssemblyCompSnAsync(
                    sn,
                    deviceCode,
                    stationName,
                    packageCode);
            }

            // 【重要】不论接口是否成功，都需要扣减剩余用量
            // 先扣减用量，再判断接口结果
            foreach (var materialCtrl in materialsToProcess)
            {
                if (materialCtrl.IsControlUsageEnabled())
                {
                    var currentRemaining = materialCtrl.GetRemainingUsage();
                    if (currentRemaining.HasValue)
                    {
                        double qty = materialCtrl.GetUnitUsage();
                        double newRemaining = currentRemaining.Value - qty;

                        this.Invoke(new Action(() =>
                        {
                            materialCtrl.SetRemainingUsage(newRemaining);
                            materialCtrl.SaveData();
                        }));

                        LogService.Info($"{materialCtrl.Material.Name} 剩余用量: {currentRemaining:F2} → {newRemaining:F2}");

                        if (newRemaining <= 0)
                        {
                            LogService.Info($"⚠️ {materialCtrl.Material.Name} 剩余用量已用完！");
                        }
                    }
                }
            }

            // 保存配置
            this.Invoke(new Action(() =>
            {
                ConfigService.Save(_config);
            }));

            // 判断绑定结果
            if (assemblyResult.RESULT != "PASS")
            {
                LogService.Error($"物料绑定失败: {assemblyResult.MESSAGE}");
                UpdateResultDisplay("NG", sn);
                await SendPlcSignalAsync(portType, false);

                // 检查是否有物料用完，需要停止
                CheckMaterialEmpty(materialsToProcess);
                return;
            }
            LogService.Info("✓ 物料绑定成功");

            // 4. 产品完成
            var completeResult = await _mesService.CompleteAsync(sn, shoporder, stationName, deviceCode);
            if (completeResult.RESULT != "PASS")
            {
                LogService.Error($"产品完成失败: {completeResult.MESSAGE}");
                UpdateResultDisplay("NG", sn);
                await SendPlcSignalAsync(portType, false);

                // 检查是否有物料用完，需要停止
                CheckMaterialEmpty(materialsToProcess);
                return;
            }
            LogService.Info("✓ 产品完成成功");

            // 检查是否有物料用完，需要停止
            if (CheckMaterialEmpty(materialsToProcess))
            {
                return;
            }

            // 成功
            LogService.Info($"══════ 产品 {sn} 过站成功 ══════");
            UpdateResultDisplay("OK", sn);
            await SendPlcSignalAsync(portType, true);
        }

        /// <summary>
        /// 检查物料是否用完，如果用完则提示并停止
        /// </summary>
        /// <returns>是否有物料用完</returns>
        private bool CheckMaterialEmpty(List<BatchMaterialControl> materialsToCheck)
        {
            foreach (var materialCtrl in materialsToCheck)
            {
                if (materialCtrl.IsControlUsageEnabled())
                {
                    var remaining = materialCtrl.GetRemainingUsage();
                    if (remaining.HasValue && remaining.Value <= 0)
                    {
                        this.Invoke(new Action(() =>
                        {
                            MessageBox.Show($"⚠️ {materialCtrl.Material.Name} 剩余用量已用完！",
                                "物料用完", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            StopSystem();
                        }));

                        LogService.Error($"⚠️ {materialCtrl.Material.Name} 剩余用量为0，系统已停止");
                        return true;
                    }
                }
            }
            return false;
        }

        private async Task SendPlcSignalAsync(string portType, bool success)
        {
            if (_plcService == null) return;

            string trigger = portType == "L读取"
                ? (success ? "L读取执行成功" : "L读取执行失败")
                : (success ? "R读取执行成功" : "R读取执行失败");

            var signal = _config.PlcSignalConfig.GetSignal(trigger);
            if (signal != null)
            {
                await _plcService.SendSignalAsync(signal);
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            if (!_isRunning) return;
            StopSystem();
        }

        private void StopSystem()
        {
            _isRunning = false;
            _serialService?.ClosePorts();
            _serialService = null;
            _plcService?.DisconnectAll();
            _plcService = null;

            UpdateButtonStates();
            UpdateResultDisplay("WAITING", "");
            LogService.Info("═══════════════════════════════════════");
            LogService.Info("■ 软件已停止");
            LogService.Info("═══════════════════════════════════════");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _isRunning = false;
            _serialService?.Dispose();
            _plcService?.Dispose();

            SyncControlsToConfig();
            ConfigService.Save(_config);

            base.OnFormClosing(e);
        }
    }
}
