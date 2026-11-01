param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$snapshotRoot = Join-Path $PSScriptRoot 'before'
$entries = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'baseline-manifest.json') -Raw | ConvertFrom-Json
$restoreFiles = foreach ($entry in $entries) {
    $target = [IO.Path]::GetFullPath((Join-Path $projectRoot $entry.Path))
    if (-not $target.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Target outside project.' }
    $source = Join-Path $snapshotRoot $entry.Path
    if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $entry.SHA256) { throw "Snapshot hash mismatch: $($entry.Path)" }
    [pscustomobject]@{ Source=$source; Target=$target; Relative=$entry.Path }
}
$restoreFiles | Select-Object Relative
if (-not $Apply) { Write-Output 'Verified baseline. Preview only. Stop Unity Play before running with -Apply.'; return }
$currentBackup = Join-Path $PSScriptRoot ('pre-restore-' + [Guid]::NewGuid().ToString('N'))
foreach ($file in $restoreFiles) {
    $save = Join-Path $currentBackup $file.Relative
    New-Item -ItemType Directory -Path (Split-Path $save -Parent) -Force | Out-Null
    if (Test-Path -LiteralPath $file.Target) { Copy-Item -LiteralPath $file.Target -Destination $save }
    Copy-Item -LiteralPath $file.Source -Destination $file.Target -Force
    if (Test-Path -LiteralPath ($file.Source + '.meta')) {
        if (Test-Path -LiteralPath ($file.Target + '.meta')) { Copy-Item -LiteralPath ($file.Target + '.meta') -Destination ($save + '.meta') }
        Copy-Item -LiteralPath ($file.Source + '.meta') -Destination ($file.Target + '.meta') -Force
    }
}
Write-Output 'Baseline restored. Refresh Unity and run tests. Current files were backed up alongside this script.'
