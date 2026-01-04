using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MESUploadSystem.Models;

namespace MESUploadSystem.Controls
{
    public class TcpControl : UserControl
    {
        public TcpConfig Config { get; private set; }

        private ComboBox cboType;
        private TextBox txtIp;
        private TextBox txtPort;
        private Label lblTitle;
        private bool _isSettingsMode;

        private readonly Color PrimaryColor = Color.FromArgb(0, 150, 136);  // 青色
        private readonly Color BorderColor = Color.FromArgb(218, 220, 224);

        public TcpControl(TcpConfig config, bool isSettingsMode = true)
        {
            Config = config;
            _isSettingsMode = isSettingsMode;
            InitializeComponent();
            BindData();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(200, 220);
            this.BackColor = Color.White;

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            mainPanel.Paint += MainPanel_Paint;

            int y = 45;
            int labelWidth = 45;
            int controlWidth = 120;
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

            // TCP类型
            AddLabel(mainPanel, "类型:", 12, y);
            cboType = CreateComboBox(new[] { "L读取", "R读取", "写入" }, labelWidth + 20, y, controlWidth);
            mainPanel.Controls.Add(cboType);
            y += rowHeight;

            // IP地址
            AddLabel(mainPanel, "IP:", 12, y);
            txtIp = CreateTextBox(labelWidth + 20, y, controlWidth);
            mainPanel.Controls.Add(txtIp);
            y += rowHeight;

            // 端口
            AddLabel(mainPanel, "端口:", 12, y);
            txtPort = CreateTextBox(labelWidth + 20, y, controlWidth);
            mainPanel.Controls.Add(txtPort);

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

            // 顶部青色条
            using (var brush = new SolidBrush(PrimaryColor))
            {
                e.Graphics.FillRectangle(brush, 1, 1, ((Panel)sender).Width - 2, 4);
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

        private ComboBox CreateComboBox(string[] items, int x, int y, int width)
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

        private TextBox CreateTextBox(int x, int y, int width)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Width = width,
                Font = new Font("Microsoft YaHei", 9F),
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private void BindData()
        {
            cboType.SelectedItem = Config.CommType ?? "写入";
            txtIp.Text = Config.IpAddress ?? "192.168.1.10";
            txtPort.Text = Config.Port.ToString();

            if (cboType.SelectedIndex < 0) cboType.SelectedIndex = 2;
        }

        public void SaveData()
        {
            Config.CommType = cboType.SelectedItem?.ToString() ?? "写入";
            Config.IpAddress = txtIp.Text.Trim();
            if (int.TryParse(txtPort.Text, out int port)) Config.Port = port;
        }

        public void SetReadOnly()
        {
            cboType.Enabled = false;
            txtIp.ReadOnly = true;
            txtPort.ReadOnly = true;
        }
    }
}