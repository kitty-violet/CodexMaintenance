using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace CodexMaintenance
{
    internal static class Program
    {
        private const string AppName = "Codex Maintenance";
        private const string SettingsFileName = "CodexMaintenance.config";
        private const string DefaultProxy = "http://127.0.0.1:7890";
        private const int DefaultKeepBackups = 5;
        private const double DefaultVacuumThresholdMb = 20.0;

        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = AppName;

            var options = Options.Parse(args);
            if (options.ShowHelp)
            {
                PrintHelp();
                return 0;
            }

            try
            {
                PrintHeader(AppName);

                var settingsPath = GetSettingsPath();
                var settings = Settings.Load(settingsPath);
                ApplyOptionOverrides(settings, options);

                if (options.Configure || settings.NeedsSetup())
                {
                    RunSetupWizard(settings);
                    settings.Save(settingsPath);
                    Success("Configuration saved: " + settingsPath);
                }

                ValidateSettings(settings);
                settings.Save(settingsPath);

                PrintSettings(settings);

                var backupDir = CreateBackup(settings, options.DryRun);
                CleanupCodexLogs(settings, options.DryRun);
                CleanupCodexPlusBackups(settings, options.DryRun);
                CleanupMaintenanceBackups(settings, options.DryRun);
                CheckOrFixProxy(options.FixProxy, options.DryRun);

                PrintHeader("Done");
                Success("Backup folder: " + backupDir);
                Console.WriteLine("Restart Codex/Codex++ after cleanup if it is currently open.");
                PauseIfNeeded(options);
                return 0;
            }
            catch (Exception ex)
            {
                Error(ex.Message);
                PauseIfNeeded(options);
                return 1;
            }
        }

        private static void ApplyOptionOverrides(Settings settings, Options options)
        {
            if (!string.IsNullOrWhiteSpace(options.CodexHome))
            {
                settings.CodexHome = NormalizePath(options.CodexHome);
            }

            if (!string.IsNullOrWhiteSpace(options.BackupRoot))
            {
                settings.BackupRoot = NormalizePath(options.BackupRoot);
            }

            if (options.KeepBackups.HasValue)
            {
                settings.KeepBackups = Math.Max(1, options.KeepBackups.Value);
            }
        }

        private static void RunSetupWizard(Settings settings)
        {
            PrintHeader("First-time setup");
            Console.WriteLine("Choose the Codex data folder and the backup folder.");
            Console.WriteLine("No personal path is stored in the source code. Paths are stored only in your local config file.");
            Console.WriteLine();

            var defaultCodexHome = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".codex");

            settings.CodexHome = PromptPath(
                "Codex folder (.codex)",
                string.IsNullOrWhiteSpace(settings.CodexHome) ? defaultCodexHome : settings.CodexHome,
                true);

            var defaultBackupRoot = GetDefaultBackupRoot();

            settings.BackupRoot = PromptPath(
                "Backup folder",
                string.IsNullOrWhiteSpace(settings.BackupRoot) ? defaultBackupRoot : settings.BackupRoot,
                false);

            settings.KeepBackups = PromptInt("How many maintenance backups to keep", settings.KeepBackups <= 0 ? DefaultKeepBackups : settings.KeepBackups, 1, 100);
            settings.VacuumThresholdMb = PromptDouble("Run VACUUM when log DB is at least this many MB", settings.VacuumThresholdMb <= 0 ? DefaultVacuumThresholdMb : settings.VacuumThresholdMb, 1.0, 4096.0);
        }

        private static void ValidateSettings(Settings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.CodexHome))
            {
                throw new InvalidOperationException("Codex folder is not configured. Run with --configure.");
            }

            if (!Directory.Exists(settings.CodexHome))
            {
                throw new DirectoryNotFoundException("Codex folder not found: " + settings.CodexHome);
            }

            if (string.IsNullOrWhiteSpace(settings.BackupRoot))
            {
                throw new InvalidOperationException("Backup folder is not configured. Run with --configure.");
            }

            Directory.CreateDirectory(settings.BackupRoot);

            if (settings.KeepBackups <= 0)
            {
                settings.KeepBackups = DefaultKeepBackups;
            }

            if (settings.VacuumThresholdMb <= 0)
            {
                settings.VacuumThresholdMb = DefaultVacuumThresholdMb;
            }
        }

        private static void PrintSettings(Settings settings)
        {
            Console.WriteLine("Codex folder : " + settings.CodexHome);
            Console.WriteLine("Backup folder: " + settings.BackupRoot);
            Console.WriteLine("Keep backups : " + settings.KeepBackups);
            Console.WriteLine("VACUUM limit : " + settings.VacuumThresholdMb.ToString("0.0") + " MB");
            Console.WriteLine();
        }

        private static string CreateBackup(Settings settings, bool dryRun)
        {
            PrintHeader("1. Safety backup");

            var backupDir = Path.Combine(settings.BackupRoot, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Console.WriteLine((dryRun ? "[dry-run] " : "") + "Backup target: " + backupDir);

            if (!dryRun)
            {
                Directory.CreateDirectory(backupDir);
            }

            CopyIfExists(Path.Combine(settings.CodexHome, "config.toml"), Path.Combine(backupDir, "config.toml"), dryRun);
            CopyIfExists(Path.Combine(settings.CodexHome, "auth.json"), Path.Combine(backupDir, "auth.json"), dryRun);
            CopyIfExists(Path.Combine(settings.CodexHome, "config.json"), Path.Combine(backupDir, "config.json"), dryRun);

            var logDb = Path.Combine(settings.CodexHome, "logs_2.sqlite");
            CopyIfExists(logDb, Path.Combine(backupDir, "logs_2.sqlite.before_cleanup"), dryRun);
            CopyIfExists(logDb + "-wal", Path.Combine(backupDir, "logs_2.sqlite-wal.before_cleanup"), dryRun);
            CopyIfExists(logDb + "-shm", Path.Combine(backupDir, "logs_2.sqlite-shm.before_cleanup"), dryRun);

            return backupDir;
        }

        private static void CleanupCodexLogs(Settings settings, bool dryRun)
        {
            PrintHeader("2. Clean Codex logs");

            var dbPath = Path.Combine(settings.CodexHome, "logs_2.sqlite");
            if (!File.Exists(dbPath))
            {
                Console.WriteLine("No logs_2.sqlite found. Skipping.");
                return;
            }

            var sqlitePath = FindSqlite3();
            if (sqlitePath == null)
            {
                Console.WriteLine("sqlite3.exe was not found. Backup was created, but log cleanup was skipped.");
                Console.WriteLine("Install sqlite3 or add it to PATH, then run this tool again.");
                return;
            }

            var beforeMb = FileSizeMb(dbPath);
            Console.WriteLine("Log DB before: " + beforeMb.ToString("0.0") + " MB");

            var runVacuum = beforeMb >= settings.VacuumThresholdMb;
            var sql = BuildCleanupSql(runVacuum);
            if (dryRun)
            {
                Console.WriteLine("[dry-run] Would delete TRACE/DEBUG/INFO rows and checkpoint WAL.");
                if (runVacuum)
                {
                    Console.WriteLine("[dry-run] Would run VACUUM because DB size is above threshold.");
                }
                return;
            }

            var output = RunProcessWithInput(sqlitePath, Quote(dbPath), sql, 120000);
            Console.WriteLine(output.Trim());

            var afterMb = FileSizeMb(dbPath);
            Console.WriteLine("Log DB after : " + afterMb.ToString("0.0") + " MB");
        }

        private static string BuildCleanupSql(bool runVacuum)
        {
            var builder = new StringBuilder();
            builder.Append("PRAGMA busy_timeout=3000;");
            builder.Append("DELETE FROM logs WHERE level IN ('TRACE','DEBUG','INFO');");
            builder.Append("PRAGMA wal_checkpoint(TRUNCATE);");
            if (runVacuum)
            {
                builder.Append("VACUUM;");
                builder.Append("PRAGMA wal_checkpoint(TRUNCATE);");
            }
            builder.Append("PRAGMA integrity_check;");
            builder.Append("SELECT level || ':' || COUNT(*) FROM logs GROUP BY level ORDER BY COUNT(*) DESC;");
            return builder.ToString();
        }

        private static void CleanupCodexPlusBackups(Settings settings, bool dryRun)
        {
            PrintHeader("3. Trim Codex++ backup folders");

            var liveBackupDir = Path.Combine(settings.CodexHome, "backups");
            var providerSyncDir = Path.Combine(settings.CodexHome, "backups_state", "provider-sync");

            var removedLive = RemoveOldDirectories(liveBackupDir, "codex-plus-live-*", settings.KeepBackups, dryRun);
            var removedProvider = RemoveOldDirectories(providerSyncDir, "*", settings.KeepBackups, dryRun);

            Console.WriteLine("Codex++ live backups removed      : " + removedLive);
            Console.WriteLine("Provider-sync backups removed     : " + removedProvider);
        }

        private static void CleanupMaintenanceBackups(Settings settings, bool dryRun)
        {
            PrintHeader("4. Trim maintenance backups");
            var removed = RemoveOldDirectories(settings.BackupRoot, "*", settings.KeepBackups, dryRun);
            Console.WriteLine("Maintenance backups removed       : " + removed);
        }

        private static void CheckOrFixProxy(bool fixProxy, bool dryRun)
        {
            PrintHeader("5. Proxy check");

            var proxyOpen = IsPortOpen("127.0.0.1", 7890, 250);
            Console.WriteLine("127.0.0.1:7890 reachable: " + (proxyOpen ? "yes" : "no"));
            Console.WriteLine("HTTP_PROXY : " + DisplayValue(Environment.GetEnvironmentVariable("HTTP_PROXY", EnvironmentVariableTarget.User)));
            Console.WriteLine("HTTPS_PROXY: " + DisplayValue(Environment.GetEnvironmentVariable("HTTPS_PROXY", EnvironmentVariableTarget.User)));
            Console.WriteLine("ALL_PROXY  : " + DisplayValue(Environment.GetEnvironmentVariable("ALL_PROXY", EnvironmentVariableTarget.User)));

            if (!fixProxy)
            {
                Console.WriteLine("Proxy variables were not changed. Use --fix-proxy to set them.");
                return;
            }

            if (!proxyOpen)
            {
                Console.WriteLine("Proxy port is not reachable. Variables were not changed.");
                return;
            }

            if (dryRun)
            {
                Console.WriteLine("[dry-run] Would set user proxy variables to " + DefaultProxy);
                return;
            }

            Environment.SetEnvironmentVariable("HTTP_PROXY", DefaultProxy, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", DefaultProxy, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("ALL_PROXY", DefaultProxy, EnvironmentVariableTarget.User);
            Success("User proxy variables were updated.");
        }

        private static int RemoveOldDirectories(string root, string pattern, int keep, bool dryRun)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                return 0;
            }

            var rootFullPath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var dirs = Directory.GetDirectories(root, pattern)
                .Select(path => new DirectoryInfo(path))
                .OrderByDescending(dir => dir.LastWriteTimeUtc)
                .Skip(keep)
                .ToList();

            var removed = 0;
            foreach (var dir in dirs)
            {
                var dirFullPath = Path.GetFullPath(dir.FullName).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (!dirFullPath.StartsWith(rootFullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Skipped unsafe path: " + dir.FullName);
                    continue;
                }

                Console.WriteLine((dryRun ? "[dry-run] Remove " : "Remove ") + dir.FullName);
                if (!dryRun)
                {
                    try
                    {
                        dir.Delete(true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Remove failed: " + dir.FullName + " (" + ex.Message + ")");
                        continue;
                    }
                }
                removed++;
            }
            return removed;
        }

        private static void CopyIfExists(string source, string destination, bool dryRun)
        {
            if (!File.Exists(source))
            {
                return;
            }

            Console.WriteLine((dryRun ? "[dry-run] Copy " : "Copy ") + source);
            if (dryRun)
            {
                return;
            }

            var parent = Path.GetDirectoryName(destination);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                Directory.CreateDirectory(parent);
            }
            File.Copy(source, destination, true);
        }

        private static string FindSqlite3()
        {
            var candidates = new List<string>();
            var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            candidates.AddRange(path.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(dir => Path.Combine(dir.Trim().Trim('"'), "sqlite3.exe")));

            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "sqlite3.exe"));
            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SQLite", "sqlite3.exe"));

            return candidates.FirstOrDefault(File.Exists);
        }

        private static string RunProcessWithInput(string fileName, string arguments, string standardInput, int timeoutMs)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            using (var process = Process.Start(psi))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start process: " + fileName);
                }

                process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data != null)
                    {
                        stdout.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data != null)
                    {
                        stderr.AppendLine(e.Data);
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.StandardInput.Write(standardInput);
                process.StandardInput.Close();

                if (!process.WaitForExit(timeoutMs))
                {
                    try { process.Kill(); } catch { }
                    throw new TimeoutException(fileName + " timed out.");
                }
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    var errorText = stderr.ToString();
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(errorText) ? stdout.ToString() : errorText);
                }

                return stdout.ToString() + stderr.ToString();
            }
        }

        private static string PromptPath(string label, string current, bool mustExist)
        {
            while (true)
            {
                Console.Write(label + " [" + current + "]: ");
                var input = Console.ReadLine();
                var chosen = string.IsNullOrWhiteSpace(input) ? current : input.Trim().Trim('"');
                chosen = NormalizePath(chosen);

                if (!mustExist || Directory.Exists(chosen))
                {
                    if (!mustExist)
                    {
                        Directory.CreateDirectory(chosen);
                    }
                    return chosen;
                }

                Error("Folder not found: " + chosen);
            }
        }

        private static int PromptInt(string label, int current, int min, int max)
        {
            while (true)
            {
                Console.Write(label + " [" + current + "]: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    return current;
                }

                int value;
                if (int.TryParse(input.Trim(), out value) && value >= min && value <= max)
                {
                    return value;
                }

                Error("Enter a number between " + min + " and " + max + ".");
            }
        }

        private static double PromptDouble(string label, double current, double min, double max)
        {
            while (true)
            {
                Console.Write(label + " [" + current.ToString("0.0") + "]: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    return current;
                }

                double value;
                if (double.TryParse(input.Trim(), out value) && value >= min && value <= max)
                {
                    return value;
                }

                Error("Enter a number between " + min.ToString("0.0") + " and " + max.ToString("0.0") + ".");
            }
        }

        private static bool IsPortOpen(string host, int port, int timeoutMs)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect(host, port, null, null);
                    try
                    {
                        var ok = result.AsyncWaitHandle.WaitOne(timeoutMs);
                        if (!ok)
                        {
                            return false;
                        }
                        client.EndConnect(result);
                        return true;
                    }
                    finally
                    {
                        result.AsyncWaitHandle.Close();
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private static string GetSettingsPath()
        {
            var module = Process.GetCurrentProcess().MainModule;
            var exePath = module == null ? null : module.FileName;
            var exeDir = string.IsNullOrWhiteSpace(exePath) ? AppDomain.CurrentDomain.BaseDirectory : Path.GetDirectoryName(exePath);
            exeDir = exeDir ?? AppDomain.CurrentDomain.BaseDirectory;

            var dirInfo = new DirectoryInfo(exeDir);
            if (string.Equals(dirInfo.Name, "CodexMaintenance", StringComparison.OrdinalIgnoreCase) && dirInfo.Parent != null)
            {
                return Path.Combine(dirInfo.Parent.FullName, SettingsFileName);
            }

            return Path.Combine(exeDir, SettingsFileName);
        }

        private static string GetDefaultBackupRoot()
        {
            var settingsDir = Path.GetDirectoryName(GetSettingsPath());
            if (string.IsNullOrWhiteSpace(settingsDir))
            {
                settingsDir = AppDomain.CurrentDomain.BaseDirectory;
            }
            return Path.Combine(settingsDir, "backups");
        }

        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
        }

        private static double FileSizeMb(string path)
        {
            return new FileInfo(path).Length / 1024.0 / 1024.0;
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static string DisplayValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(not set)" : value;
        }

        private static void PrintHeader(string text)
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine(text);
            Console.WriteLine("========================================");
        }

        private static void Success(string text)
        {
            Console.WriteLine("[OK] " + text);
        }

        private static void Error(string text)
        {
            Console.WriteLine("[ERROR] " + text);
        }

        private static void PauseIfNeeded(Options options)
        {
            if (options.NoPause)
            {
                return;
            }

            Console.WriteLine();
            Console.Write("Press Enter to exit...");
            Console.ReadLine();
        }

        private static void PrintHelp()
        {
            Console.WriteLine(AppName);
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  CodexMaintenance.exe [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --configure              Run setup wizard.");
            Console.WriteLine("  --codex-home <path>      Set Codex data folder for this run and save it.");
            Console.WriteLine("  --backup-root <path>     Set backup folder for this run and save it.");
            Console.WriteLine("  --keep-backups <count>   Number of backup folders to keep. Default: 5.");
            Console.WriteLine("  --dry-run                Show actions without writing or deleting.");
            Console.WriteLine("  --fix-proxy              Set user proxy variables if 127.0.0.1:7890 is reachable.");
            Console.WriteLine("  --no-pause               Exit without waiting for Enter.");
            Console.WriteLine("  --help                   Show help.");
        }
    }

    internal sealed class Options
    {
        public bool ShowHelp;
        public bool Configure;
        public bool DryRun;
        public bool FixProxy;
        public bool NoPause;
        public string CodexHome;
        public string BackupRoot;
        public int? KeepBackups;

        public static Options Parse(string[] args)
        {
            var options = new Options();
            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];
                if (EqualsArg(arg, "--help") || EqualsArg(arg, "-h") || EqualsArg(arg, "/?"))
                {
                    options.ShowHelp = true;
                }
                else if (EqualsArg(arg, "--configure"))
                {
                    options.Configure = true;
                }
                else if (EqualsArg(arg, "--dry-run"))
                {
                    options.DryRun = true;
                }
                else if (EqualsArg(arg, "--fix-proxy"))
                {
                    options.FixProxy = true;
                }
                else if (EqualsArg(arg, "--no-pause"))
                {
                    options.NoPause = true;
                }
                else if (EqualsArg(arg, "--codex-home"))
                {
                    options.CodexHome = RequireValue(args, ref index, arg);
                }
                else if (EqualsArg(arg, "--backup-root"))
                {
                    options.BackupRoot = RequireValue(args, ref index, arg);
                }
                else if (EqualsArg(arg, "--keep-backups"))
                {
                    int keep;
                    if (!int.TryParse(RequireValue(args, ref index, arg), out keep))
                    {
                        throw new ArgumentException("--keep-backups requires a number.");
                    }
                    options.KeepBackups = keep;
                }
                else
                {
                    throw new ArgumentException("Unknown option: " + arg);
                }
            }
            return options;
        }

        private static bool EqualsArg(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static string RequireValue(string[] args, ref int index, string optionName)
        {
            if (index + 1 >= args.Length)
            {
                throw new ArgumentException(optionName + " requires a value.");
            }
            index++;
            return args[index];
        }
    }

    internal sealed class Settings
    {
        public string CodexHome;
        public string BackupRoot;
        public int KeepBackups = DefaultKeepBackupsValue;
        public double VacuumThresholdMb = DefaultVacuumThresholdMbValue;

        private const int DefaultKeepBackupsValue = 5;
        private const double DefaultVacuumThresholdMbValue = 20.0;

        public bool NeedsSetup()
        {
            return string.IsNullOrWhiteSpace(CodexHome) || string.IsNullOrWhiteSpace(BackupRoot);
        }

        public static Settings Load(string path)
        {
            var settings = new Settings();
            if (!File.Exists(path))
            {
                return settings;
            }

            foreach (var rawLine in File.ReadAllLines(path))
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
                var value = line.Substring(separator + 1).Trim();

                if (string.Equals(key, "CodexHome", StringComparison.OrdinalIgnoreCase))
                {
                    settings.CodexHome = Expand(value);
                }
                else if (string.Equals(key, "BackupRoot", StringComparison.OrdinalIgnoreCase))
                {
                    settings.BackupRoot = Expand(value);
                }
                else if (string.Equals(key, "KeepBackups", StringComparison.OrdinalIgnoreCase))
                {
                    int keep;
                    if (int.TryParse(value, out keep))
                    {
                        settings.KeepBackups = keep;
                    }
                }
                else if (string.Equals(key, "VacuumThresholdMb", StringComparison.OrdinalIgnoreCase))
                {
                    double mb;
                    if (double.TryParse(value, out mb))
                    {
                        settings.VacuumThresholdMb = mb;
                    }
                }
            }

            return settings;
        }

        public void Save(string path)
        {
            var lines = new[]
            {
                "# Codex Maintenance local configuration",
                "# This file is machine-local and should not be committed.",
                "CodexHome=" + (CodexHome ?? string.Empty),
                "BackupRoot=" + (BackupRoot ?? string.Empty),
                "KeepBackups=" + KeepBackups,
                "VacuumThresholdMb=" + VacuumThresholdMb.ToString("0.0")
            };

            var parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                Directory.CreateDirectory(parent);
            }
            File.WriteAllLines(path, lines, Encoding.UTF8);
        }

        private static string Expand(string value)
        {
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(value.Trim().Trim('"')));
        }
    }
}


