using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace CodexMaintenanceGui
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly Color pageBackground = Color.FromArgb(244, 247, 251);
        private readonly Color cardBackground = Color.White;
        private readonly Color primary = Color.FromArgb(37, 99, 235);
        private readonly Color primaryDark = Color.FromArgb(30, 64, 175);
        private readonly Color danger = Color.FromArgb(220, 38, 38);
        private readonly Color textColor = Color.FromArgb(31, 41, 55);
        private readonly Color mutedColor = Color.FromArgb(107, 114, 128);

        private readonly TextBox codexHomeText = new TextBox();
        private readonly TextBox backupRootText = new TextBox();
        private readonly NumericUpDown keepBackupsNumber = new NumericUpDown();
        private readonly NumericUpDown vacuumNumber = new NumericUpDown();
        private readonly RichTextBox logBox = new RichTextBox();
        private readonly Label statusLabel = new Label();
        private readonly Button dryRunButton = new Button();
        private readonly Button cleanupButton = new Button();
        private readonly Button saveButton = new Button();
        private readonly Button openBackupsButton = new Button();
        private readonly Button openCodexButton = new Button();

        private readonly string executableDirectory;
        private readonly string configDirectory;
        private readonly string configPath;

        public MainForm()
        {
            executableDirectory = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            configDirectory = ResolveConfigDirectory(executableDirectory);
            configPath = Path.Combine(configDirectory, "CodexMaintenance.config");

            Text = "Codex Maintenance";
            Width = 1040;
            Height = 760;
            MinimumSize = new Size(920, 660);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = pageBackground;
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            AutoScaleMode = AutoScaleMode.Dpi;
            try
            {
                var appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (appIcon != null)
                {
                    Icon = appIcon;
                }
            }
            catch
            {
            }

            BuildLayout();
            LoadSettings();
            RefreshStatus();
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.Padding = new Padding(18);
            root.BackColor = pageBackground;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 268));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            Controls.Add(root);

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildConfigCard(), 0, 1);
            root.Controls.Add(BuildLogCard(), 0, 2);
            root.Controls.Add(BuildFooter(), 0, 3);
        }

        private Control BuildHeader()
        {
            var header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = primaryDark;
            header.Padding = new Padding(22, 14, 22, 14);
            header.Margin = new Padding(0, 0, 0, 14);

            var title = new Label();
            title.Text = "Codex Maintenance";
            title.ForeColor = Color.White;
            title.Font = new Font("Segoe UI", 22F, FontStyle.Bold, GraphicsUnit.Point);
            title.AutoSize = true;
            title.Location = new Point(22, 14);
            header.Controls.Add(title);

            var subtitle = new Label();
            subtitle.Text = "安全备份并清理 Codex / Codex++ 本地日志，让启动更轻快。";
            subtitle.ForeColor = Color.FromArgb(219, 234, 254);
            subtitle.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
            subtitle.AutoSize = true;
            subtitle.Location = new Point(25, 58);
            header.Controls.Add(subtitle);

            return header;
        }

        private Control BuildConfigCard()
        {
            var card = CreateCard();
            card.RowCount = 6;
            card.ColumnCount = 4;
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 122));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 122));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var sectionTitle = new Label();
            sectionTitle.Text = "本地配置";
            sectionTitle.ForeColor = textColor;
            sectionTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold, GraphicsUnit.Point);
            sectionTitle.Dock = DockStyle.Fill;
            sectionTitle.TextAlign = ContentAlignment.MiddleLeft;
            card.Controls.Add(sectionTitle, 0, 0);
            card.SetColumnSpan(sectionTitle, 4);

            AddLabel(card, "Codex 目录", 0, 1);
            SetupTextBox(codexHomeText);
            card.Controls.Add(codexHomeText, 1, 1);
            var chooseCodexButton = CreateSecondaryButton("选择...");
            chooseCodexButton.Click += delegate { ChooseFolder(codexHomeText, true); };
            card.Controls.Add(chooseCodexButton, 2, 1);
            openCodexButton.Text = "打开目录";
            StyleSecondaryButton(openCodexButton);
            openCodexButton.Click += delegate { OpenFolder(codexHomeText.Text); };
            card.Controls.Add(openCodexButton, 3, 1);

            AddLabel(card, "备份目录", 0, 2);
            SetupTextBox(backupRootText);
            card.Controls.Add(backupRootText, 1, 2);
            var chooseBackupButton = CreateSecondaryButton("选择...");
            chooseBackupButton.Click += delegate { ChooseFolder(backupRootText, false); };
            card.Controls.Add(chooseBackupButton, 2, 2);
            openBackupsButton.Text = "打开备份";
            StyleSecondaryButton(openBackupsButton);
            openBackupsButton.Click += delegate { OpenBackups(); };
            card.Controls.Add(openBackupsButton, 3, 2);

            AddLabel(card, "保留备份", 0, 3);
            keepBackupsNumber.Minimum = 1;
            keepBackupsNumber.Maximum = 100;
            keepBackupsNumber.Value = 5;
            keepBackupsNumber.Dock = DockStyle.Left;
            keepBackupsNumber.Width = 120;
            card.Controls.Add(keepBackupsNumber, 1, 3);

            AddLabel(card, "VACUUM 阈值", 2, 3);
            vacuumNumber.DecimalPlaces = 1;
            vacuumNumber.Minimum = 1;
            vacuumNumber.Maximum = 4096;
            vacuumNumber.Value = 20;
            vacuumNumber.Increment = 1;
            vacuumNumber.Dock = DockStyle.Left;
            vacuumNumber.Width = 110;
            card.Controls.Add(vacuumNumber, 3, 3);

            var buttonPanel = new FlowLayoutPanel();
            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.FlowDirection = FlowDirection.LeftToRight;
            buttonPanel.Padding = new Padding(0, 8, 0, 0);
            buttonPanel.WrapContents = false;
            buttonPanel.BackColor = cardBackground;

            saveButton.Text = "保存配置";
            StyleSecondaryButton(saveButton);
            saveButton.Click += delegate { SaveSettingsWithMessage(); };
            buttonPanel.Controls.Add(saveButton);

            dryRunButton.Text = "预览清理";
            StylePrimaryButton(dryRunButton);
            dryRunButton.Click += delegate { RunMaintenance(true); };
            buttonPanel.Controls.Add(dryRunButton);

            cleanupButton.Text = "正式清理";
            StyleDangerButton(cleanupButton);
            cleanupButton.Click += delegate { RunMaintenance(false); };
            buttonPanel.Controls.Add(cleanupButton);

            card.Controls.Add(buttonPanel, 0, 4);
            card.SetColumnSpan(buttonPanel, 4);

            return card;
        }

        private Control BuildLogCard()
        {
            var card = CreateCard();
            card.RowCount = 2;
            card.ColumnCount = 1;
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var title = new Label();
            title.Text = "运行日志";
            title.ForeColor = textColor;
            title.Font = new Font("Segoe UI", 13F, FontStyle.Bold, GraphicsUnit.Point);
            title.Dock = DockStyle.Fill;
            title.TextAlign = ContentAlignment.MiddleLeft;
            card.Controls.Add(title, 0, 0);

            logBox.Dock = DockStyle.Fill;
            logBox.BorderStyle = BorderStyle.None;
            logBox.BackColor = Color.FromArgb(17, 24, 39);
            logBox.ForeColor = Color.FromArgb(229, 231, 235);
            logBox.Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point);
            logBox.ReadOnly = true;
            logBox.WordWrap = false;
            logBox.Text = "准备就绪。建议先点击“预览清理”，确认路径正确后再正式清理。" + Environment.NewLine;
            card.Controls.Add(logBox, 0, 1);

            return card;
        }

        private Control BuildFooter()
        {
            statusLabel.Dock = DockStyle.Fill;
            statusLabel.ForeColor = mutedColor;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            return statusLabel;
        }

        private TableLayoutPanel CreateCard()
        {
            var card = new TableLayoutPanel();
            card.Dock = DockStyle.Fill;
            card.BackColor = cardBackground;
            card.Padding = new Padding(18);
            card.Margin = new Padding(0, 0, 0, 14);
            return card;
        }

        private void AddLabel(TableLayoutPanel card, string text, int column, int row)
        {
            var label = new Label();
            label.Text = text;
            label.ForeColor = mutedColor;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            card.Controls.Add(label, column, row);
        }

        private void SetupTextBox(TextBox textBox)
        {
            textBox.Dock = DockStyle.Fill;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        }

        private Button CreateSecondaryButton(string text)
        {
            var button = new Button();
            button.Text = text;
            StyleSecondaryButton(button);
            return button;
        }

        private void StylePrimaryButton(Button button)
        {
            StyleButton(button, primary, Color.White);
        }

        private void StyleDangerButton(Button button)
        {
            StyleButton(button, danger, Color.White);
        }

        private void StyleSecondaryButton(Button button)
        {
            StyleButton(button, Color.FromArgb(229, 231, 235), textColor);
        }

        private void StyleButton(Button button, Color backColor, Color foreColor)
        {
            button.Width = 116;
            button.Height = 34;
            button.Margin = new Padding(0, 0, 10, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = backColor;
            button.ForeColor = foreColor;
            button.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point);
        }

        private void LoadSettings()
        {
            var values = ReadConfig(configPath);
            var defaultCodexHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex");
            var defaultBackupRoot = Path.Combine(configDirectory, "backups");

            codexHomeText.Text = GetValue(values, "CodexHome", defaultCodexHome);
            backupRootText.Text = GetValue(values, "BackupRoot", defaultBackupRoot);
            keepBackupsNumber.Value = SafeDecimal(GetValue(values, "KeepBackups", "5"), 5, 1, 100);
            vacuumNumber.Value = SafeDecimal(GetValue(values, "VacuumThresholdMb", "20.0"), 20, 1, 4096);
        }

        private Dictionary<string, string> ReadConfig(string path)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(path))
            {
                return values;
            }

            foreach (var rawLine in File.ReadAllLines(path, Encoding.UTF8))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                var separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                var key = line.Substring(0, separator).Trim();
                var value = line.Substring(separator + 1).Trim().Trim('"');
                values[key] = ExpandPath(value);
            }
            return values;
        }

        private string GetValue(Dictionary<string, string> values, string key, string fallback)
        {
            string value;
            return values.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
        }

        private decimal SafeDecimal(string value, decimal fallback, decimal minimum, decimal maximum)
        {
            decimal parsed;
            if (!decimal.TryParse(value, out parsed))
            {
                parsed = fallback;
            }
            if (parsed < minimum)
            {
                parsed = minimum;
            }
            if (parsed > maximum)
            {
                parsed = maximum;
            }
            return parsed;
        }

        private bool SaveSettings(bool quiet)
        {
            try
            {
                var codexHome = ExpandPath(codexHomeText.Text.Trim());
                var backupRoot = ExpandPath(backupRootText.Text.Trim());

                if (string.IsNullOrWhiteSpace(codexHome))
                {
                    MessageBox.Show("请先填写 Codex 目录。", "保存配置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                if (!Directory.Exists(codexHome))
                {
                    MessageBox.Show("Codex 目录不存在：" + codexHome, "保存配置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                if (string.IsNullOrWhiteSpace(backupRoot))
                {
                    MessageBox.Show("请先填写备份目录。", "保存配置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                Directory.CreateDirectory(backupRoot);
                Directory.CreateDirectory(configDirectory);

                var lines = new[]
                {
                    "# Codex Maintenance local configuration",
                    "# This file is machine-local and should not be committed.",
                    "CodexHome=" + codexHome,
                    "BackupRoot=" + backupRoot,
                    "KeepBackups=" + ((int)keepBackupsNumber.Value).ToString(),
                    "VacuumThresholdMb=" + vacuumNumber.Value.ToString("0.0")
                };
                File.WriteAllLines(configPath, lines, Encoding.UTF8);

                codexHomeText.Text = codexHome;
                backupRootText.Text = backupRoot;
                RefreshStatus();
                AppendLog("配置已保存：" + configPath);

                if (!quiet)
                {
                    MessageBox.Show("配置已保存。", "Codex Maintenance", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "保存配置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void SaveSettingsWithMessage()
        {
            SaveSettings(false);
        }

        private void RunMaintenance(bool dryRun)
        {
            if (!SaveSettings(true))
            {
                return;
            }

            var toolPath = ResolveConsoleToolPath();
            if (!File.Exists(toolPath))
            {
                MessageBox.Show("找不到命令行维护工具：" + toolPath, "运行失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!dryRun)
            {
                var confirm = MessageBox.Show(
                    "即将正式清理 Codex 日志。\r\n\r\n清理前会创建备份，但仍建议先关闭 Codex / Codex++。\r\n\r\n确认继续？",
                    "确认正式清理",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes)
                {
                    AppendLog("已取消正式清理。");
                    return;
                }
            }

            SetBusy(true);
            logBox.Clear();
            AppendLog(dryRun ? "开始预览清理..." : "开始正式清理...");

            ThreadPool.QueueUserWorkItem(delegate
            {
                var output = RunProcess(toolPath, dryRun ? "--dry-run --no-pause" : "--no-pause");
                BeginInvoke(new Action(delegate
                {
                    AppendLog(output.Output.Trim());
                    if (output.ExitCode == 0)
                    {
                        AppendLog(dryRun ? "预览完成。" : "清理完成。");
                    }
                    else
                    {
                        AppendLog("运行失败，退出码：" + output.ExitCode);
                    }
                    SetBusy(false);
                    RefreshStatus();
                }));
            });
        }

        private ProcessResult RunProcess(string fileName, string arguments)
        {
            var result = new ProcessResult();
            try
            {
                var processInfo = new ProcessStartInfo();
                processInfo.FileName = fileName;
                processInfo.Arguments = arguments;
                processInfo.WorkingDirectory = Path.GetDirectoryName(fileName) ?? executableDirectory;
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardOutput = true;
                processInfo.RedirectStandardError = true;
                processInfo.CreateNoWindow = true;

                using (var process = Process.Start(processInfo))
                {
                    if (process == null)
                    {
                        result.ExitCode = 1;
                        result.Output = "启动进程失败。";
                        return result;
                    }
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    result.ExitCode = process.ExitCode;
                    result.Output = output + error;
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.ExitCode = 1;
                result.Output = ex.Message;
                return result;
            }
        }

        private void SetBusy(bool busy)
        {
            dryRunButton.Enabled = !busy;
            cleanupButton.Enabled = !busy;
            saveButton.Enabled = !busy;
            openBackupsButton.Enabled = !busy;
            openCodexButton.Enabled = !busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
            statusLabel.Text = busy ? "正在运行，请稍候..." : BuildStatusText();
        }

        private void RefreshStatus()
        {
            statusLabel.Text = BuildStatusText();
        }

        private string BuildStatusText()
        {
            var codexHome = ExpandPath(codexHomeText.Text.Trim());
            var backupRoot = ExpandPath(backupRootText.Text.Trim());
            var dbPath = Path.Combine(codexHome, "logs_2.sqlite");
            var dbInfo = File.Exists(dbPath) ? (new FileInfo(dbPath).Length / 1024.0 / 1024.0).ToString("0.0") + " MB" : "未找到";
            var configInfo = File.Exists(configPath) ? "已保存" : "未保存";
            return "配置：" + configInfo + "    日志数据库：" + dbInfo + "    备份目录：" + backupRoot;
        }

        private void AppendLog(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }
            logBox.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + text + Environment.NewLine);
            logBox.SelectionStart = logBox.TextLength;
            logBox.ScrollToCaret();
        }

        private void ChooseFolder(TextBox targetTextBox, bool mustExist)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = mustExist ? "选择 Codex 的 .codex 目录" : "选择备份保存目录";
                dialog.ShowNewFolderButton = !mustExist;
                if (Directory.Exists(targetTextBox.Text))
                {
                    dialog.SelectedPath = targetTextBox.Text;
                }
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    targetTextBox.Text = dialog.SelectedPath;
                    RefreshStatus();
                }
            }
        }

        private void OpenBackups()
        {
            if (!SaveSettings(true))
            {
                return;
            }
            OpenFolder(backupRootText.Text);
        }

        private void OpenFolder(string folder)
        {
            try
            {
                var expanded = ExpandPath(folder);
                if (!Directory.Exists(expanded))
                {
                    MessageBox.Show("目录不存在：" + expanded, "打开目录", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Process.Start(expanded);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "打开目录失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string ResolveConsoleToolPath()
        {
            var local = Path.Combine(executableDirectory, "CodexMaintenance.exe");
            if (File.Exists(local))
            {
                return local;
            }
            return Path.Combine(configDirectory, "CodexMaintenance.exe");
        }

        private static string ResolveConfigDirectory(string exeDirectory)
        {
            var localConfig = Path.Combine(exeDirectory, "CodexMaintenance.config");
            if (File.Exists(localConfig))
            {
                return exeDirectory;
            }

            var directory = new DirectoryInfo(exeDirectory);
            if (string.Equals(directory.Name, "CodexMaintenance", StringComparison.OrdinalIgnoreCase) && directory.Parent != null)
            {
                var parentConfig = Path.Combine(directory.Parent.FullName, "CodexMaintenance.config");
                if (File.Exists(parentConfig))
                {
                    return directory.Parent.FullName;
                }
            }

            return exeDirectory;
        }

        private static string ExpandPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim().Trim('"')));
        }
    }

    internal sealed class ProcessResult
    {
        public int ExitCode;
        public string Output;
    }
}


