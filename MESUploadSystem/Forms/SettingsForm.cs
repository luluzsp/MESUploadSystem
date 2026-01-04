using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MESUploadSystem.Controls;
using MESUploadSystem.Models;

namespace MESUploadSystem.Forms
{
    public partial class SettingsForm : Form
    {
        private AppConfig _config;
        private FlowLayoutPanel pnlComponents;
        private List<BatchMaterialControl> _materialControls = new List<BatchMaterialControl>();
        private List<SerialPortControl> _portControls = new List<SerialPortControl>();
        private List<UdpControl> _udpControls = new List<UdpControl>();
        private List<TcpControl> _tcpControls = new List<TcpControl>();

        private int _materialIndex = 0;
        private int _portIndex = 0;
        private int _udpIndex = 0;
        private int _tcpIndex = 0;

        private readonly Color PrimaryColor = Color.FromArgb(66, 133, 244);
        private readonly Color SuccessColor = Color.FromArgb(52, 168, 83);
        private readonly Color DangerColor = Color.FromArgb(234, 67, 53);
        private readonly Color PurpleColor = Color.FromArgb(156, 39, 176);
        private readonly Color TealColor = Color.FromArgb(0, 150, 136);
        private readonly Color BackgroundColor = Color.FromArgb(248, 249, 250);

        public SettingsForm(AppConfig config)
        {
            _config = config;
            InitializeComponent();
            LoadExistingConfig();
        }

        private void InitializeComponent()
        {
            this.Text = "软件设置";
            this.Size = new Size(1250, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Microsoft YaHei", 9F);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = BackgroundColor;

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
                Text = "⚙ 软件设置",
                Font = new Font("Microsoft YaHei", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(32, 33, 36),
                Location = new Point(20, 20),
                AutoSize = true
            };
            pnlTop.Controls.Add(lblTitle);

            int btnX = 180;
            int btnY = 18;
            int btnW = 95;

            var btnAddMaterial = CreateButton("➕ 批次物料", btnX, btnY, PrimaryColor, btnW);
            btnAddMaterial.Click += BtnAddMaterial_Click;
            pnlTop.Controls.Add(btnAddMaterial);

            var btnAddPort = CreateButton("➕ 串口", btnX + 105, btnY, PrimaryColor, 80);
            btnAddPort.Click += BtnAddPort_Click;
            pnlTop.Controls.Add(btnAddPort);

            var btnAddUdp = CreateButton("➕ UDP", btnX + 195, btnY, PurpleColor, 80);
            btnAddUdp.Click += BtnAddUdp_Click;
            pnlTop.Controls.Add(btnAddUdp);

            var btnAddTcp = CreateButton("➕ TCP", btnX + 285, btnY, TealColor, 80);
            btnAddTcp.Click += BtnAddTcp_Click;
            pnlTop.Controls.Add(btnAddTcp);

            var btnReset = CreateButton("🔄 重置", btnX + 385, btnY, DangerColor, 80);
            btnReset.Click += BtnReset_Click;
            pnlTop.Controls.Add(btnReset);

            var btnSave = CreateButton("💾 保存", btnX + 475, btnY, SuccessColor, 80);
            btnSave.Click += BtnSave_Click;
            pnlTop.Controls.Add(btnSave);

            this.Controls.Add(pnlTop);

            // 组件面板
            pnlComponents = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(15),
                BackColor = BackgroundColor,
                WrapContents = true
            };
            this.Controls.Add(pnlComponents);

            this.Controls.SetChildIndex(pnlComponents, 0);
            this.Controls.SetChildIndex(pnlTop, 1);
        }

        private Button CreateButton(string text, int x, int y, Color color, int width)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 34),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Microsoft YaHei", 9F)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void LoadExistingConfig()
        {
            // 加载批次物料
            foreach (var m in _config.BatchMaterials)
            {
                _materialIndex = Math.Max(_materialIndex, m.Index);
                var ctrl = new BatchMaterialControl(m, true);
                _materialControls.Add(ctrl);
                pnlComponents.Controls.Add(ctrl);
            }

            // 加载串口
            foreach (var p in _config.SerialPorts)
            {
                _portIndex = Math.Max(_portIndex, p.Index);
                var ctrl = new SerialPortControl(p, true);
                _portControls.Add(ctrl);
                pnlComponents.Controls.Add(ctrl);
            }

            // 加载UDP
            foreach (var u in _config.UdpConfigs)
            {
                _udpIndex = Math.Max(_udpIndex, u.Index);
                var ctrl = new UdpControl(u, true);
                _udpControls.Add(ctrl);
                pnlComponents.Controls.Add(ctrl);
            }

            // 加载TCP
            foreach (var t in _config.TcpConfigs)
            {
                _tcpIndex = Math.Max(_tcpIndex, t.Index);
                var ctrl = new TcpControl(t, true);
                _tcpControls.Add(ctrl);
                pnlComponents.Controls.Add(ctrl);
            }
        }

        private void BtnAddMaterial_Click(object sender, EventArgs e)
        {
            _materialIndex++;
            var m = new BatchMaterial { Index = _materialIndex };
            var ctrl = new BatchMaterialControl(m, true);
            _materialControls.Add(ctrl);
            pnlComponents.Controls.Add(ctrl);
        }

        private void BtnAddPort_Click(object sender, EventArgs e)
        {
            _portIndex++;
            var p = new SerialPortConfig { Index = _portIndex };
            var ctrl = new SerialPortControl(p, true);
            _portControls.Add(ctrl);
            pnlComponents.Controls.Add(ctrl);
        }

        private void BtnAddUdp_Click(object sender, EventArgs e)
        {
            _udpIndex++;
            var u = new UdpConfig { Index = _udpIndex };
            var ctrl = new UdpControl(u, true);
            _udpControls.Add(ctrl);
            pnlComponents.Controls.Add(ctrl);
        }

        private void BtnAddTcp_Click(object sender, EventArgs e)
        {
            _tcpIndex++;
            var t = new TcpConfig { Index = _tcpIndex };
            var ctrl = new TcpControl(t, true);
            _tcpControls.Add(ctrl);
            pnlComponents.Controls.Add(ctrl);
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要清空所有配置吗？", "确认重置",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                pnlComponents.Controls.Clear();
                _materialControls.Clear();
                _portControls.Clear();
                _udpControls.Clear();
                _tcpControls.Clear();
                _materialIndex = _portIndex = _udpIndex = _tcpIndex = 0;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // 保存批次物料
            _config.BatchMaterials.Clear();
            foreach (var ctrl in _materialControls)
            {
                ctrl.SaveData();
                _config.BatchMaterials.Add(ctrl.Material);
            }

            // 保存串口
            _config.SerialPorts.Clear();
            foreach (var ctrl in _portControls)
            {
                ctrl.SaveData();
                _config.SerialPorts.Add(ctrl.Config);
            }

            // 保存UDP
            _config.UdpConfigs.Clear();
            foreach (var ctrl in _udpControls)
            {
                ctrl.SaveData();
                _config.UdpConfigs.Add(ctrl.Config);
            }

            // 保存TCP
            _config.TcpConfigs.Clear();
            foreach (var ctrl in _tcpControls)
            {
                ctrl.SaveData();
                _config.TcpConfigs.Add(ctrl.Config);
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}