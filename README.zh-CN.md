# Codex Maintenance 中文使用指南

`CodexMaintenance.exe` 是一个 Windows 下的 Codex / Codex++ 本地维护工具，用来安全备份并清理 Codex 日志，减少日志数据库过大导致的启动变慢问题。

## 适合解决什么问题

- Codex / Codex++ 启动越来越慢。
- `.codex` 目录里的日志数据库变大。
- Codex++ 自动备份越来越多。
- 想把清理前的安全备份放到其它盘。
- 想先预览会处理什么，再决定是否正式执行。

## 快速开始

下载 release 压缩包并解压后，可以直接双击：

```text
CodexMaintenance-menu.cmd
```

也可以在 PowerShell 或 CMD 中运行配置向导：

```powershell
.\CodexMaintenance.exe --configure
```

它会让你设置：

1. `Codex folder (.codex)`：你的 Codex 配置目录，通常是 `%USERPROFILE%\.codex`。
2. `Backup folder`：清理前的安全备份保存目录，可以放到其它盘。
3. 保留多少份维护备份。
4. 日志数据库达到多大时才执行 `VACUUM`。

配置会保存到本地文件：

```text
CodexMaintenance.config
```

这个文件只属于你的本机，不建议上传到 GitHub。

## 菜单入口

`CodexMaintenance-menu.cmd` 提供一个简单菜单：

- 预览清理内容，不修改文件。
- 正式清理并创建备份。
- 修改 Codex/备份目录配置。
- 打开备份文件夹。
- 查看版本号。

## 推荐使用流程

### 1. 先预览

```powershell
.\CodexMaintenance.exe --dry-run
```

它只显示将要执行的动作，不会真正删除或修改文件。

### 2. 确认没问题后正式清理

```powershell
.\CodexMaintenance.exe
```

正式清理前会自动创建安全备份。

### 3. 修改备份目录

```powershell
.\CodexMaintenance.exe --configure
```

也可以直接指定：

```powershell
.\CodexMaintenance.exe --backup-root "D:\CodexBackups"
```

## 常用命令

```powershell
.\CodexMaintenance.exe --help
.\CodexMaintenance.exe --version
.\CodexMaintenance.exe --dry-run
.\CodexMaintenance.exe --configure
.\CodexMaintenance.exe --keep-backups 10
.\CodexMaintenance.exe --no-pause
```

## 安全说明

- 清理前会备份 `config.toml`、`auth.json`、`config.json` 和日志数据库文件。
- 只清理 `TRACE`、`DEBUG`、`INFO` 级别日志。
- 会保留 `WARN` / `ERROR` 等更重要的日志。
- 不会删除账号凭据。
- 不会把你的本机路径写进源码。
- 建议第一次运行先使用 `--dry-run`。

## 配置文件示例

```text
CodexHome=%USERPROFILE%\.codex
BackupRoot=backups
KeepBackups=5
VacuumThresholdMb=20.0
```

`BackupRoot=backups` 表示默认把备份放在配置文件旁边的 `backups` 文件夹。你也可以改成其它盘，例如：

```text
BackupRoot=D:\CodexMaintenance\backups
```

## 构建和打包

```powershell
.\build.ps1
.\scripts\smoke-test.ps1
.\scripts\package.ps1
```

## 更多文档

- [安全模型](docs/SAFETY.md)
- [常见问题排查](docs/TROUBLESHOOTING.zh-CN.md)

## 开源说明

仓库中只应包含源码、说明文档、示例配置和构建脚本；不要提交你的真实 `CodexMaintenance.config`、备份文件、日志数据库或账号文件。
