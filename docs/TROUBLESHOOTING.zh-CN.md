# 常见问题排查

## 备份为什么跑到 C 盘？

检查 `CodexMaintenance.config` 里的 `BackupRoot`。

建议设置为其它盘，例如：

```text
BackupRoot=D:\CodexMaintenance\backups
```

如果写成相对路径：

```text
BackupRoot=backups
```

则表示放在配置文件旁边的 `backups` 文件夹。

## 为什么双击后窗口一直不退出？

如果没有传 `--no-pause`，工具执行完会等待回车，方便你看结果。

脚本调用时可以使用：

```powershell
.\CodexMaintenance.exe --no-pause
```

## sqlite3 找不到怎么办？

工具需要 `sqlite3.exe` 来清理日志数据库。可以把 SQLite 或 Anaconda 自带的 `sqlite3.exe` 加入 PATH。

如果找不到 sqlite3，工具仍会先创建备份，然后跳过日志清理。

## 第一次应该怎么跑？

先预览：

```powershell
.\CodexMaintenance.exe --dry-run
```

确认没问题后再正式执行：

```powershell
.\CodexMaintenance.exe
```
