using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MESUploadSystem.Models;

namespace MESUploadSystem.Forms
{
    public partial class MesSettingsForm : Form
    {
        private MesConfig _config;

        private TextBox txtMesUrl;
        private TextBox txtMesToken;
        private TextBox txtLStation;
        private TextBox txtLDevice;
        private TextBox txtRStation;
        private TextBox txtRDevice;
        private CheckBox chkProductRecruit;
        private CheckBox chkRemoveSnSuffix;
        private CheckBox chkBindAllMaterials;  // 新增：批次物料绑定同一产品

        private readonly Color PrimaryColor = Color.FromArgb(66, 133, 244);
        private readonly Color SuccessColor = Color.FromArgb(52, 168, 83);

        public MesSettingsForm(MesConfig config)
        {
            _config = config;
            InitializeComponent();
            BindData();
        }

        private void InitializeComponent()
        {
            this.Text = "MES设置";
            this.Size = new Size(550, 820);  // 增加高度以容纳新选项和按钮
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Microsoft YaHei", 9F);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);

            var mainPanel = new Panel
            {
                Size = new Size(500, 760),  // 增加面板高度
                Location = new Point(25, 20),
                BackColor = Color.White
            };
            mainPanel.Paint += MainPanel_Paint;

            int y = 25;
            int labelX = 25;
            int textX = 130;
            int textWidth = 320;
            int rowHeight = 38;

            // 标题
            var lblTitle = new Label
            {
                Text = "🔧 MES系统配置",
                Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(32, 33, 36),
                Location = new Point(labelX, y),
                AutoSize = true
            };
            mainPanel.Controls.Add(lblTitle);
            y += 40;

            // 接口设置
            AddSectionLabel(mainPanel, "接口设置", labelX, y);
            y += 28;

            AddRow(mainPanel, "MES网址:", labelX, textX, ref y, out txtMesUrl, textWidth, rowHeight);

            // MES Token - 使用密码输入框
            AddPasswordRow(mainPanel, "MES Token:", labelX, textX, ref y, out txtMesToken, textWidth, rowHeight);

            y += 8;
            AddSectionLabel(mainPanel, "L边工站", labelX, y);
            y += 28;

            AddRow(mainPanel, "工站名称:", labelX, textX, ref y, out txtLStation, textWidth, rowHeight);
            AddRow(mainPanel, "设备编号:", labelX, textX, ref y, out txtLDevice, textWidth, rowHeight);

            y += 8;
            AddSectionLabel(mainPanel, "R边工站", labelX, y);
            y += 28;

            AddRow(mainPanel, "工站名称:", labelX, textX, ref y, out txtRStation, textWidth, rowHeight);
            AddRow(mainPanel, "设备编号:", labelX, textX, ref y, out txtRDevice, textWidth, rowHeight);

            // 高级选项
            y += 15;
            AddSectionLabel(mainPanel, "高级选项", labelX, y);
            y += 30;

            chkProductRecruit = new CheckBox
            {
                Text = "启用产品收录功能（SN不存在时自动收录）",
                Location = new Point(labelX, y),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9F),
                ForeColor = Color.FromArgb(66, 66, 66)
            };
            mainPanel.Controls.Add(chkProductRecruit);

            // 提示（在启用产品收录功能下面）
            y += 28;
            var lblTip = new Label
            {
                Text = "💡 PLC信号配置请在主界面「PLC设置」中进行",
                Location = new Point(labelX + 20, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(150, 150, 150),
                Font = new Font("Microsoft YaHei", 8.5F)
            };
            mainPanel.Controls.Add(lblTip);

            // 去除SN后缀选项
            y += 35;
            chkRemoveSnSuffix = new CheckBox
            {
                Text = "去除SN后缀（去除最后一个+号后的内容）",
                Location = new Point(labelX, y),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9F),
                ForeColor = Color.FromArgb(66, 66, 66)
            };
            mainPanel.Controls.Add(chkRemoveSnSuffix);

            // SN处理示例说明
            y += 25;
            var lblSnExample = new Label
            {
                Text = "    示例：FM71234+56AT+ANB → FM71234+56AT",
                Location = new Point(labelX, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(150, 150, 150),
                Font = new Font("Microsoft YaHei", 8.5F, FontStyle.Italic)
            };
            mainPanel.Controls.Add(lblSnExample);

            // 新增：批次物料绑定同一产品选项
            y += 40;
            chkBindAllMaterials = new CheckBox
            {
                Text = "批次物料绑定同一产品",
                Location = new Point(labelX, y),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9F),
                ForeColor = Color.FromArgb(66, 66, 66)
            };
            mainPanel.Controls.Add(chkBindAllMaterials);

            // 批次物料绑定说明
            y += 25;
            var lblBindTip1 = new Label
            {
                Text = "    启用后，所有批次物料将绑定到同一产品SN",
                Location = new Point(labelX, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(150, 150, 150),
                Font = new Font("Microsoft YaHei", 8.5F)
            };
            mainPanel.Controls.Add(lblBindTip1);

            y += 20;
            var lblBindTip2 = new Label
            {
                Text = "    使用comp_sn1, comp_sn2, comp_sn3...参数传递",
                Location = new Point(labelX, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(150, 150, 150),
                Font = new Font("Microsoft YaHei", 8.5F, FontStyle.Italic)
            };
            mainPanel.Controls.Add(lblBindTip2);

            y += 70;

            // 按钮
            var btnCancel = CreateButton("取消", 140, y, Color.FromArgb(108, 117, 125), 110);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            mainPanel.Controls.Add(btnCancel);

            var btnSave = CreateButton("💾 保存", 270, y, SuccessColor, 110);
            btnSave.Click += BtnSave_Click;
            mainPanel.Controls.Add(btnSave);

            this.Controls.Add(mainPanel);
        }

        private void MainPanel_Paint(object sender, PaintEventArgs e)
        {
            var rect = new Rectangle(0, 0, ((Panel)sender).Width - 1, ((Panel)sender).Height - 1);
            using (var pen = new Pen(Color.FromArgb(218, 220, 224), 1))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

        private void AddSectionLabel(Panel parent, string text, int x, int y)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold),
                ForeColor = PrimaryColor,
                Location = new Point(x, y),
                AutoSize = true
            };
            parent.Controls.Add(lbl);

            var line = new Panel
            {
                Location = new Point(x + 70, y + 8),
                Size = new Size(380, 1),
                BackColor = Color.FromArgb(230, 230, 230)
            };
            parent.Controls.Add(line);
        }

        private void AddRow(Panel parent, string labelText, int labelX, int textX, ref int y,
            out TextBox textBox, int textWidth, int rowHeight)
        {
            var label = new Label
            {
                Text = labelText,
                Location = new Point(labelX, y + 4),
                AutoSize = true,
                ForeColor = Color.FromArgb(95, 99, 104)
            };
            parent.Controls.Add(label);

            textBox = new TextBox
            {
                Location = new Point(textX, y),
                Width = textWidth,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Microsoft YaHei", 9F)
            };
            parent.Controls.Add(textBox);

            y += rowHeight;
        }

        /// <summary>
        /// 添加密码输入行
        /// </summary>
        private void AddPasswordRow(Panel parent, string labelText, int labelX, int textX, ref int y,
            out TextBox textBox, int textWidth, int rowHeight)
        {
            var label = new Label
            {
                Text = labelText,
                Location = new Point(labelX, y + 4),
                AutoSize = true,
                ForeColor = Color.FromArgb(95, 99, 104)
            };
            parent.Controls.Add(label);

            textBox = new TextBox
            {
                Location = new Point(textX, y),
                Width = textWidth,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Microsoft YaHei", 9F),
                UseSystemPasswordChar = true  // 密码输入模式
            };
            parent.Controls.Add(textBox);

            y += rowHeight;
        }

        private Button CreateButton(string text, int x, int y, Color color, int width)
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
                Font = new Font("Microsoft YaHei", 9F)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void BindData()
        {
            txtMesUrl.Text = _config.MesUrl ?? "";
            txtMesToken.Text = _config.MesToken ?? "";
            txtLStation.Text = _config.LStationName ?? "";
            txtLDevice.Text = _config.LDeviceCode ?? "";
            txtRStation.Text = _config.RStationName ?? "";
            txtRDevice.Text = _config.RDeviceCode ?? "";
            chkProductRecruit.Checked = _config.EnableProductRecruit;
            chkRemoveSnSuffix.Checked = _config.RemoveSnSuffix;
            chkBindAllMaterials.Checked = _config.BindAllMaterialsToSameProduct;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            _config.MesUrl = txtMesUrl.Text.Trim();
            _config.MesToken = txtMesToken.Text.Trim();
            _config.LStationName = txtLStation.Text.Trim();
            _config.LDeviceCode = txtLDevice.Text.Trim();
            _config.RStationName = txtRStation.Text.Trim();
            _config.RDeviceCode = txtRDevice.Text.Trim();
            _config.EnableProductRecruit = chkProductRecruit.Checked;
            _config.RemoveSnSuffix = chkRemoveSnSuffix.Checked;
            _config.BindAllMaterialsToSameProduct = chkBindAllMaterials.Checked;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
