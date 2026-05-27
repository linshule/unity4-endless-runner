param(
    [string]$DotPath = "C:\Windows\Temp\_graphviz\dot.exe",
    [string]$OutDir = "D:\unitypk\work\docs\diagrams",
    [string]$DotFile
)

$env:Path += ";C:\Program Files\MySQL\MySQL Workbench 8.0"

$baseName = [System.IO.Path]::GetFileNameWithoutExtension($DotFile)
$svgFile = "$env:TEMP\$baseName.svg"
$pngFile = "$OutDir\$baseName.png"

& $DotPath -Tsvg $DotFile -o $svgFile 2>$null

python -c @"
from cairosvg import svg2png
svg2png(url=r'$svgFile', write_to=r'$pngFile')
"@

if (Test-Path $pngFile) {
    Write-Host "OK: $baseName.png - $([math]::Round((Get-Item $pngFile).Length/1KB)) KB"
    Remove-Item $svgFile -Force -ErrorAction SilentlyContinue
} else {
    Write-Host "FAILED: $baseName"
}
