# PowerShell build script for Windows
param(
    [string]$RID = "win-x64",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$ProjectPath = Join-Path $ProjectRoot "src\QuickCode.Cli\QuickCode.Cli.csproj"

Write-Host "Building for $RID..." -ForegroundColor Green

dotnet publish $ProjectPath `
    -c $Configuration `
    -r $RID `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:PublishTrimmed=false

Write-Host "Build completed!" -ForegroundColor Green
$OutputPath = Join-Path $ProjectRoot "src\QuickCode.Cli\bin\$Configuration\net10.0\$RID\publish"
Write-Host "Output: $OutputPath"

