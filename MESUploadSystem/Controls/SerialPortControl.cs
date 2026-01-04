using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO.Ports;
using System.Windows.Forms;
using MESUploadSystem.Models;

namespace MESUploadSystem.Controls
{
    public class SerialPortControl : UserControl
    {
        public SerialPortConfig Config { get; private set; }

        private ComboBox cboType;
        private ComboBox cboName;
        private ComboBox cboDataBits;
        private ComboBox cboStopBits;
        private ComboBox cboBaudRate;
        private ComboBox cboParity;
        private Label lblTitle;
        private bool _isSettingsMode;

        private readonly Color PrimaryColor = Color.FromArgb(66, 133, 244);
        private readonly Color BorderColor = Color.FromArgb(218, 220, 224);

        public SerialPortControl(SerialPortConfig config, bool isSettingsMode = true)
        {
            Config = config;
            _isSettingsMode = isSettingsMode;
            InitializeComponent();
            BindData();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(200, 320);
            this.BackColor = Color.White;

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            mainPanel.Paint += MainPanel_Paint;

            int y = 45;
            int labelWidth = 55;
            int controlWidth = 110;
            int rowHeight = 38;

            // 标题
            lblTitle = new Label
            {
                Text = Config.Name,
                Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold),
                ForeColor = PrimaryColor,
                Location = new Point(12, 12),
                AutoSize = true
            };
            mainPanel.Controls.Add(lblTitle);

            // 串口类型
            AddLabel(mainPanel, "类型:", 12, y);
            cboType = CreateComboBox(new[] { "L读取", "R读取", "写入" }, labelWidth + 20, y, controlWidth);
            mainPanel.Controls.Add(cboType);
            y += rowHeight;

            // 串口名称
            AddLabel(mainPanel, "名称:", 12, y);
            cboName = CreateComboBox(SerialPort.GetPortNames(), labelWidth + 20, y, controlWidth);
            if (cboName.Items.Count == 0) cboName.Items.Add("COM1");
            mainPanel.Controls.Add(cboName);
            y += rowHeight;

            // 数据位
            AddLabel(mainPanel, "数据位:", 12, y);
            cboDataBits = CreateComboBox(new object[] { 4, 5, 6, 7, 8 }, labelWidth + 20, y, controlWidth);
            mainPanel.Controls.Add(cboDataBits);
            y += rowHeight;

            // 停止位
            AddLabel(mainPanel, "停止位:", 12, y);
            cboStopBits = CreateComboBox(new[] { "None", "One", "Two" }, labelWidth + 20, y, controlWidth);
            mainPanel.Controls.Add(cboStopBits);
            y += rowHeight;

            // 波特率
            AddLabel(mainPanel, "波特率:", 12, y);
            cboBaudRate = CreateComboBox(new object[] { 9600, 19200, 38400, 57600, 115200 }, labelWidth + 20, y, controlWidth);
            mainPanel.Controls.Add(cboBaudRate);
            y += rowHeight;

            // 校验位
            AddLabel(mainPanel, "校验位:", 12, y);
            cboParity = CreateComboBox(new[] { "None", "Odd", "Even", "Mark", "Space" }, labelWidth + 20, y, controlWidth);
            mainPanel.Controls.Add(cboParity);

            if (!_isSettingsMode) SetReadOnly();

            this.Controls.Add(mainPanel);
        }

        private void MainPanel_Paint(object sender, PaintEventArgs e)
        {
            var rect = new Rectangle(0, 0, ((Panel)sender).Width - 1, ((Panel)sender).Height - 1);
            using (var pen = new Pen(BorderColor, 1))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                DrawRoundedRectangle(e.Graphics, pen, rect, 8);
            }
        }

        private void DrawRoundedRectangle(Graphics g, Pen pen, Rectangle rect, int radius)
        {
            using (var path = new GraphicsPath())
            {
                path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
                path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
                path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
                path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
                path.CloseFigure();
                g.DrawPath(pen, path);
            }
        }

        private Label AddLabel(Panel parent, string text, int x, int y)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(x, y + 5),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9F),
                ForeColor = Color.FromArgb(95, 99, 104)
            };
            parent.Controls.Add(lbl);
            return lbl;
        }

        private ComboBox CreateComboBox(object[] items, int x, int y, int width)
        {
            var cbo = new ComboBox
            {
                Location = new Point(x, y),
                Width = width,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft YaHei", 9F)
            };
            cbo.Items.AddRange(items);
            return cbo;
        }

        private void BindData()
        {
            cboType.SelectedItem = Config.PortType;
            cboName.SelectedItem = Config.PortName;
            cboDataBits.SelectedItem = Config.DataBits;
            cboStopBits.SelectedItem = Config.StopBits;
            cboBaudRate.SelectedItem = Config.BaudRate;
            cboParity.SelectedItem = Config.Parity;

            // 设置默认值
            if (cboType.SelectedIndex < 0) cboType.SelectedIndex = 0;
            if (cboName.SelectedIndex < 0) cboName.SelectedIndex = 0;
            if (cboDataBits.SelectedIndex < 0) cboDataBits.SelectedItem = 8;
            if (cboStopBits.SelectedIndex < 0) cboStopBits.SelectedItem = "One";
            if (cboBaudRate.SelectedIndex < 0) cboBaudRate.SelectedItem = 115200;
            if (cboParity.SelectedIndex < 0) cboParity.SelectedItem = "None";
        }

        public void SaveData()
        {
            Config.PortType = cboType.SelectedItem?.ToString() ?? "L读取";
            Config.PortName = cboName.SelectedItem?.ToString() ?? "COM1";
            Config.DataBits = (int)(cboDataBits.SelectedItem ?? 8);
            Config.StopBits = cboStopBits.SelectedItem?.ToString() ?? "One";
            Config.BaudRate = (int)(cboBaudRate.SelectedItem ?? 115200);
            Config.Parity = cboParity.SelectedItem?.ToString() ?? "None";
        }

        public void SetReadOnly()
        {
            cboType.Enabled = false;
            cboName.Enabled = false;
            cboDataBits.Enabled = false;
            cboStopBits.Enabled = false;
            cboBaudRate.Enabled = false;
            cboParity.Enabled = false;
        }
    }
}