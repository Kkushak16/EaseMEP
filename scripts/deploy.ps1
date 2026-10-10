# Smart MEP Router - Automated Installation Script
param(
    [string]$RevitVersion = '2027',
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

Write-Host '--------------------------------------------------------------------------------'
Write-Host '       EaseMEP (Smart MEP Router) - Automated Add-in Deployment                 '
Write-Host '--------------------------------------------------------------------------------'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionRoot = Split-Path -Parent $scriptDir
$addinManifestSource = Join-Path $solutionRoot 'addin\SmartMepRouter.addin'

if (-not (Test-Path $addinManifestSource)) {
    throw ('Could not find add-in manifest at: ' + $addinManifestSource)
}

$revitProcesses = Get-Process -Name 'Revit' -ErrorAction SilentlyContinue
if ($revitProcesses) {
    Write-Warning 'Revit is currently running. Existing assembly DLLs may be locked.'
}

$versionsToDeploy = if ($RevitVersion -eq 'all') { @('2024', '2025', '2026', '2027') } else { @($RevitVersion) }

Write-Host ('Building solution (' + $Configuration + ')...')
$dotnetPath = if (Test-Path 'D:\dotnet\dotnet.exe') { 'D:\dotnet\dotnet.exe' } else { 'dotnet' }
$slnPath = Join-Path $solutionRoot 'SmartMepRouter.sln'

& $dotnetPath build $slnPath -c $Configuration --nologo -v q
if ($LASTEXITCODE -ne 0) {
    throw 'Build failed.'
}
Write-Host 'Build succeeded.'

$appData = [Environment]::GetFolderPath('ApplicationData')

foreach ($ver in $versionsToDeploy) {
    $revitAddinDir = Join-Path $appData ('Autodesk\Revit\Addins\' + $ver)
    $targetBundleDir = Join-Path $revitAddinDir 'SmartMepRouter'
    $targetManifestPath = Join-Path $revitAddinDir 'SmartMepRouter.addin'

    $targetFramework = if ([int]$ver -le 2024) { 'net48' } else { 'net8.0-windows' }
    $sourceBinDir = Join-Path $solutionRoot ('src\SmartMepRouter.Revit\bin\' + $Configuration + '\' + $targetFramework)

    if (-not (Test-Path $sourceBinDir)) {
        Write-Warning ('Binary directory not found for ' + $ver + ' (' + $sourceBinDir + '). Skipping.')
        continue
    }

    Write-Host ('Deploying to Revit ' + $ver + ' (' + $targetFramework + ')...')

    if (-not (Test-Path $targetBundleDir)) {
        New-Item -ItemType Directory -Path $targetBundleDir -Force | Out-Null
    }

    $allFiles = Get-ChildItem -Path $sourceBinDir -Include '*.dll','*.pdb' -Recurse -File
    foreach ($file in $allFiles) {
        $destPath = Join-Path $targetBundleDir $file.Name
        Copy-Item -Path $file.FullName -Destination $destPath -Force
    }

    Copy-Item -Path $addinManifestSource -Destination $targetManifestPath -Force

    Write-Host ('  [OK] Installed ' + $ver + ' manifest to: ' + $targetManifestPath)
    Write-Host ('  [OK] Deployed binaries to: ' + $targetBundleDir)
}

Write-Host '--------------------------------------------------------------------------------'
Write-Host 'Deployment completed successfully! Launch Autodesk Revit to use EaseMEP.'
Write-Host '--------------------------------------------------------------------------------'
