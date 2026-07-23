# YapiLab CAD Tools kurulumu.
# Release derlemesini yapar ve eklentiyi Autodesk otomatik yukleme klasorune
# (%AppData%\Autodesk\ApplicationPlugins) kopyalar. Bir sonraki AutoCAD acilisindan
# itibaren YAPILAB / YL komutlari NETLOAD gerektirmeden hazir olur.
#
# Kullanim:  powershell -ExecutionPolicy Bypass -File install.ps1
# Kaldirmak icin: ApplicationPlugins altindaki YapiLabCadTools.bundle klasorunu silin.

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host 'YapiLab CAD Tools derleniyor (Release)...'
dotnet build (Join-Path $repoRoot 'src\YapiLabCadTools\YapiLabCadTools.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Derleme basarisiz - kurulum iptal edildi.' }

$bundleTarget = Join-Path $env:APPDATA 'Autodesk\ApplicationPlugins\YapiLabCadTools.bundle'
$contents = Join-Path $bundleTarget 'Contents'
New-Item -ItemType Directory -Force $contents | Out-Null

Copy-Item (Join-Path $repoRoot 'bundle\YapiLabCadTools.bundle\PackageContents.xml') $bundleTarget -Force
Copy-Item (Join-Path $repoRoot 'src\YapiLabCadTools\bin\Release\YapiLabCadTools.dll') $contents -Force

Write-Host ''
Write-Host "Kurulum tamamlandi: $bundleTarget"
Write-Host 'AutoCAD kapaliysa bir sonraki acilista, acik ise yeniden baslatildiginda'
Write-Host 'eklenti otomatik yuklenecek. Komutlar: YAPILAB veya YL'
