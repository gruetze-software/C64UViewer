[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^[A-Za-z0-9.-]{3,50}$')][string]$PackageName,
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$Publisher,
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$PublisherDisplayName
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$project = Join-Path $root 'C64UViewer.csproj'
$version = dotnet msbuild $project -nologo -getProperty:Version
if ($LASTEXITCODE -ne 0 -or $version -notmatch '^\d+\.\d+\.\d+$') {
    throw 'Expected a three-component project version.'
}

$sdkBin = Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Windows Kits/10/bin'
$makeAppx = Get-ChildItem $sdkBin -Directory |
    Where-Object Name -Match '^\d+\.\d+\.\d+\.\d+$' |
    Sort-Object { [version]$_.Name } -Descending |
    ForEach-Object { Join-Path $_.FullName 'x64/makeappx.exe' } |
    Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
    Select-Object -First 1
if (!$makeAppx) { throw 'Install the Windows SDK with MakeAppx.exe.' }

$output = New-Item -ItemType Directory -Path (Join-Path $root 'artifacts/windows-store') -Force
$package = Join-Path $output.FullName "c64uviewer_$($version)_windows_x64.msix"
$layout = Join-Path $output.FullName ([guid]::NewGuid().ToString())
$app = Join-Path $layout 'C64UViewer'

try {
    dotnet publish $project -c Release -r win-x64 --self-contained true -p:WindowsStoreBuild=true -o $app
    if ($LASTEXITCODE -ne 0) { throw 'Windows publish failed.' }
    foreach ($file in 'c64uviewer.exe', 'SDL2.dll') {
        if (!(Test-Path -LiteralPath (Join-Path $app $file) -PathType Leaf)) {
            throw "Missing Windows runtime file: $file"
        }
    }

    $assets = New-Item -ItemType Directory -Path (Join-Path $layout 'Assets')
    Add-Type -AssemblyName System.Drawing
    $icon = [Drawing.Image]::FromFile((Join-Path $root 'Assets/icon.png'))
    try {
        foreach ($size in 44, 50, 150) {
            $image = [Drawing.Bitmap]::new($size, $size)
            try {
                $graphics = [Drawing.Graphics]::FromImage($image)
                try {
                    $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                    $graphics.DrawImage($icon, 0, 0, $size, $size)
                } finally {
                    $graphics.Dispose()
                }
                $image.Save((Join-Path $assets.FullName "Logo$size.png"), [Drawing.Imaging.ImageFormat]::Png)
            } finally {
                $image.Dispose()
            }
        }
    } finally {
        $icon.Dispose()
    }

    [xml]$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'AppxManifest.xml') -Raw
    $manifest.Package.Identity.SetAttribute('Name', $PackageName)
    $manifest.Package.Identity.SetAttribute('Publisher', $Publisher)
    $manifest.Package.Identity.SetAttribute('Version', "$version.0")
    $manifest.Package.Properties.PublisherDisplayName = $PublisherDisplayName
    $manifest.Save((Join-Path $layout 'AppxManifest.xml'))

    & $makeAppx pack /d $layout /p $package /o
    if ($LASTEXITCODE -ne 0) { throw 'MakeAppx failed.' }
    Write-Host "Store package: $package"
} finally {
    if (Test-Path -LiteralPath $layout) {
        Remove-Item -LiteralPath $layout -Recurse -Force
    }
}