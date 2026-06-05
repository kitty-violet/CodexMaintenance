# Contributing

Thanks for improving Codex Maintenance.

## Local Workflow

1. Run a dry run first:

   ```powershell
   .\CodexMaintenance.exe --dry-run
   ```

2. Build from source:

   ```powershell
   .\build.ps1
   ```

3. Run smoke tests:

   ```powershell
   .\scripts\smoke-test.ps1
   ```

## Safety Rules

- Do not commit `CodexMaintenance.config`.
- Do not commit backups, logs, SQLite databases, or credentials.
- Keep destructive actions guarded by backups and dry-run support.
- Prefer clear console output over silent changes.
