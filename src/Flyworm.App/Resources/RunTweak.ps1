param(
    [Parameter(Mandatory = $true)]
    [string]$ScriptPath,
    [string]$Choice = "1"
)

$ErrorActionPreference = "Continue"
$ProgressPreference = "SilentlyContinue"

$script:FlywormReadCount = 0
function Read-Host {
    param([Parameter(ValueFromRemainingArguments = $true)] $Ignored)
    $script:FlywormReadCount++
    if ($script:FlywormReadCount -eq 1) {
        return $Choice
    }
    return "1"
}

function Pause {
    param([Parameter(ValueFromRemainingArguments = $true)] $Ignored)
}

if (-not (Test-Path -LiteralPath $ScriptPath)) {
    throw "Script not found: $ScriptPath"
}

$content = Get-Content -LiteralPath $ScriptPath -Raw -Encoding UTF8
$content = [regex]::Replace(
    $content,
    '(?s)If\s*\(!\(\[Security\.Principal\.WindowsPrincipal\].*?Start-Process PowerShell\.exe.*?Exit\s*\}',
    "# Flyworm: already elevated"
)

$script:PSScriptRoot = [System.IO.Path]::GetDirectoryName($ScriptPath)
$script:PSCommandPath = $ScriptPath
$script:MyInvocation = [pscustomobject]@{
    MyCommand = [pscustomobject]@{ Definition = $ScriptPath }
}

Set-Location -LiteralPath $script:PSScriptRoot
$execution = [scriptblock]::Create($content)
& $execution
