using System;
using System.Drawing;
using System.Windows.Forms;
using MESUploadSystem.Models;

namespace MESUploadSystem.Forms
{
    public partial class PlcSettingsForm : Form
    {
        private PlcSignalConfig _config;
        private DataGridView dgvSignals;
        private NumericUpDown numTimeout;  // 超时时间输入框

        private readonly Color PrimaryColor = Color.FromArgb(66, 133, 244);
        private readonly Color SuccessColor = Color.FromArgb(52, 168, 83);

        public PlcSettingsForm(PlcSignalConfig config)
        {
            _config = config ?? PlcSignalConfig.GetDefault();
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "PLC通讯配置";
            this.Size = new Size(650, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Microsoft YaHei", 9F);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);

            // 标题
            var lblTitle = new Label
            {
                Text = "📡 PLC信号配置",
                Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(32, 33, 36),
                Location = new Point(25, 20),
                AutoSize = true
            };
            this.Controls.Add(lblTitle);

            // 说明
            var lblTip = new Label
            {
                Text = "配置不同执行结果对应的PLC信号地址和值",
                Font = new Font("Microsoft YaHei", 9F),
                ForeColor = Color.FromArgb(128, 128, 128),
                Location = new Point(25, 50),
                AutoSize = true
            };
            this.Controls.Add(lblTip);

            // 值说明标签
            var lblValueInfo = new Label
            {
                Text = "💡 提示：ON = 发送01，OFF = 发送00",
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(66, 133, 244),
                Location = new Point(25, 72),
                AutoSize = true
            };
            this.Controls.Add(lblValueInfo);

            // ========== 超时时间配置区域 ==========
            var pnlTimeout = new Panel
            {
                Location = new Point(25, 100),
                Size = new Size(585, 45),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblTimeout = new Label
            {
                Text = "⏱ 响应超时时间：",
                Font = new Font("Microsoft YaHei", 9F),
                ForeColor = Color.FromArgb(32, 33, 36),
                Location = new Point(15, 12),
                AutoSize = true
            };
            pnlTimeout.Controls.Add(lblTimeout);

            numTimeout = new NumericUpDown
            {
                Location = new Point(130, 9),
                Size = new Size(80, 25),
                Minimum = 100,
                Maximum = 10000,
                Increment = 100,
                Value = 1000,
                Font = new Font("Microsoft YaHei", 9F)
            };
            pnlTimeout.Controls.Add(numTimeout);

            var lblMs = new Label
            {
                Text = "毫秒 (ms)",
                Font = new Font("Microsoft YaHei", 9F),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(215, 12),
                AutoSize = true
            };
            pnlTimeout.Controls.Add(lblMs);

            var lblTimeoutTip = new Label
            {
                Text = "（发送信号后等待PLC响应的时间，超时仅记录日志不影响程序运行）",
                Font = new Font("Microsoft YaHei", 8F),
                ForeColor = Color.FromArgb(150, 150, 150),
                Location = new Point(290, 13),
                AutoSize = true
            };
            pnlTimeout.Controls.Add(lblTimeoutTip);

            this.Controls.Add(pnlTimeout);

            // ========== DataGridView ==========
            dgvSignals = new DataGridView
            {
                Location = new Point(25, 155),
                Size = new Size(585, 230),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Microsoft YaHei", 9F)
            };

            // 列定义
            dgvSignals.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Trigger",
                HeaderText = "执行时机",
                ReadOnly = true,
                FillWeight = 35
            });

            dgvSignals.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Address",
                HeaderText = "PLC地址",
                FillWeight = 25
            });

            var colValue = new DataGridViewComboBoxColumn
            {
                Name = "Value",
                HeaderText = "值",
                FillWeight = 25
            };
            colValue.Items.AddRange("ON (01)", "OFF (00)");
            dgvSignals.Columns.Add(colValue);

            dgvSignals.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SendData",
                HeaderText = "发送数据",
                ReadOnly = true,
                FillWeight = 15
            });

            // 样式
            dgvSignals.EnableHeadersVisualStyles = false;
            dgvSignals.ColumnHeadersDefaultCellStyle.BackColor = PrimaryColor;
            dgvSignals.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvSignals.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold);
            dgvSignals.ColumnHeadersHeight = 35;
            dgvSignals.RowTemplate.Height = 32;

            dgvSignals.CellValueChanged += DgvSignals_CellValueChanged;
            dgvSignals.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgvSignals.IsCurrentCellDirty)
                    dgvSignals.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            this.Controls.Add(dgvSignals);

            // ========== 按钮 ==========
            var btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(350, 405),
                Size = new Size(100, 36),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(btnCancel);

            var btnSave = new Button
            {
                Text = "💾 保存",
                Location = new Point(470, 405),
                Size = new Size(100, 36),
                BackColor = SuccessColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);
        }

        private void DgvSignals_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvSignals.Columns[e.ColumnIndex].Name == "Value")
            {
                UpdateSendDataColumn(e.RowIndex);
            }
        }

        private void UpdateSendDataColumn(int rowIndex)
        {
            var valueCell = dgvSignals.Rows[rowIndex].Cells["Value"].Value?.ToString() ?? "";
            var sendDataCell = dgvSignals.Rows[rowIndex].Cells["SendData"];

            if (valueCell.Contains("ON"))
            {
                sendDataCell.Value = "01";
                sendDataCell.Style.ForeColor = Color.Green;
                sendDataCell.Style.Font = new Font("Consolas", 9F, FontStyle.Bold);
            }
            else
            {
                sendDataCell.Value = "00";
                sendDataCell.Style.ForeColor = Color.Red;
                sendDataCell.Style.Font = new Font("Consolas", 9F, FontStyle.Bold);
            }
        }

        private void LoadData()
        {
            // 加载超时时间
            numTimeout.Value = Math.Max(numTimeout.Minimum,
                               Math.Min(numTimeout.Maximum, _config.ResponseTimeout));

            // 加载信号列表
            dgvSignals.Rows.Clear();
            foreach (var signal in _config.Signals)
            {
                int rowIndex = dgvSignals.Rows.Add();
                dgvSignals.Rows[rowIndex].Cells["Trigger"].Value = signal.Trigger;
                dgvSignals.Rows[rowIndex].Cells["Address"].Value = signal.Address;
                dgvSignals.Rows[rowIndex].Cells["Value"].Value = signal.Value ? "ON (01)" : "OFF (00)";
                UpdateSendDataColumn(rowIndex);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // 保存超时时间
            _config.ResponseTimeout = (int)numTimeout.Value;

            // 保存信号列表
            _config.Signals.Clear();
            foreach (DataGridViewRow row in dgvSignals.Rows)
            {
                var valueStr = row.Cells["Value"].Value?.ToString() ?? "";

                var signal = new PlcSignalItem
                {
                    Trigger = row.Cells["Trigger"].Value?.ToString(),
                    Address = row.Cells["Address"].Value?.ToString(),
                    Value = valueStr.Contains("ON")
                };
                _config.Signals.Add(signal);
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
