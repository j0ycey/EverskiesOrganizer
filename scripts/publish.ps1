$ErrorActionPreference = "Stop"

Set-Location (Join-Path $PSScriptRoot "..")

dotnet publish .\EverskiesOrganizer.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishTrimmed=false `
    -o .\publish\win-x64

Write-Host ""
Write-Host "Published: .\publish\win-x64\EverskiesOrganizer.exe"
