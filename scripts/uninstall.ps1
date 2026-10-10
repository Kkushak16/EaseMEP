<#
.SYNOPSIS
    Smart MEP Router — Automated Uninstaller Script (Phase 10)
.DESCRIPTION
    Removes the EaseMEP add-in and manifest from Autodesk Revit Addins directories.
#>

param(
    [string]$RevitVersion = "all"
)

Write-Host "Uninstalling EaseMEP (Smart MEP Router)..." -ForegroundColor Yellow

$appData = [Environment]::GetFolderPath("ApplicationData")
$versions = if ($RevitVersion -eq "all") { @("2024", "2025", "2026", "2027") } else { @($RevitVersion) }

foreach ($ver in $versions) {
    $revitAddinDir = Join-Path $appData "Autodesk\Revit\Addins\$ver"
    $targetBundleDir = Join-Path $revitAddinDir "SmartMepRouter"
    $targetManifestPath = Join-Path $revitAddinDir "SmartMepRouter.addin"

    if (Test-Path $targetManifestPath) {
        Remove-Item -Path $targetManifestPath -Force
        Write-Host "Removed manifest: $targetManifestPath" -ForegroundColor Green
    }

    if (Test-Path $targetBundleDir) {
        Remove-Item -Path $targetBundleDir -Recurse -Force
        Write-Host "Removed binaries: $targetBundleDir" -ForegroundColor Green
    }
}

Write-Host "Uninstallation completed cleanly." -ForegroundColor Cyan
