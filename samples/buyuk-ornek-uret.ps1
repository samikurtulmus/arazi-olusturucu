# 100.000 satırlık performans testi dosyası üretir (No <TAB> Y <TAB> X).
# Kullanım:  .\buyuk-ornek-uret.ps1            → buyuk-ornek-100k.txt
#            .\buyuk-ornek-uret.ps1 -RowCount 250000
param(
    [int]$RowCount = 100000,
    [string]$OutFile = "buyuk-ornek-100k.txt"
)

$inv = [System.Globalization.CultureInfo]::InvariantCulture
$sb = New-Object System.Text.StringBuilder ($RowCount * 32)
$rand = New-Object System.Random 42
$angleStep = 2 * [Math]::PI / $RowCount

for ($i = 0; $i -lt $RowCount; $i++) {
    $angle = $i * $angleStep
    $r = 5000 + 500 * [Math]::Sin(12 * $angle) + $rand.NextDouble() * 20
    $y = 456800 + $r * [Math]::Cos($angle)     # Sağa (easting)
    $x = 4423400 + $r * [Math]::Sin($angle)    # Yukarı (northing)
    [void]$sb.Append($i + 1).Append("`t")
    [void]$sb.Append($y.ToString("F2", $inv)).Append("`t")
    [void]$sb.AppendLine($x.ToString("F2", $inv))
}

$path = Join-Path $PSScriptRoot $OutFile
[System.IO.File]::WriteAllText($path, $sb.ToString())
Write-Host "$RowCount satır yazıldı: $path"
