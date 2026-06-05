# Codex Maintenance 中文使用指南

Codex Maintenance 是一个 Windows 本地维护工具，用来安全备份并清理 Codex / Codex++ 产生的本地日志和历史备份，适合解决 Codex 启动逐渐变慢、`.codex` 目录越来越大、备份文件越积越多等问题。

这个工具的设计原则是：先备份，再清理；先预览，再执行；本机配置不上传；不保存你的 GitHub、OpenAI 或其它 API 密钥。

## 主要功能

- 安全备份 Codex 关键文件：
  - `config.toml`
  - `auth.json`
  - `config.json`
  - `logs_2.sqlite`
  - `logs_2.sqlite-wal`
  - `logs_2.sqlite-shm`
- 清理日志数据库中的低价值日志：
  - 删除 `TRACE`
  - 删除 `DEBUG`
  - 删除 `INFO`
  - 保留 `WARN` / `ERROR`
- 对 SQLite WAL 文件执行 checkpoint，减少日志残留。
- 日志数据库达到阈值时才执行 `VACUUM`，避免每次都做耗时压缩。
- 清理过旧的维护备份，只保留指定数量。
- 清理 Codex++ 产生的旧备份目录。
- 可选检查本地代理 `127.0.0.1:7890`，并在需要时设置用户环境变量。
- 支持 `--dry-run` 预览模式，不实际修改文件。

## 适合谁使用

适合以下情况：

- Codex 或 Codex++ 打开越来越慢。
- `.codex` 目录里的 `logs_2.sqlite` 变大。
- Codex++ 的备份目录越来越多。
- 想把清理前的备份放到其它盘，例如 D 盘或 E 盘。
- 想先看清楚会删什么，再决定是否真正执行。
- 想做一个开源、可审计、不会夹带个人配置的小工具。

不适合以下情况：

- 你希望它清理所有聊天记录。
- 你希望它自动上传、自动同步或自动登录。
- 你希望它修改 API Key、账号登录状态或 Codex 本体程序。

这个工具只维护本地日志和备份，不负责账号、密钥或远程服务。

## 下载方式

你可以从 Release 页面下载压缩包：

```text
CodexMaintenance-v1.1.0.zip
```

解压后会看到类似结构：

```text
CodexMaintenance/
  CodexMaintenanceGui.exe
  CodexMaintenance.exe
  CodexMaintenance.config.example
  README.md
  README.zh-CN.md
  build.ps1
  src/
    CodexMaintenance.cs
    CodexMaintenanceGui.cs
```


## 推荐：使用图形界面

新版提供 `CodexMaintenanceGui.exe`，双击即可打开正常 Windows 窗口，不需要在终端里输入命令。

图形界面里可以直接完成：

- 选择 `.codex` 目录。
- 选择备份目录。
- 设置保留备份数量。
- 设置 `VACUUM` 阈值。
- 保存配置。
- 预览清理内容。
- 正式清理。
- 打开备份文件夹。

推荐流程：

1. 双击 `CodexMaintenanceGui.exe`。
2. 检查 Codex 目录和备份目录是否正确。
3. 点击“保存配置”。
4. 先点击“预览清理”。
5. 确认没问题后，再点击“正式清理”。

图形界面实际调用同目录下的 `CodexMaintenance.exe` 执行维护任务，所以核心清理逻辑和命令行版本一致。
## 第一次使用

打开 PowerShell 或 CMD，进入工具目录：

```powershell
cd /d <你的工具目录>\CodexMaintenance
```

如果你不用图形界面，也可以使用命令行。第一次建议先运行配置向导：

```powershell
.\CodexMaintenance.exe --configure
```

配置向导会让你设置：

1. `Codex folder (.codex)`：你的 Codex 配置目录，通常是：

   ```text
   %USERPROFILE%\.codex
   ```

2. `Backup folder`：清理前的安全备份保存目录，可以放到其它盘，例如：

   ```text
   D:\CodexMaintenance\backups
   E:\CodexMaintenance\backups
   ```

3. `Keep backups`：保留多少份维护备份，默认是 `5`。

4. `VACUUM limit`：日志数据库达到多少 MB 时才执行 `VACUUM`，默认是 `20.0 MB`。

配置会保存到本地配置文件：

```text
CodexMaintenance.config
```

这个文件只属于你的本机，里面可能包含你的本机路径，不建议上传到 GitHub。

## 推荐使用流程

### 第一步：先预览

```powershell
.\CodexMaintenance.exe --dry-run
```

预览模式只显示将要执行的动作，不会真正删除、复制、压缩或修改任何文件。

你会看到类似信息：

```text
Codex folder : C:\Users\你的用户名\.codex
Backup folder: D:\CodexMaintenance\backups
Keep backups : 5
VACUUM limit : 20.0 MB

1. Safety backup
[dry-run] Backup target: ...

2. Clean Codex logs
Log DB before: 19.7 MB
[dry-run] Would delete TRACE/DEBUG/INFO rows and checkpoint WAL.
```

如果预览中显示的 `.codex` 目录和备份目录都正确，再执行正式清理。

### 第二步：正式清理

```powershell
.\CodexMaintenance.exe
```

正式执行时，工具会先创建备份目录，然后再清理日志。

备份目录名称通常类似：

```text
20260605_184550
```

里面会保存清理前的重要文件副本。

### 第三步：需要时修改配置

如果你想修改 `.codex` 目录或备份目录，重新运行：

```powershell
.\CodexMaintenance.exe --configure
```

也可以用命令行直接设置：

```powershell
.\CodexMaintenance.exe --backup-root "D:\CodexBackups"
```

## 常用命令

查看帮助：

```powershell
.\CodexMaintenance.exe --help
```

预览清理动作：

```powershell
.\CodexMaintenance.exe --dry-run
```

正式清理：

```powershell
.\CodexMaintenance.exe
```

重新配置：

```powershell
.\CodexMaintenance.exe --configure
```

指定 Codex 配置目录：

```powershell
.\CodexMaintenance.exe --codex-home "%USERPROFILE%\.codex"
```

指定备份目录：

```powershell
.\CodexMaintenance.exe --backup-root "E:\CodexMaintenance\backups"
```

保留最近 10 份维护备份：

```powershell
.\CodexMaintenance.exe --keep-backups 10
```

执行完成后不等待回车，适合脚本调用：

```powershell
.\CodexMaintenance.exe --no-pause
```

检查并修复代理环境变量：

```powershell
.\CodexMaintenance.exe --fix-proxy
```

## 参数说明

| 参数 | 作用 |
| --- | --- |
| `--configure` | 打开配置向导，设置 `.codex` 目录、备份目录、保留数量和 VACUUM 阈值。 |
| `--codex-home <path>` | 指定 Codex 数据目录，并保存到本地配置。 |
| `--backup-root <path>` | 指定维护备份目录，并保存到本地配置。 |
| `--keep-backups <count>` | 设置保留多少份维护备份。 |
| `--dry-run` | 只预览，不实际修改。 |
| `--fix-proxy` | 如果 `127.0.0.1:7890` 可用，则设置用户级代理环境变量。 |
| `--no-pause` | 执行完成后不等待回车。 |
| `--help` | 显示帮助。 |

## 配置文件示例

```text
CodexHome=%USERPROFILE%\.codex
BackupRoot=backups
KeepBackups=5
VacuumThresholdMb=20.0
```

说明：

- `CodexHome` 是 Codex 的数据目录。
- `BackupRoot` 是维护备份目录。
- `BackupRoot=backups` 表示把备份放在配置文件旁边的 `backups` 文件夹。
- `KeepBackups=5` 表示只保留最近 5 份维护备份。
- `VacuumThresholdMb=20.0` 表示日志数据库达到 20 MB 时才执行 `VACUUM`。

如果你想把备份放到其它盘，可以改成：

```text
BackupRoot=D:\CodexMaintenance\backups
```

或者：

```text
BackupRoot=E:\CodexMaintenance\backups
```

## 安全说明

这个工具会读取并备份 `auth.json`，但它不会把内容上传到任何地方，也不会打印其中的密钥或 token。

请注意：

- 不要把真实的 `CodexMaintenance.config` 上传到 GitHub。
- 不要上传 `backups` 目录。
- 不要上传 `.codex` 目录。
- 不要上传 `auth.json`、`.sqlite`、`.sqlite-wal`、`.sqlite-shm`。
- 第一次运行建议先用 `--dry-run`。

仓库中的 `.gitignore` 已经默认忽略这些本地敏感文件。

## 会不会删除重要信息

默认情况下，它只删除日志数据库中的低级别日志：

```text
TRACE
DEBUG
INFO
```

它会保留：

```text
WARN
ERROR
```

正式清理前会创建安全备份。如果你想恢复，可以到备份目录里找到对应时间戳文件夹，把备份文件复制回 `.codex` 目录。

## 为什么清理后 Codex 会变快

Codex / Codex++ 使用过程中可能产生大量日志。日志数据库变大后，启动时读取、同步、扫描这些文件可能更慢。

这个工具不会删除 Codex 主程序，也不会修改模型配置。它只是减少低价值日志和旧备份，让本地启动负担小一些。

## 常见问题

### 1. 为什么要先备份？

因为日志数据库和配置文件都属于本机状态。即使清理逻辑很保守，也应该先留下恢复点。

### 2. 备份能放到其它盘吗？

可以。运行：

```powershell
.\CodexMaintenance.exe --configure
```

把 `Backup folder` 改到其它盘即可。

### 3. 为什么我的备份跑到 C 盘了？

旧版本示例配置默认使用 `%LOCALAPPDATA%`。新版已经改成更容易理解的 `backups`，也就是放在工具配置旁边。你仍然可以手动指定任意目录。

### 4. 清理时 Codex 要关闭吗？

建议关闭 Codex / Codex++ 后再清理。这样 SQLite 日志数据库不会被占用，清理更稳定。

### 5. `sqlite3.exe was not found` 怎么办？

工具需要 `sqlite3.exe` 执行日志数据库清理。如果提示找不到，可以安装 SQLite，或把已有的 `sqlite3.exe` 加入 PATH。即使找不到 SQLite，工具仍会先创建备份，但会跳过日志清理。

### 6. 代理修复是必须的吗？

不是。`--fix-proxy` 是可选功能，只在你需要把用户环境变量设置为 `http://127.0.0.1:7890` 时使用。

### 7. 会不会影响 API Key？

不会。工具不会修改 API Key，也不会上传、打印或解析你的 token。它只备份本地文件和清理日志数据库。

## 从源码构建

本项目不依赖第三方 NuGet 包。在 Windows 上运行：

```powershell
.\build.ps1
```

构建脚本会寻找 Windows 自带的 .NET Framework C# 编译器，例如：

```text
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```

构建输出位于：

```text
bin\CodexMaintenance.exe
```

## 开源发布建议

公开仓库建议只包含：

- 源码：`src/CodexMaintenance.cs`
- 构建脚本：`build.ps1`
- 示例配置：`CodexMaintenance.config.example`
- 文档：`README.md` / `README.zh-CN.md`
- 许可证：`LICENSE`

不要提交：

- `CodexMaintenance.config`
- `backups/`
- `.codex/`
- `auth.json`
- `logs_2.sqlite`
- 任何 API Key、token 或本机私密路径



