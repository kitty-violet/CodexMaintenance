# Codex Maintenance

A small Windows maintenance utility for local Codex / Codex++ installations.

It helps keep startup fast by safely backing up and trimming noisy local log data. It is designed to be portable: users choose their own `.codex` folder and backup folder, and no personal paths are stored in source code.

## Features

- First-run setup wizard for:
  - Codex data folder, usually `%USERPROFILE%\.codex`
  - maintenance backup folder
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
- Dry-run mode and a Windows menu launcher for safer previews.

## Quick Start

Download a release zip, extract it, then either double-click:

```text
CodexMaintenance-menu.cmd
```

or configure from a terminal:

```powershell
.\CodexMaintenance.exe --configure
```

The tool stores local settings in:

```text
CodexMaintenance.config
```

That file is intentionally ignored by Git.

## Menu Launcher

`CodexMaintenance-menu.cmd` provides a simple menu:

- dry-run preview
- run cleanup with backup
- configure folders
- open backup folder
- show version

The menu uses relative paths, so it can be copied with the executable.

## Configuration Example

```text
CodexHome=%USERPROFILE%\.codex
BackupRoot=backups
KeepBackups=5
VacuumThresholdMb=20.0
```

`BackupRoot=backups` keeps maintenance backups beside the local config file. You can change it to another drive, for example:

```text
BackupRoot=D:\CodexMaintenance\backups
```

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
--version                Show version.
--help                   Show help.
```

Examples:

```powershell
.\CodexMaintenance.exe --dry-run
.\CodexMaintenance.exe --configure
.\CodexMaintenance.exe --backup-root "D:\CodexBackups"
.\CodexMaintenance.exe --fix-proxy
```

## Build and Package

This project intentionally avoids external dependencies. On Windows, build with:

```powershell
.\build.ps1
```

Run a smoke test:

```powershell
.\scripts\smoke-test.ps1
```

Create a release zip:

```powershell
.\scripts\package.ps1
```

Build output:

```text
bin\CodexMaintenance.exe
artifacts\CodexMaintenance-v<version>.zip
```

## Safety Notes

- The tool backs up important files before modifying the log database.
- It only removes low-level log rows: `TRACE`, `DEBUG`, and `INFO`.
- It does not delete account credentials.
- It does not contain personal machine paths in source code.
- Run `--dry-run` first if you want to preview actions.

More details:

- [Safety model](docs/SAFETY.md)
- [中文排错指南](docs/TROUBLESHOOTING.zh-CN.md)

## License

MIT
