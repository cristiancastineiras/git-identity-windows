# git-identity installer (Windows, NativeAOT)

User-level installer for git-identity. No admin rights, no MSI, no driver signing, no Windows Store. Just a `cmd.exe` script that does the right thing.

## What's in here

| File             | Purpose                                                     |
|------------------|-------------------------------------------------------------|
| `git-identity.exe` | The actual tool (1.81 MB, NativeAOT)                     |
| `install.cmd`    | Copies the exe to `%LOCALAPPDATA%\Programs\git-identity\`, adds it to your user PATH, and registers a git alias so `git identity` works |
| `uninstall.cmd`  | Reverses everything `install.cmd` did                      |
| `register.cmd`   | Adds Explorer context menu entries (right-click any folder) |
| `unregister.cmd` | Removes the context menu entries                            |
| `git-identity.ps1` | PowerShell shim: source from your profile for `git-identity` in PS1 |
| `README.md`      | This file                                                    |

## Install

```cmd
:: From this folder:
install.cmd
```

This:
1. Creates `%LOCALAPPDATA%\Programs\git-identity\` and copies the .exe there.
2. Adds that directory to your user PATH (persistent, no admin).
3. Runs `git config --global alias.identity '!"C:\Users\...\git-identity.exe"'` so `git identity` works in any shell with git.

### Optional: Explorer context menu

```cmd
register.cmd
```

This adds two context menu entries to the Windows Explorer:
- **Right-click a folder** → "git identity here"
- **Right-click the background of a folder** → "git identity here"

Both open a `cmd.exe` in that folder. Run `git identity -l` to see your identities, then `git identity work` to switch.

## Uninstall

```cmd
uninstall.cmd
unregister.cmd  :: optional
```

`uninstall.cmd` removes the files, the PATH entry, and the git alias. It does **not** touch your identities stored in the global git config (those stay; remove them individually with `git identity -r <name>`).

## Use from PowerShell

```powershell
# Once per session:
. "$env:LOCALAPPDATA\Programs\git-identity\git-identity.ps1"
git-identity -l

# Or as a one-shot:
& "$env:LOCALAPPDATA\Programs\git-identity\git-identity.ps1" -p work
```

Or pin it in your profile:

```powershell
Add-Content $PROFILE '. "$env:LOCALAPPDATA\Programs\git-identity\git-identity.ps1"'
```

After that, `git-identity` is a first-class command in PowerShell.

## What this installer does NOT do

- Does not register the tool as a Windows service.
- Does not add a Start Menu shortcut (Windows 10/11 does this automatically when you run `install.cmd` the first time, via the "New app installed" notification; click it to pin).
- Does not modify the system PATH (only the user PATH, no admin required).
- Does not bundle git. You must have Git for Windows installed.

## Performance

| Metric                    | Value           |
|---------------------------|-----------------|
| File size                 | 1.81 MB         |
| Cold start                | ~100-200 ms     |
| Peak RSS during execution | ~2.5 MB         |
| Resident after exit       | 0 MB            |
| Allocations in hot path   | 0               |

## Verification after install

```cmd
:: Open a NEW terminal (so the new PATH is picked up):
git identity --help
git identity -l
git identity -p work
```

If `git identity` says `'identity' is not a git command`, your git alias is not registered. Run this manually:

```cmd
git config --global alias.identity "!C:\Users\YOURNAME\AppData\Local\Programs\git-identity\git-identity.exe"
```

(Substitute the actual path.)
