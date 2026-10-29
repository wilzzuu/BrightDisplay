$ErrorActionPreference = 'Stop'

$projectPath = Join-Path $PSScriptRoot 'BrightDisplay.csproj'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'The .NET SDK is required to build BrightDisplay. Install it from https://dotnet.microsoft.com/download.'
}

dotnet publish $projectPath -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$outputPath = Join-Path $PSScriptRoot 'bin\Release\net10.0-windows\win-x64\publish\BrightDisplay.exe'
Write-Host "Build succeeded: $outputPath"
