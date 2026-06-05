# Security Policy

## Supported Versions

Only the latest release is actively maintained.

## What This Tool Touches

`CodexMaintenance` is a local Windows utility. It can read and back up files inside the configured Codex data folder, including `auth.json`, because the backup step is meant to preserve important local state before cleanup.

The project repository must not contain:

- real `CodexMaintenance.config`
- `auth.json`
- log databases such as `logs_2.sqlite`
- backup folders
- API keys, tokens, or screenshots containing secrets

## Reporting Issues

If you find a security issue, open a private report if GitHub security advisories are enabled, or create an issue with sensitive details removed.
