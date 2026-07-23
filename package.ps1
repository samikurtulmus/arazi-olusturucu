# Dagitim paketi olusturur: dist\YapiLabCadTools.zip
# Icinde derlenmis DLL dahil komple bundle vardir. Baska bir kullanicinin kurulumu:
#   1. ZIP'i acin.
#   2. YapiLabCadTools.bundle klasorunu %AppData%\Autodesk\ApplicationPlugins altina kopyalayin.
#   3. AutoCAD'i yeniden baslatin - YAPILAB / YL komutlari hazir.
# (AutoCAD 2025-2027 disinda hicbir sey gerekmez; .NET calisma ortami AutoCAD ile gelir.)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host 'Release derleniyor...'
dotnet build (Join-Path $repoRoot 'src\YapiLabCadTools\YapiLabCadTools.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Derleme basarisiz - paketleme iptal edildi.' }

$stage = Join-Path $env:TEMP 'yapilab-package'
$bundle = Join-Path $stage 'YapiLabCadTools.bundle'
if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
New-Item -ItemType Directory -Force (Join-Path $bundle 'Contents') | Out-Null

Copy-Item (Join-Path $repoRoot 'bundle\YapiLabCadTools.bundle\PackageContents.xml') $bundle
Copy-Item (Join-Path $repoRoot 'src\YapiLabCadTools\bin\Release\YapiLabCadTools.dll') (Join-Path $bundle 'Contents')

$dist = Join-Path $repoRoot 'dist'
New-Item -ItemType Directory -Force $dist | Out-Null
$zip = Join-Path $dist 'YapiLabCadTools.zip'
if (Test-Path $zip) { Remove-Item -Force $zip }
Compress-Archive -Path $bundle -DestinationPath $zip

Remove-Item -Recurse -Force $stage
Write-Host ''
Write-Host "Paket hazir: $zip"
Write-Host 'Kurulum: ZIP''i acip YapiLabCadTools.bundle klasorunu'
Write-Host '%AppData%\Autodesk\ApplicationPlugins altina kopyalamak yeterli.'
