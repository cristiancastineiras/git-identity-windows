# git-identity portable (Windows, NativeAOT)

A standalone `.exe` that requires no installation, no .NET runtime, no admin rights. Just drop it anywhere on your `PATH` and run.

## What's in here

- `git-identity.exe` — 1.81 MB single-file native binary (NativeAOT, .NET 10)

That's it.

## Quick start

```cmd
:: Option 1: drop it on your existing PATH
copy git-identity.exe %USERPROFILE%\bin\

:: Option 2: just use it from this folder
git-identity.exe -p work
```

## Make `git identity` work

Run this once (from any shell that has git on PATH):

```cmd
git config --global alias.identity "!C:\path\to\git-identity.exe"
```

After that, `git identity -l` works in any shell that has git.

## Shell support

- **cmd.exe**: works directly (`git-identity.exe -l`)
- **PowerShell**: works directly (`& .\git-identity.exe -l`)
- **Git Bash / WSL / Cygwin**: works (`./git-identity.exe -l`)
- **Windows Terminal**: works
- **Explorer**: right-click a folder and you can launch cmd there (Windows default). The `register.cmd` in the `installer/` package adds a proper "git identity here" entry.

## Performance

| Metric                    | Value           |
|---------------------------|-----------------|
| File size                 | 1.81 MB         |
| Cold start                | ~100-200 ms     |
| Peak RSS during execution | ~2.5 MB         |
| Resident after exit       | 0 MB            |
| Allocations in hot path   | 0               |

## When to upgrade to the `installer/` package instead

The installer package is for when you want:
- `git identity` to work without configuring the git alias
- Explorer context menu entries (right-click any folder)
- Proper uninstall via `uninstall.cmd`
- The PowerShell shim for `git-identity.ps1`

See `../installer/README.md` for that.
