# Codex Maintenance

A small Windows maintenance utility for local Codex / Codex++ installations.

It helps keep startup fast by safely backing up and trimming noisy local log data. It is designed to be portable: users choose their own `.codex` folder and backup folder on first run, and no personal paths are stored in source code.

## Features

- First-run setup wizard for:
  - Codex data folder, usually `%USERPROFILE%\.codex`
  - Maintenance backup folder
- Safety backups before cleanup:
  - `config.toml`
  - `auth.json`
  - `config.json`
  - `logs_2.sqlite` plus WAL/SHM files when present
- Log database cleanup:
  - removes `TRACE`, `DEBUG`, and `INFO` rows
  - keeps warning/error level records
  - checkpoints WAL files
  - runs `VACUUM` only when the database exceeds a configurable size threshold
- Backup retention:
  - trims old maintenance backups
  - trims old Codex++ live/provider-sync backup folders
- Optional proxy helper:
  - checks `127.0.0.1:7890`
  - can set user `HTTP_PROXY`, `HTTPS_PROXY`, and `ALL_PROXY` with `--fix-proxy`
- Dry-run mode for safe previews.

## Quick Start

Download or build `CodexMaintenance.exe`, then run:

```powershell
.\CodexMaintenance.exe --configure
```

The tool stores local settings next to the executable:

```text
CodexMaintenance.config
```

That file is intentionally ignored by Git.

## Usage

```powershell
CodexMaintenance.exe [options]
```

Options:

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

Examples:

```powershell
.\CodexMaintenance.exe --dry-run
.\CodexMaintenance.exe --configure
.\CodexMaintenance.exe --backup-root "D:\CodexBackups"
.\CodexMaintenance.exe --fix-proxy
```

## Build

This project intentionally avoids external dependencies. On Windows, build with:

```powershell
.\build.ps1
```

The script uses the .NET Framework C# compiler included with Windows when available:

```text
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```

Build output:

```text
bin\CodexMaintenance.exe
```

## Safety Notes

- The tool backs up important files before modifying the log database.
- It only removes low-level log rows: `TRACE`, `DEBUG`, and `INFO`.
- It does not delete account credentials.
- It does not contain personal machine paths in source code.
- Run `--dry-run` first if you want to preview actions.

## License

MIT
