# Codex Maintenance 中文使用指南

`CodexMaintenance.exe` 是一个 Windows 下的 Codex / Codex++ 本地维护工具，用来安全备份并清理 Codex 日志，减少日志数据库过大导致的启动变慢问题。

## 适合解决什么问题

- Codex / Codex++ 启动越来越慢。
- `.codex` 目录里的日志数据库变大。
- Codex++ 自动备份越来越多。
- 想把清理前的备份放到其它盘。
- 想先预览会删什么，再真正执行。

## 第一次使用

打开 PowerShell 或 CMD，进入工具目录：

```powershell
cd /d <你的工具目录>\CodexMaintenance
```

第一次建议先运行配置向导：

```powershell
.\CodexMaintenance.exe --configure
```

它会让你设置两个目录：

1. `Codex folder (.codex)`：你的 Codex 配置目录，通常是 `%USERPROFILE%\.codex`
2. `Backup folder`：清理前的安全备份保存目录，可以放到其它盘

配置会保存到工具旁边的本地文件：

```text
CodexMaintenance.config
```

这个文件只属于你本机，不建议上传到 GitHub。

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

重新进入配置向导，修改 `Backup folder` 即可。

## 常用命令

```powershell
.\CodexMaintenance.exe --help
```

查看帮助。

```powershell
.\CodexMaintenance.exe --dry-run
```

预览清理动作，不实际修改。

```powershell
.\CodexMaintenance.exe --configure
```

重新设置 `.codex` 目录和备份目录。

```powershell
.\CodexMaintenance.exe --backup-root "D:\CodexBackups"
```

直接指定备份目录。

```powershell
.\CodexMaintenance.exe --keep-backups 10
```

保留最近 10 份维护备份。

```powershell
.\CodexMaintenance.exe --no-pause
```

执行完后不等待按回车，适合脚本调用。

```powershell
.\CodexMaintenance.exe --fix-proxy
```

如果 `127.0.0.1:7890` 可连接，则设置用户级代理环境变量：

- `HTTP_PROXY`
- `HTTPS_PROXY`
- `ALL_PROXY`

## 它会备份什么

正式清理前，会把下面这些文件复制到你设置的备份目录：

- `config.toml`
- `auth.json`
- `config.json`
- `logs_2.sqlite`
- `logs_2.sqlite-wal`
- `logs_2.sqlite-shm`

备份目录按时间命名，例如：

```text
20260604_200000
```

## 它会清理什么

主要清理 Codex 日志数据库里的低等级日志：

- `TRACE`
- `DEBUG`
- `INFO`

会保留更重要的错误和警告日志。

如果日志数据库超过阈值，工具会执行压缩整理，减少体积。

## 它不会做什么

- 不会删除你的 Codex 账号。
- 不会删除你的登录凭据。
- 不会把个人路径写进源码。
- 不会上传任何文件。
- 不会联网。

## 如果提示找不到 sqlite3

工具需要 `sqlite3.exe` 来清理日志数据库。

如果提示找不到，可以：

1. 安装 SQLite 命令行工具；
2. 或把已有的 `sqlite3.exe` 所在目录加入 `PATH`；
3. 然后重新运行工具。

即使找不到 `sqlite3.exe`，工具也会先做安全备份，只是跳过日志数据库清理。

## 打包和构建

如果你想从源码重新生成 exe：

```powershell
.\build.ps1
```

生成结果在：

```text
bin\CodexMaintenance.exe
```

## 上传 GitHub 前注意

不要提交这些本地文件：

- `CodexMaintenance.config`
- `bin\`
- `*.exe`
- 备份目录

这些已经写在 `.gitignore` 里。

## 建议

如果只是日常使用，推荐顺序是：

```powershell
.\CodexMaintenance.exe --dry-run
.\CodexMaintenance.exe
```

第一次先预览，确认没问题后再正式清理。 
