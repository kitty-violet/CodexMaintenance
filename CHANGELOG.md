# Changelog

## v1.1.2

- Fix package output so the GUI executable and icon assets are included.
- Resolve relative paths from `CodexMaintenance.config` beside that config file.
- Update the CLI version shown by `--version`.
- Strengthen smoke tests for portable config behavior.

## v1.1.1

- Add a Windows desktop GUI for configuring and running maintenance.
- Add a branded application icon and generated icon assets.
- Improve README portability notes for open-source users.

## v1.0.2

- Add `--version` support.
- Add a reusable Windows menu launcher for safer interactive use.
- Add package and smoke-test scripts.
- Add GitHub Actions CI.
- Add safety and troubleshooting documentation.

## v1.0.1

- Fix backup path behavior so local backups can stay beside the tool/config instead of defaulting to AppData.
- Make package-folder executables share the parent `CodexMaintenance.config`.
- Fix Chinese README encoding.
- Improve SQLite cleanup execution by feeding SQL through standard input.
- Keep real local config, backups, logs, and credentials out of the repository.

## v1.0.0

- Initial open-source release.

