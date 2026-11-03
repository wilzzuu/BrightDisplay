$ErrorActionPreference = 'Stop'

$projectPath = Join-Path $PSScriptRoot 'BrightDisplay.csproj'
$outputPath = Join-Path $PSScriptRoot 'dist'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'The .NET SDK is required to build BrightDisplay. Install it from https://dotnet.microsoft.com/download.'
}

dotnet publish $projectPath -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true --output $outputPath
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$executablePath = Join-Path $outputPath 'BrightDisplay.exe'
if (-not (Test-Path $executablePath -PathType Leaf)) {
    throw "dotnet publish completed without producing the expected executable: $executablePath"
}

Write-Host "Build succeeded: $executablePath"
