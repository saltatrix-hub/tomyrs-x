[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$artifactsDir = Join-Path $projectRoot 'artifacts'
$appPublishDir = Join-Path $artifactsDir 'app'
$setupPublishDir = Join-Path $artifactsDir 'setup'
$payloadPath = Join-Path $projectRoot 'src\TomyrsX.Setup\Payload.zip'

$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
$localDotnet = Join-Path $projectRoot '.dotnet\dotnet.exe'
$systemDotnet = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
if ($dotnetCommand) {
    $dotnetPath = $dotnetCommand.Source
} elseif (Test-Path -LiteralPath $localDotnet) {
    $dotnetPath = $localDotnet
} elseif (Test-Path -LiteralPath $systemDotnet) {
    $dotnetPath = $systemDotnet
} else {
    throw '.NET 8 SDK bulunamadı. https://dotnet.microsoft.com/download/dotnet/8.0 adresinden yükleyin.'
}

foreach ($path in @($appPublishDir, $setupPublishDir)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }
    New-Item -ItemType Directory -Path $path | Out-Null
}

& $dotnetPath publish (Join-Path $projectRoot 'src\TomyrsX.App\TomyrsX.App.csproj') `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishTrimmed=false `
    -o $appPublishDir

if (Test-Path -LiteralPath $payloadPath) {
    Remove-Item -LiteralPath $payloadPath -Force
}
Compress-Archive -Path (Join-Path $appPublishDir '*') -DestinationPath $payloadPath -CompressionLevel Optimal

& $dotnetPath publish (Join-Path $projectRoot 'src\TomyrsX.Setup\TomyrsX.Setup.csproj') `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishTrimmed=false `
    -o $setupPublishDir

$setupExe = Join-Path $setupPublishDir 'TomyrsX-Setup.exe'
if (-not (Test-Path -LiteralPath $setupExe)) {
    throw 'TomyrsX-Setup.exe üretilemedi.'
}

Copy-Item -LiteralPath $setupExe -Destination (Join-Path $artifactsDir 'TomyrsX-Setup.exe') -Force
Write-Host "Hazır: $artifactsDir\TomyrsX-Setup.exe"
