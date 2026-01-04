using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MESUploadSystem.Models;

namespace MESUploadSystem.Controls
{
    public class BatchMaterialControl : UserControl
    {
        public BatchMaterial Material { get; private set; }

        private ComboBox cboPosition;
        private ComboBox cboControl;
        private TextBox txtCapacity;
        private TextBox txtUnitUsage;
        private TextBox txtRemaining;
        private TextBox txtPackageCode;
        private Button btnToggle;
        private Label lblTitle;
        private bool _isSettingsMode;

        // 颜色主题
        private readonly Color PrimaryColor = Color.FromArgb(66, 133, 244);
        private readonly Color SuccessColor = Color.FromArgb(52, 168, 83);
        private readonly Color WarningColor = Color.FromArgb(251, 188, 4);
        private readonly Color DangerColor = Color.FromArgb(234, 67, 53);
        private readonly Color BackgroundColor = Color.FromArgb(248, 249, 250);
        private readonly Color BorderColor = Color.FromArgb(218, 220, 224);

        public BatchMaterialControl(BatchMaterial material, bool isSettingsMode = true)
        {
            Material = material;
            _isSettingsMode = isSettingsMode;
            InitializeComponent();
            BindData();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(220, 320);
            this.BackColor = Color.White;
            this.Padding = new Padding(1);

            // 主面板
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            mainPanel.Paint += MainPanel_Paint;

            int y = 45;
            int labelWidth = 65;
            int controlWidth = 115;
            int rowHeight = 38;

            // 标题
            lblTitle = new Label
            {
                Text = Material.Name,
                Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold),
                ForeColor = PrimaryColor,
                Location = new Point(12, 12),
                AutoSize = true
            };
            mainPanel.Controls.Add(lblTitle);

            // 上料位置
            AddLabel(mainPanel, "上料位置:", 12, y);
            cboPosition = CreateComboBox(new[] { "L", "R" }, labelWidth + 20, y, controlWidth);
            mainPanel.Controls.Add(cboPosition);
            y += rowHeight;

            // 管控用量
            AddLabel(mainPanel, "管控用量:", 12, y);
            cboControl = CreateComboBox(new[] { "Y", "N" }, labelWidth + 20, y, controlWidth);
            cboControl.SelectedIndexChanged += CboControl_Changed;
            mainPanel.Controls.Add(cboControl);
            y += rowHeight;

            // 包装容量
            AddLabel(mainPanel, "包装容量:", 12, y);
            txtCapacity = CreateTextBox(labelWidth + 20, y, controlWidth);
            mainPanel.Controls.Add(txtCapacity);
            y += rowHeight;

            // 单位用量
            AddLabel(mainPanel, "单位用量:", 12, y);
            txtUnitUsage = CreateTextBox(labelWidth + 20, y, controlWidth);
            mainPanel.Controls.Add(txtUnitUsage);
            y += rowHeight;

            // 剩余用量
            AddLabel(mainPanel, "剩余用量:", 12, y);
            txtRemaining = CreateTextBox(labelWidth + 20, y, controlWidth);
            txtRemaining.BackColor = Color.FromArgb(255, 243, 224);
            mainPanel.Controls.Add(txtRemaining);
            y += rowHeight;

            // 包装编号
            AddLabel(mainPanel, "包装编号:", 12, y);
            txtPackageCode = CreateTextBox(labelWidth + 20, y, controlWidth);
            txtPackageCode.BackColor = Color.FromArgb(232, 245, 233);
            mainPanel.Controls.Add(txtPackageCode);
            y += rowHeight + 8;

            // 更换/锁定按钮(仅主界面显示)
            if (!_isSettingsMode)
            {
                btnToggle = new Button
                {
                    Text = "🔓 更换包装编号",
                    Location = new Point(12, y),
                    Size = new Size(192, 36),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Microsoft YaHei", 9F),
                    Cursor = Cursors.Hand
                };
                btnToggle.FlatAppearance.BorderSize = 0;
                UpdateToggleButtonStyle();
                btnToggle.Click += BtnToggle_Click;
                mainPanel.Controls.Add(btnToggle);

                SetMainFormMode();
            }

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

        private ComboBox CreateComboBox(string[] items, int x, int y, int width)
        {
            var cbo = new ComboBox
            {
                Location = new Point(x, y),
                Width = width,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft YaHei", 9F),
                FlatStyle = FlatStyle.Flat
            };
            cbo.Items.AddRange(items);
            return cbo;
        }

        private TextBox CreateTextBox(int x, int y, int width)
        {
            var txt = new TextBox
            {
                Location = new Point(x, y),
                Width = width,
                Font = new Font("Microsoft YaHei", 9F),
                BorderStyle = BorderStyle.FixedSingle
            };
            return txt;
        }

        private void BindData()
        {
            cboPosition.SelectedItem = Material.Position ?? "L";
            cboControl.SelectedItem = Material.ControlUsage ?? "Y";
            txtCapacity.Text = Material.PackageCapacity?.ToString() ?? "";
            txtUnitUsage.Text = Material.UnitUsage?.ToString() ?? "";
            txtRemaining.Text = Material.RemainingUsage?.ToString() ?? "";
            txtPackageCode.Text = Material.PackageCode ?? "";

            UpdateControlState();
        }

        public void SaveData()
        {
            Material.Position = cboPosition.SelectedItem?.ToString() ?? "L";
            Material.ControlUsage = cboControl.SelectedItem?.ToString() ?? "Y";

            if (int.TryParse(txtCapacity.Text, out int cap))
                Material.PackageCapacity = cap;
            else
                Material.PackageCapacity = null;

            if (double.TryParse(txtUnitUsage.Text, out double unit))
                Material.UnitUsage = unit;
            else
                Material.UnitUsage = null;

            if (double.TryParse(txtRemaining.Text, out double rem))
                Material.RemainingUsage = rem;
            else
                Material.RemainingUsage = null;

            Material.PackageCode = txtPackageCode.Text.Trim();
        }

        public string GetPackageCode() => txtPackageCode.Text.Trim();

        public double GetUnitUsage()
        {
            if (double.TryParse(txtUnitUsage.Text, out double val))
                return val;
            return 1;
        }

        public double? GetRemainingUsage()
        {
            if (double.TryParse(txtRemaining.Text, out double val))
                return val;
            return null;
        }

        public void SetRemainingUsage(double value)
        {
            txtRemaining.Text = value.ToString("F2");
            Material.RemainingUsage = value;

            // 根据剩余用量更新颜色
            if (value <= 0)
                txtRemaining.BackColor = Color.FromArgb(255, 205, 210);
            else if (value < GetUnitUsage() * 5)
                txtRemaining.BackColor = Color.FromArgb(255, 243, 224);
            else
                txtRemaining.BackColor = Color.FromArgb(232, 245, 233);
        }

        public bool IsControlUsageEnabled() => cboControl.SelectedItem?.ToString() == "Y";

        private void CboControl_Changed(object sender, EventArgs e)
        {
            UpdateControlState();
        }

        private void UpdateControlState()
        {
            bool enabled = cboControl.SelectedItem?.ToString() == "Y";

            if (_isSettingsMode)
            {
                txtCapacity.ReadOnly = !enabled;
                txtUnitUsage.ReadOnly = !enabled;
                txtRemaining.ReadOnly = !enabled;
            }

            txtCapacity.BackColor = enabled ? Color.White : Color.FromArgb(245, 245, 245);
            txtUnitUsage.BackColor = enabled ? Color.White : Color.FromArgb(245, 245, 245);
            txtRemaining.BackColor = enabled ? Color.FromArgb(255, 243, 224) : Color.FromArgb(245, 245, 245);

            if (!enabled && _isSettingsMode)
            {
                txtCapacity.Text = "";
                txtUnitUsage.Text = "";
                txtRemaining.Text = "";
            }
        }

        private void BtnToggle_Click(object sender, EventArgs e)
        {
            Material.IsLocked = !Material.IsLocked;
            UpdateToggleButtonStyle();
            SetFieldsEditableState();
        }

        private void UpdateToggleButtonStyle()
        {
            if (Material.IsLocked)
            {
                btnToggle.Text = "🔓 更换包装编号";
                btnToggle.BackColor = WarningColor;
                btnToggle.ForeColor = Color.White;
            }
            else
            {
                btnToggle.Text = "🔒 锁定批次物料";
                btnToggle.BackColor = SuccessColor;
                btnToggle.ForeColor = Color.White;
            }
        }

        private void SetFieldsEditableState()
        {
            bool canEdit = !Material.IsLocked;

            // 包装编号和包装容量可编辑
            txtPackageCode.ReadOnly = !canEdit;
            txtCapacity.ReadOnly = !canEdit;

            // 单位用量和剩余用量始终只读（在主界面）
            txtUnitUsage.ReadOnly = true;
            txtRemaining.ReadOnly = true;

            // 更新背景色
            txtPackageCode.BackColor = canEdit ? Color.White : Color.FromArgb(232, 245, 233);
            txtCapacity.BackColor = canEdit ? Color.White : Color.FromArgb(245, 245, 245);
        }

        private void SetMainFormMode()
        {
            _isSettingsMode = false;
            cboPosition.Enabled = false;
            cboControl.Enabled = false;

            // 默认锁定状态
            Material.IsLocked = true;
            SetFieldsEditableState();
        }
    }
}
