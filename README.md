# Codex Maintenance

Codex Maintenance is a small Windows utility for local Codex / Codex++ installations. It safely backs up important local files, trims noisy log data, and removes old maintenance backups so Codex startup stays lightweight.

The tool is designed around four rules:

1. Back up before cleanup.
2. Preview before changing anything.
3. Keep local configuration out of Git.
4. Never upload secrets, credentials, logs, or backups.

## Features

- First-run setup wizard for:
  - Codex data folder, usually `%USERPROFILE%\.codex`
  - maintenance backup folder
  - backup retention count
  - SQLite `VACUUM` threshold
- Safety backup before cleanup:
  - `config.toml`
  - `auth.json`
  - `config.json`
  - `logs_2.sqlite`
  - `logs_2.sqlite-wal`
  - `logs_2.sqlite-shm`
- Log database cleanup:
  - removes `TRACE`, `DEBUG`, and `INFO` rows
  - keeps `WARN` and `ERROR` rows
  - checkpoints WAL files
  - runs `VACUUM` only when the database exceeds a configurable threshold
- Backup retention:
  - trims old maintenance backup folders
  - trims old Codex++ live/provider-sync backup folders
- Optional proxy helper:
  - checks whether `127.0.0.1:7890` is reachable
  - can set user-level `HTTP_PROXY`, `HTTPS_PROXY`, and `ALL_PROXY` with `--fix-proxy`
- Dry-run mode for safe previews.

## Who This Is For

Use this tool if:

- Codex / Codex++ startup is getting slower.
- Your `.codex` log database is getting large.
- Codex++ backup folders are accumulating.
- You want cleanup backups on another drive.
- You want a simple open-source utility that is easy to audit.

Do not use this tool if you expect it to manage API keys, perform GitHub uploads, log into services, or modify Codex itself. It only maintains local logs and backups.

## Download

Download the latest release archive:

```text
CodexMaintenance-v1.1.1.zip
```

After extracting it, the folder looks like this:

```text
CodexMaintenance/
  CodexMaintenanceGui.exe
  CodexMaintenance.exe
  CodexMaintenance.config.example
  README.md
  README.zh-CN.md
  assets/
    CodexMaintenance.ico
  scripts/
    generate-icon.ps1
  build.ps1
  src/
    CodexMaintenance.cs
    CodexMaintenanceGui.cs
```



## Portability Notes

This project is not tied to one specific computer. It does not hard-code a username, drive letter, or machine-local path such as `X:\Tools`.

Default behavior:

- `CodexMaintenance.config` is stored beside the executable.
- `BackupRoot=backups` stores backups beside the local config file.
- The recommended Codex folder is `%USERPROFILE%\.codex`.
- Users can choose any backup folder through the GUI or with `--configure`.

A user can extract the tool to any folder, for example:

```text
D:\Tools\CodexMaintenance
C:\Users\Someone\Downloads\CodexMaintenance
```

Then they can double-click `CodexMaintenanceGui.exe`.

The normal requirements still apply: Windows, .NET Framework, and `sqlite3.exe` for log database cleanup. If SQLite is unavailable, the tool can still create backups but skips database cleanup.
## Recommended: Use the GUI

The latest version includes `CodexMaintenanceGui.exe`. Double-click it to open a normal Windows desktop window instead of typing commands in a terminal.

The GUI lets you:

- choose the `.codex` folder;
- choose the backup folder;
- set backup retention;
- set the `VACUUM` threshold;
- save configuration;
- preview cleanup;
- run cleanup;
- open the backup folder.

Recommended workflow:

1. Double-click `CodexMaintenanceGui.exe`.
2. Check the Codex folder and backup folder.
3. Click `Save`.
4. Click `Dry Run` first.
5. If the preview looks right, click `Run Cleanup`.

The GUI calls the same `CodexMaintenance.exe` command-line tool internally, so the maintenance logic is shared between GUI and CLI usage.
## First Run

Open PowerShell or CMD in the tool folder:

```powershell
cd /d <your-tool-folder>\CodexMaintenance
```

If you prefer the command line, run the setup wizard:

```powershell
.\CodexMaintenance.exe --configure
```

The wizard asks for:

1. `Codex folder (.codex)` — usually `%USERPROFILE%\.codex`.
2. `Backup folder` — where safety backups are stored.
3. How many maintenance backups to keep.
4. The database size threshold for running `VACUUM`.

The local configuration is saved as:

```text
CodexMaintenance.config
```

This file may contain machine-local paths and should not be committed.

## Recommended Workflow

### 1. Preview first

```powershell
.\CodexMaintenance.exe --dry-run
```

Dry-run mode prints what would happen without copying, deleting, compacting, or modifying anything.

### 2. Run cleanup

```powershell
.\CodexMaintenance.exe
```

The tool creates a timestamped backup folder before cleaning logs.

Example backup folder name:

```text
20260605_184550
```

### 3. Reconfigure when needed

```powershell
.\CodexMaintenance.exe --configure
```

You can also set values directly:

```powershell
.\CodexMaintenance.exe --backup-root "D:\CodexBackups"
.\CodexMaintenance.exe --keep-backups 10
```

## Command Reference

```text
--configure              Run setup wizard.
--codex-home <path>      Set Codex data folder for this run and save it.
--backup-root <path>     Set backup folder for this run and save it.
--keep-backups <count>   Number of backup folders to keep. Default: 5.
--dry-run                Show actions without writing or deleting.
--fix-proxy              Set user proxy variables if 127.0.0.1:7890 is reachable.
--no-pause               Exit without waiting for Enter.
--help                   Show help.
```

## Configuration Example

```text
CodexHome=%USERPROFILE%\.codex
BackupRoot=backups
KeepBackups=5
VacuumThresholdMb=20.0
```

`BackupRoot=backups` stores backups beside the local config file. You can use another drive instead:

```text
BackupRoot=D:\CodexMaintenance\backups
```

## Safety Notes

The tool may copy `auth.json` into the local backup folder, but it does not upload, print, parse, or transmit credentials.

Do not commit:

- `CodexMaintenance.config`
- `backups/`
- `.codex/`
- `auth.json`
- `logs_2.sqlite`
- `logs_2.sqlite-wal`
- `logs_2.sqlite-shm`
- API keys, tokens, or machine-local secret files

The included `.gitignore` is configured to avoid committing these files.

## What Gets Deleted

The cleanup SQL removes only low-level log rows:

```text
TRACE
DEBUG
INFO
```

It keeps higher-value rows:

```text
WARN
ERROR
```

Before cleanup, the tool creates a safety backup. To restore, copy files from a timestamped backup folder back into your `.codex` directory.

## Troubleshooting

### `sqlite3.exe was not found`

Install SQLite or put `sqlite3.exe` on PATH. The tool can still create backups, but log cleanup is skipped without SQLite.

### Backup folder is on the wrong drive

Run:

```powershell
.\CodexMaintenance.exe --configure
```

Then set `Backup folder` to the desired directory.

### Should Codex be closed before cleanup?

Recommended: yes. Closing Codex / Codex++ avoids SQLite lock conflicts and makes cleanup more reliable.

### Does this affect API keys?

No. The tool does not edit API keys or login state. It only backs up local files and cleans low-level log rows.

### Does `--fix-proxy` run automatically?

No. Proxy changes only happen when you explicitly pass `--fix-proxy`.

## Build From Source

No external NuGet packages are required.

```powershell
.\build.ps1
```

The build script uses the Windows .NET Framework C# compiler when available:

```text
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```

Build output:

```text
bin\CodexMaintenance.exe
```

## Repository Hygiene

Public repositories should include source, docs, example config, build scripts, and license files only. Keep real local config, backups, logs, databases, and credentials out of the repository.

## License

MIT






