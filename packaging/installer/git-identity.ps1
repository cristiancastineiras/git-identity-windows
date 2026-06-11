# git-identity.ps1
# PowerShell shim for the git-identity tool. Source this file from
# your PowerShell profile to make `git-identity` work as a command
# in any PowerShell session:
#
#     . "$env:LOCALAPPDATA\Programs\git-identity\git-identity.ps1"
#
# Or install the shim module (see packaging/installer/install.cmd).

param(
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$Arguments
)

$exe = Join-Path $PSScriptRoot "git-identity.exe"
if (-not (Test-Path $exe)) {
    Write-Error "git-identity.exe not found at $exe. Run install.cmd first."
    exit 127
}

# Forward all arguments as-is, with proper argument parsing.
& $exe @Arguments
exit $LASTEXITCODE
