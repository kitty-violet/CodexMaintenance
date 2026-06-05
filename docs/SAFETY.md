# Safety Model

Codex Maintenance is intentionally conservative.

## Before Cleanup

The tool creates a safety backup of important local Codex files before changing the log database:

- `config.toml`
- `auth.json`
- `config.json`
- `logs_2.sqlite`
- `logs_2.sqlite-wal`
- `logs_2.sqlite-shm`

## What Gets Removed

Only low-level log rows are removed:

- `TRACE`
- `DEBUG`
- `INFO`

Higher-value records such as `WARN` and `ERROR` are kept.

## What Does Not Happen

The tool does not:

- delete credentials
- upload files
- create startup tasks
- run in the background
- silently change proxy variables unless `--fix-proxy` is explicitly used

## Recommended First Run

Always preview first:

```powershell
.\CodexMaintenance.exe --dry-run
```
