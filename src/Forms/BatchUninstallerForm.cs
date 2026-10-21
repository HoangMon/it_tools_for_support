using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Ernastie.Forms;

public class BatchUninstallerForm : Form
{
    // Dynamic Design System Tokens from ThemeHelper
    private Color _bgForm => ThemeHelper.BgForm;
    private Color _cardBg => ThemeHelper.CardBg;
    private Color _borderColor => ThemeHelper.BorderColor;
    private Color _textHeading => ThemeHelper.TextHeading;
    private Color _textMuted => ThemeHelper.TextMuted;
    private Color _btnBg => ThemeHelper.BtnBg;
    private Color _btnHover => ThemeHelper.BtnHover;
    private Color _btnBorder => ThemeHelper.BtnBorder;
    private Color _warningAmber => ThemeHelper.WarningAmber;
    private Color _successGreen => ThemeHelper.SuccessGreen;
    private Color _dangerRed => ThemeHelper.DangerRed;

    private List<InstalledAppItem> _allApps = new();
    private List<InstalledAppItem> _displayApps = new();

    // Controls
    private TextBox _txtSearch = null!;
    private ComboBox _cboSort = null!;
    private CheckBox _chkShowSystem = null!;
    private DataGridView _dgvApps = null!;
    private Label _lblCounts = null!;
    private CheckBox _chkCreateRestorePoint = null!;
    private CheckBox _chkCleanLeftovers = null!;
    private Button _btnSelectAllVisible = null!;
    private Button _btnDeselectAll = null!;
    private Button _btnUninstallSelected = null!;
    private Label _lblFooterStatus = null!;
    private Button _btnReload = null!;

    public BatchUninstallerForm()
    {
        InitializeComponents();
        LoadInstalledApps();
    }

    private void InitializeComponents()
    {
        Text = "🧹 Gỡ nhiều phần mềm (Control Panel + App Settings)";
        Size = new Size(980, 710);
        MinimumSize = new Size(920, 660);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = _bgForm;
        Font = new Font("Segoe UI", 9.25f);
        Icon = SystemIcons.Application;
        DoubleBuffered = true;

        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14, 10, 14, 10),
            BackColor = _bgForm
        };

        // 1. Header Row (Title & Đọc lại danh sách)
        var pnlHeader = BuildTopHeader();
        pnlHeader.Dock = DockStyle.Top;
        pnlHeader.Height = 36;
        mainPanel.Controls.Add(pnlHeader);

        // 2. Filter & Sort Row (Tìm | Sắp xếp | Hiện cả driver)
        var pnlFilters = BuildFilterBar();
        pnlFilters.Dock = DockStyle.Top;
        pnlFilters.Height = 36;
        mainPanel.Controls.Add(pnlFilters);

        // 3. Grid Table (Fill)
        _dgvApps = BuildDataGridView();
        _dgvApps.Dock = DockStyle.Fill;
        mainPanel.Controls.Add(_dgvApps);

        // 4. Bottom Actions & Count Bar (Hiện X/Y mục | [Tích tất cả] [Bỏ tích] [Gỡ mục đã tích])
        var pnlBottomActions = BuildBottomActionsBar();
        pnlBottomActions.Dock = DockStyle.Bottom;
        pnlBottomActions.Height = 44;
        mainPanel.Controls.Add(pnlBottomActions);

        // 5. Options Row ([✓] Tạo điểm khôi phục | [✓] Gỡ xong tìm dọn file)
        var pnlOptions = BuildOptionsRow();
        pnlOptions.Dock = DockStyle.Bottom;
        pnlOptions.Height = 28;
        mainPanel.Controls.Add(pnlOptions);

        // 6. Notes & Instructions Box
        var pnlNotes = BuildNotesArea();
        pnlNotes.Dock = DockStyle.Bottom;
        pnlNotes.Height = 84;
        mainPanel.Controls.Add(pnlNotes);

        // 7. Footer Status
        var pnlFooter = BuildFooterStatus();
        pnlFooter.Dock = DockStyle.Bottom;
        pnlFooter.Height = 26;
        mainPanel.Controls.Add(pnlFooter);

        Controls.Add(mainPanel);

        // Dock ordering
        pnlHeader.BringToFront();
        pnlFilters.BringToFront();
        _dgvApps.BringToFront();
        pnlBottomActions.BringToFront();
        pnlOptions.BringToFront();
        pnlNotes.BringToFront();
        pnlFooter.BringToFront();
        ThemeHelper.ApplyTheme(this);
    }

    private Panel BuildTopHeader()
    {
        var pnl = new Panel
        {
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 4)
        };

        var lblTitle = new Label
        {
            Text = "🧹 Gỡ nhiều phần mềm cùng lúc",
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            Location = new Point(0, 2),
            AutoSize = true
        };

        _btnReload = new Button
        {
            Text = "🔄 Đọc lại danh sách",
            Size = new Size(160, 30),
            Location = new Point(pnl.Width - 160, 2),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnReload.FlatAppearance.BorderColor = _btnBorder;
        _btnReload.FlatAppearance.MouseOverBackColor = _btnHover;
        _btnReload.Click += (_, _) => LoadInstalledApps();

        pnl.Controls.Add(lblTitle);
        pnl.Controls.Add(_btnReload);

        return pnl;
    }

    private Panel BuildFilterBar()
    {
        var pnl = new Panel
        {
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 6)
        };

        var lblSearch = new Label
        {
            Text = "🔍 Tìm:",
            Location = new Point(0, 4),
            Size = new Size(52, 26),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = _textHeading
        };

        _txtSearch = new TextBox
        {
            Location = new Point(54, 4),
            Size = new Size(270, 26),
            Font = new Font("Segoe UI", 9.25f)
        };
        _txtSearch.TextChanged += (_, _) => ApplyFilterAndSort();

        var lblSort = new Label
        {
            Text = "Sắp xếp:",
            Location = new Point(340, 4),
            Size = new Size(60, 26),
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 9f),
            ForeColor = _textHeading
        };

        _cboSort = new ComboBox
        {
            Location = new Point(404, 4),
            Size = new Size(180, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9f)
        };
        _cboSort.Items.AddRange(new object[]
        {
            "Tên A → Z",
            "Tên Z → A",
            "Dung lượng giảm dần",
            "Dung lượng tăng dần",
            "Nơi cài đặt"
        });
        _cboSort.SelectedIndex = 0;
        _cboSort.SelectedIndexChanged += (_, _) => ApplyFilterAndSort();

        _chkShowSystem = new CheckBox
        {
            Text = "Hiện cả driver / runtime hệ thống",
            Location = new Point(600, 4),
            Size = new Size(260, 26),
            Font = new Font("Segoe UI", 9f),
            ForeColor = _textHeading,
            Cursor = Cursors.Hand,
            Checked = false
        };
        _chkShowSystem.CheckedChanged += (_, _) => ApplyFilterAndSort();

        pnl.Controls.Add(lblSearch);
        pnl.Controls.Add(_txtSearch);
        pnl.Controls.Add(lblSort);
        pnl.Controls.Add(_cboSort);
        pnl.Controls.Add(_chkShowSystem);

        return pnl;
    }

    private DataGridView BuildDataGridView()
    {
        var dgv = new DataGridView
        {
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            EnableHeadersVisualStyles = false,
            GridColor = _borderColor,
            Font = new Font("Segoe UI", 9.25f),
            RowTemplate = { Height = 30 }
        };

        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = _textHeading;
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        dgv.ColumnHeadersHeight = 32;

        var colCheck = new DataGridViewCheckBoxColumn { Name = "ColCheck", HeaderText = "✓", Width = 35 };
        var colName = new DataGridViewTextBoxColumn { Name = "ColName", HeaderText = "Tên phần mềm", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
        var colLoc = new DataGridViewTextBoxColumn { Name = "ColLoc", HeaderText = "Nằm ở đâu", Width = 115 };
        var colSize = new DataGridViewTextBoxColumn { Name = "ColSize", HeaderText = "Dung lượng", Width = 95, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } };
        var colPublisher = new DataGridViewTextBoxColumn { Name = "ColPublisher", HeaderText = "Nhà phát hành", Width = 210 };
        var colMethod = new DataGridViewTextBoxColumn { Name = "ColMethod", HeaderText = "Cách gỡ", Width = 110 };

        dgv.Columns.AddRange(colCheck, colName, colLoc, colSize, colPublisher, colMethod);

        dgv.CellValueChanged += (s, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 0)
            {
                var row = dgv.Rows[e.RowIndex];
                if (row.Tag is InstalledAppItem app)
                {
                    app.IsChecked = Convert.ToBoolean(row.Cells[0].Value);
                    UpdateCountsLabel();
                }
            }
        };

        dgv.CellContentClick += (s, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 0)
            {
                dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };

        return dgv;
    }

    private Panel BuildBottomActionsBar()
    {
        var pnl = new Panel
        {
            BackColor = Color.Transparent,
            Padding = new Padding(0, 6, 0, 2)
        };

        _lblCounts = new Label
        {
            Text = "Hiện 0/0 mục · Đã tích: 0",
            Location = new Point(0, 8),
            Size = new Size(320, 28),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = _textHeading,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _btnSelectAllVisible = new Button
        {
            Text = "✓ Tích tất cả đang hiện",
            Size = new Size(185, 34),
            Location = new Point(pnl.Width - 460, 5),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnSelectAllVisible.FlatAppearance.BorderColor = _btnBorder;
        _btnSelectAllVisible.FlatAppearance.MouseOverBackColor = _btnHover;
        _btnSelectAllVisible.Click += (_, _) => CheckAllVisible(true);

        _btnDeselectAll = new Button
        {
            Text = "✕ Bỏ tích hết",
            Size = new Size(120, 34),
            Location = new Point(pnl.Width - 268, 5),
            BackColor = _btnBg,
            ForeColor = _textHeading,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnDeselectAll.FlatAppearance.BorderColor = _btnBorder;
        _btnDeselectAll.FlatAppearance.MouseOverBackColor = _btnHover;
        _btnDeselectAll.Click += (_, _) => CheckAllVisible(false);

        _btnUninstallSelected = new Button
        {
            Text = "🗑️ Gỡ mục đã tích",
            Size = new Size(140, 34),
            Location = new Point(pnl.Width - 142, 5),
            BackColor = _btnBg,
            ForeColor = _dangerRed,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnUninstallSelected.FlatAppearance.BorderColor = _btnBorder;
        _btnUninstallSelected.FlatAppearance.MouseOverBackColor = _btnHover;
        _btnUninstallSelected.Click += async (_, _) => await UninstallSelectedBatchAsync();

        pnl.Controls.Add(_lblCounts);
        pnl.Controls.Add(_btnSelectAllVisible);
        pnl.Controls.Add(_btnDeselectAll);
        pnl.Controls.Add(_btnUninstallSelected);

        return pnl;
    }

    private Panel BuildOptionsRow()
    {
        var pnl = new Panel
        {
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 2)
        };

        _chkCreateRestorePoint = new CheckBox
        {
            Text = "Tạo điểm khôi phục trước khi gỡ",
            Location = new Point(0, 2),
            Size = new Size(280, 24),
            Font = new Font("Segoe UI", 9f),
            ForeColor = _textHeading,
            Checked = true,
            Cursor = Cursors.Hand
        };

        _chkCleanLeftovers = new CheckBox
        {
            Text = "Gỡ xong thì tìm & dọn file / registry còn sót",
            Location = new Point(310, 2),
            Size = new Size(340, 24),
            Font = new Font("Segoe UI", 9f),
            ForeColor = _textHeading,
            Checked = true,
            Cursor = Cursors.Hand
        };

        pnl.Controls.Add(_chkCreateRestorePoint);
        pnl.Controls.Add(_chkCleanLeftovers);

        return pnl;
    }

    private Panel BuildNotesArea()
    {
        var pnl = new Panel
        {
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 0)
        };

        var lblNotes = new Label
        {
            Text = "Mục \"Phải bấm tay\" sẽ bật cửa sổ gỡ của chính hãng — bạn bấm tiếp trong cửa sổ đó, tool chờ xong rồi mới chạy mục tiếp theo.\n" +
                   "App Store chỉ gỡ cho tài khoản đang đăng nhập. Các gói lõi Windows không cho gỡ đã tự ẩn khỏi danh sách này.\n" +
                   "Gỡ xong tool ĐỌC LẠI máy để nói đúng cái nào thật sự đã đi — không tin mã thoát của trình gỡ.\n" +
                   "Phần dọn \"còn sót\" chỉ LIỆT KÊ ra cho bạn xem và tự tích — tool không bao giờ tự xóa gì.",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 8.25f),
            ForeColor = _textHeading,
            TextAlign = ContentAlignment.TopLeft
        };

        pnl.Controls.Add(lblNotes);
        return pnl;
    }

    private Panel BuildFooterStatus()
    {
        var pnl = new Panel
        {
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 0)
        };

        _lblFooterStatus = new Label
        {
            Text = "⏳ Đang tải danh sách phần mềm...",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = _textHeading,
            TextAlign = ContentAlignment.MiddleLeft
        };

        pnl.Controls.Add(_lblFooterStatus);
        return pnl;
    }

    private async void LoadInstalledApps()
    {
        _lblFooterStatus.Text = "⏳ Đang quét phần mềm trong Control Panel và App Store...";
        _btnReload.Enabled = false;

        await Task.Run(() =>
        {
            var list = new List<InstalledAppItem>();

            // 1. Scan Control Panel (Win32 Registry)
            ScanRegistryUninstall(RegistryHive.LocalMachine, RegistryView.Registry64, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", list);
            ScanRegistryUninstall(RegistryHive.LocalMachine, RegistryView.Registry32, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", list);
            ScanRegistryUninstall(RegistryHive.CurrentUser, RegistryView.Default, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", list);

            // Deduplicate by DisplayName
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var deduplicated = new List<InstalledAppItem>();
            foreach (var item in list)
            {
                if (seen.Add(item.DisplayName))
                {
                    deduplicated.Add(item);
                }
            }

            // 2. Scan App Store (UWP Appx packages)
            ScanUwpApps(deduplicated);

            _allApps = deduplicated;
        });

        _btnReload.Enabled = true;
        ApplyFilterAndSort();
        _lblFooterStatus.Text = "⌛ Đọc xong. Tích các mục cần gỡ rồi bấm \"Gỡ mục đã tích\".";
    }

    private static void ScanRegistryUninstall(RegistryHive hive, RegistryView view, string subKeyPath, List<InstalledAppItem> list)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var uninstallKey = baseKey.OpenSubKey(subKeyPath);
            if (uninstallKey == null) return;

            foreach (var subName in uninstallKey.GetSubKeyNames())
            {
                try
                {
                    using var appKey = uninstallKey.OpenSubKey(subName);
                    if (appKey == null) continue;

                    var name = appKey.GetValue("DisplayName")?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(name)) continue;

                    var uninstall = appKey.GetValue("UninstallString")?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrEmpty(uninstall)) continue;

                    var quietUninstall = appKey.GetValue("QuietUninstallString")?.ToString()?.Trim() ?? "";
                    var publisher = appKey.GetValue("Publisher")?.ToString()?.Trim() ?? "";
                    var version = appKey.GetValue("DisplayVersion")?.ToString()?.Trim() ?? "";
                    var location = appKey.GetValue("InstallLocation")?.ToString()?.Trim() ?? "";

                    long sizeKb = 0;
                    if (appKey.GetValue("EstimatedSize") is int sizeInt) sizeKb = sizeInt;
                    else if (long.TryParse(appKey.GetValue("EstimatedSize")?.ToString(), out long sizeLong)) sizeKb = sizeLong;

                    var isSystem = false;
                    if (appKey.GetValue("SystemComponent") is int sysInt && sysInt == 1) isSystem = true;
                    if (appKey.GetValue("ParentKeyName") != null) continue; // Updates/Patches

                    // Filter out drivers/runtimes if recognized
                    if (name.Contains("Visual C++", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Security Update", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Update for", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Windows Driver Package", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Realtek High Definition Audio", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Intel(R) Chipset", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("NVIDIA Graphics Driver", StringComparison.OrdinalIgnoreCase))
                    {
                        isSystem = true;
                    }

                    list.Add(new InstalledAppItem
                    {
                        DisplayName = name,
                        LocationType = "Control Panel",
                        EstimatedSizeKb = sizeKb,
                        Publisher = publisher,
                        DisplayVersion = version,
                        UninstallString = uninstall,
                        QuietUninstallString = quietUninstall,
                        InstallLocation = location,
                        IsSystemOrDriver = isSystem
                    });
                }
                catch { }
            }
        }
        catch { }
    }

    private static void ScanUwpApps(List<InstalledAppItem> list)
    {
        try
        {
            var psi = new ProcessStartInfo("powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -Command \"Get-AppxPackage | Where-Object { -not $_.IsFramework -and -not $_.NonRemovable } | ForEach-Object { $_.Name + ':::' + $_.PackageFullName + ':::' + $_.Publisher }\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            using var proc = Process.Start(psi)!;
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            using var reader = new StringReader(output);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split(new[] { ":::" }, StringSplitOptions.None);
                if (parts.Length >= 2)
                {
                    var name = parts[0].Trim();
                    var pkgFull = parts[1].Trim();
                    var pub = parts.Length > 2 ? parts[2].Trim() : "";

                    // Filter core windows noise
                    if (name.StartsWith("Microsoft.NET", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("Microsoft.VCLibs", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("Microsoft.UI.Xaml", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    list.Add(new InstalledAppItem
                    {
                        DisplayName = name,
                        LocationType = "App Store",
                        Publisher = pub,
                        PackageFullName = pkgFull,
                        IsSystemOrDriver = false
                    });
                }
            }
        }
        catch { }
    }

    private void ApplyFilterAndSort()
    {
        var search = _txtSearch.Text.Trim();
        bool showSystem = _chkShowSystem.Checked;

        var query = _allApps.AsEnumerable();

        if (!showSystem)
        {
            query = query.Where(a => !a.IsSystemOrDriver);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(a => a.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                     a.Publisher.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        // Sorting
        query = _cboSort.SelectedIndex switch
        {
            1 => query.OrderByDescending(a => a.DisplayName),
            2 => query.OrderByDescending(a => a.EstimatedSizeKb),
            3 => query.OrderBy(a => a.EstimatedSizeKb),
            4 => query.OrderBy(a => a.LocationType).ThenBy(a => a.DisplayName),
            _ => query.OrderBy(a => a.DisplayName)
        };

        _displayApps = query.ToList();

        // Populate table
        _dgvApps.Rows.Clear();
        foreach (var app in _displayApps)
        {
            int rIdx = _dgvApps.Rows.Add(app.IsChecked, app.DisplayName, app.LocationType, app.SizeDisplay, app.Publisher, app.UninstallMethod);
            _dgvApps.Rows[rIdx].Tag = app;

            if (app.UninstallMethod == "Phải bấm tay")
            {
                _dgvApps.Rows[rIdx].Cells["ColMethod"].Style.ForeColor = _warningAmber;
            }
            else
            {
                _dgvApps.Rows[rIdx].Cells["ColMethod"].Style.ForeColor = _successGreen;
            }
        }

        UpdateCountsLabel();
    }

    private void UpdateCountsLabel()
    {
        int checkedCount = _allApps.Count(a => a.IsChecked);
        _lblCounts.Text = $"Hiện {_displayApps.Count}/{_allApps.Count} mục  ·  Đã tích: {checkedCount}";
    }

    private void CheckAllVisible(bool check)
    {
        foreach (DataGridViewRow row in _dgvApps.Rows)
        {
            row.Cells[0].Value = check;
            if (row.Tag is InstalledAppItem app)
            {
                app.IsChecked = check;
            }
        }
        UpdateCountsLabel();
    }

    private async Task UninstallSelectedBatchAsync()
    {
        var selectedApps = _allApps.Where(a => a.IsChecked).ToList();
        if (selectedApps.Count == 0)
        {
            MessageBox.Show("Vui lòng tích chọn ít nhất một phần mềm để gỡ bỏ!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var sbList = new StringBuilder();
        foreach (var app in selectedApps.Take(15))
        {
            sbList.AppendLine($"• {app.DisplayName} ({app.LocationType})");
        }
        if (selectedApps.Count > 15)
        {
            sbList.AppendLine($"... và {selectedApps.Count - 15} phần mềm khác");
        }

        var confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn gỡ bỏ {selectedApps.Count} phần mềm đã chọn sau đây không?\n\n{sbList}\n" +
            "Hành động này sẽ xóa các ứng dụng khỏi hệ thống.",
            "Xác Nhận Gỡ Bỏ Phần Mềm",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        _btnUninstallSelected.Enabled = false;
        _btnReload.Enabled = false;

        // 1. Create System Restore Point if requested
        if (_chkCreateRestorePoint.Checked)
        {
            _lblFooterStatus.Text = "⏳ Đang tạo điểm khôi phục hệ thống (System Restore Point)...";
            await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo("powershell.exe",
                        "-NoProfile -ExecutionPolicy Bypass -Command \"Checkpoint-Computer -Description 'Truoc_khi_go_phan_mem' -RestorePointType APPLICATION_UNINSTALL\"")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var p = Process.Start(psi)!;
                    p.WaitForExit(15000);
                }
                catch { }
            });
        }

        // 2. Loop Uninstall
        int current = 0;
        int total = selectedApps.Count;
        var uninstalledNames = new List<string>();

        foreach (var app in selectedApps)
        {
            current++;
            _lblFooterStatus.Text = $"⏳ Đang gỡ ({current}/{total}): {app.DisplayName}...";

            await Task.Run(() =>
            {
                try
                {
                    if (app.LocationType == "App Store")
                    {
                        // Remove UWP AppX
                        var psi = new ProcessStartInfo("powershell.exe",
                            $"-NoProfile -ExecutionPolicy Bypass -Command \"Remove-AppxPackage -Package '{app.PackageFullName}'\"")
                        {
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using var p = Process.Start(psi)!;
                        p.WaitForExit();
                        uninstalledNames.Add(app.DisplayName);
                    }
                    else
                    {
                        // Win32 App
                        if (!string.IsNullOrEmpty(app.QuietUninstallString))
                        {
                            RunUninstallCmd(app.QuietUninstallString, true);
                        }
                        else if (app.UninstallString.Contains("MsiExec", StringComparison.OrdinalIgnoreCase))
                        {
                            // Make MSI silent: change /I to /X and append /qn /norestart
                            var msiCmd = app.UninstallString.Replace("/I", "/X", StringComparison.OrdinalIgnoreCase);
                            if (!msiCmd.Contains("/qn", StringComparison.OrdinalIgnoreCase))
                            {
                                msiCmd += " /qn /norestart";
                            }
                            RunUninstallCmd(msiCmd, true);
                        }
                        else if (app.UninstallString.Contains("unins000.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            RunUninstallCmd($"\"{app.UninstallString}\" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART", true);
                        }
                        else
                        {
                            // Interactive mode
                            Invoke(() =>
                            {
                                MessageBox.Show(
                                    $"Chuẩn bị mở cửa sổ gỡ của phần mềm:\n\"{app.DisplayName}\"\n\n" +
                                    "Vui lòng bấm tiếp trong cửa sổ gỡ của hãng. Tool sẽ tự động chờ xong rồi mới chạy tiếp mục tiếp theo.",
                                    "Gỡ Bấm Tay",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                            });
                            RunUninstallCmd(app.UninstallString, false);
                        }
                        uninstalledNames.Add(app.DisplayName);
                    }
                }
                catch { }
            });
        }

        // 3. Optional Leftover Clean dialog
        if (_chkCleanLeftovers.Checked && uninstalledNames.Count > 0)
        {
            _lblFooterStatus.Text = "⏳ Đang quét các tệp & thư mục còn sót của phần mềm vừa gỡ...";
            var leftovers = FindLeftovers(uninstalledNames);
            if (leftovers.Count > 0)
            {
                using var dlg = new LeftoverCleanDialog(leftovers);
                dlg.ShowDialog(this);
            }
        }

        // 4. Reload installed apps to verify what was truly removed
        _lblFooterStatus.Text = "⏳ Đang đọc lại danh sách máy tính để đối soát kết quả...";
        LoadInstalledApps();

        _btnUninstallSelected.Enabled = true;
        _btnReload.Enabled = true;

        MessageBox.Show($"Đã hoàn tất tiến trình gỡ bỏ {uninstalledNames.Count} phần mềm!\nDanh sách đã được làm mới để đối chiếu.", "Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void RunUninstallCmd(string command, bool silent)
    {
        try
        {
            string fileName;
            string args = "";

            command = command.Trim();
            if (command.StartsWith("\""))
            {
                int quoteEnd = command.IndexOf('\"', 1);
                if (quoteEnd > 0)
                {
                    fileName = command.Substring(1, quoteEnd - 1);
                    args = command.Substring(quoteEnd + 1).Trim();
                }
                else
                {
                    fileName = command;
                }
            }
            else
            {
                int spaceIdx = command.IndexOf(' ');
                if (spaceIdx > 0)
                {
                    fileName = command.Substring(0, spaceIdx);
                    args = command.Substring(spaceIdx + 1).Trim();
                }
                else
                {
                    fileName = command;
                }
            }

            var psi = new ProcessStartInfo(fileName, args)
            {
                CreateNoWindow = silent,
                UseShellExecute = !silent
            };
            using var p = Process.Start(psi)!;
            p.WaitForExit();
        }
        catch { }
    }

    private static List<string> FindLeftovers(List<string> appNames)
    {
        var foundPaths = new List<string>();
        var searchRoots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        };

        foreach (var name in appNames)
        {
            var clean = name.Split(' ')[0].Trim();
            if (clean.Length < 3) continue;

            foreach (var root in searchRoots)
            {
                if (!Directory.Exists(root)) continue;
                try
                {
                    foreach (var dir in Directory.GetDirectories(root))
                    {
                        var dirName = Path.GetFileName(dir);
                        if (dirName.Equals(clean, StringComparison.OrdinalIgnoreCase) && !foundPaths.Contains(dir))
                        {
                            foundPaths.Add(dir);
                        }
                    }
                }
                catch { }
            }
        }

        return foundPaths;
    }
}

public class InstalledAppItem
{
    public string DisplayName { get; set; } = string.Empty;
    public string LocationType { get; set; } = "Control Panel";
    public long EstimatedSizeKb { get; set; } = 0;
    public string Publisher { get; set; } = string.Empty;
    public string DisplayVersion { get; set; } = string.Empty;
    public string UninstallString { get; set; } = string.Empty;
    public string QuietUninstallString { get; set; } = string.Empty;
    public string InstallLocation { get; set; } = string.Empty;
    public string PackageFullName { get; set; } = string.Empty;
    public bool IsSystemOrDriver { get; set; } = false;
    public bool IsChecked { get; set; } = false;

    public string SizeDisplay
    {
        get
        {
            if (EstimatedSizeKb <= 0) return "—";
            if (EstimatedSizeKb >= 1024 * 1024)
                return $"{(EstimatedSizeKb / (1024.0 * 1024.0)):F1} GB";
            return $"{(EstimatedSizeKb / 1024.0):F0} MB";
        }
    }

    public string UninstallMethod
    {
        get
        {
            if (LocationType == "App Store") return "Ngầm";
            if (!string.IsNullOrEmpty(QuietUninstallString)) return "Ngầm";
            if (UninstallString.Contains("MsiExec", StringComparison.OrdinalIgnoreCase)) return "Ngầm";
            if (UninstallString.Contains("unins000.exe", StringComparison.OrdinalIgnoreCase)) return "Ngầm";
            return "Phải bấm tay";
        }
    }
}

public class LeftoverCleanDialog : Form
{
    private readonly List<string> _paths;
    private CheckedListBox _clb = null!;

    public LeftoverCleanDialog(List<string> paths)
    {
        _paths = paths;
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        Text = "🧹 Dọn dẹp thư mục còn sót sau khi gỡ phần mềm";
        Size = new Size(680, 420);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = ThemeHelper.BgForm;
        Font = new Font("Segoe UI", 9f);
        Icon = SystemIcons.Application;

        var lblTitle = new Label
        {
            Text = "Phát hiện các thư mục còn sót của phần mềm vừa gỡ.\nTích chọn những thư mục bạn muốn xóa bỏ hoàn toàn:",
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(12, 10, 12, 0),
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold)
        };

        _clb = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            CheckOnClick = true,
            Font = new Font("Consolas", 9f)
        };
        foreach (var p in _paths)
        {
            _clb.Items.Add(p, false); // User manually ticks according to rule
        }

        var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(12, 8, 12, 8), BackColor = Color.Transparent };
        var btnDelete = new Button
        {
            Text = "🗑️ Xóa các mục đã tích",
            Dock = DockStyle.Right,
            Width = 180,
            BackColor = Color.FromArgb(185, 28, 28),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnDelete.Click += (_, _) =>
        {
            int deleted = 0;
            foreach (var item in _clb.CheckedItems)
            {
                var dir = item.ToString();
                if (Directory.Exists(dir))
                {
                    try { Directory.Delete(dir, true); deleted++; } catch { }
                }
            }
            MessageBox.Show($"Đã xóa {deleted} thư mục còn sót!", "Dọn Xong", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        };

        var btnSkip = new Button
        {
            Text = "Bỏ qua",
            Dock = DockStyle.Right,
            Width = 90,
            BackColor = Color.FromArgb(235, 225, 205),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 10, 0)
        };
        btnSkip.Click += (_, _) => Close();

        bottomBar.Controls.Add(btnDelete);
        bottomBar.Controls.Add(btnSkip);

        Controls.Add(_clb);
        Controls.Add(lblTitle);
        Controls.Add(bottomBar);
        ThemeHelper.ApplyTheme(this);
    }
}
