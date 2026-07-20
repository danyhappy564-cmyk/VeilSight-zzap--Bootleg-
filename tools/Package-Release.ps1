param(
    [string]$Configuration = "Release",
    [string]$SptVersion = "4.0.13"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $projectRoot "VeilSight.csproj"
[xml]$project = Get-Content -LiteralPath $projectFile
$versionNode = $project.SelectSingleNode("/Project/PropertyGroup/VersionPrefix")
$version = if ($null -eq $versionNode) { "" } else { $versionNode.InnerText.Trim() }

if ([string]::IsNullOrWhiteSpace($version)) {
    throw "VeilSight.csproj does not define VersionPrefix."
}

$dllPath = Join-Path $projectRoot "bin\$Configuration\VeilSight.dll"
if (-not (Test-Path -LiteralPath $dllPath)) {
    throw "Build VeilSight in $Configuration mode before packaging. Missing: $dllPath"
}

$releaseDirectory = Join-Path $projectRoot "release"
[System.IO.Directory]::CreateDirectory($releaseDirectory) | Out-Null
$archivePath = Join-Path $releaseDirectory "VeilSight-$version-SPT-$SptVersion.zip"
$temporaryPath = "$archivePath.tmp"

if (Test-Path -LiteralPath $temporaryPath) {
    [System.IO.File]::Delete($temporaryPath)
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$stream = [System.IO.File]::Open(
    $temporaryPath,
    [System.IO.FileMode]::CreateNew,
    [System.IO.FileAccess]::ReadWrite,
    [System.IO.FileShare]::None)

try {
    $archive = [System.IO.Compression.ZipArchive]::new(
        $stream,
        [System.IO.Compression.ZipArchiveMode]::Create,
        $false)

    try {
        foreach ($directory in @(
            "BepInEx/",
            "BepInEx/plugins/",
            "BepInEx/plugins/VeilSight/")) {
            $entry = $archive.CreateEntry($directory)
            $entry.ExternalAttributes = 0x10
        }

        $files = [ordered]@{
            "BepInEx/plugins/VeilSight/VeilSight.dll" = $dllPath
            "README.txt" = (Join-Path $projectRoot "README.md")
            "CHANGELOG.txt" = (Join-Path $projectRoot "CHANGELOG.md")
            "LICENSE.txt" = (Join-Path $projectRoot "LICENSE")
        }

        foreach ($item in $files.GetEnumerator()) {
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $archive,
                $item.Value,
                $item.Key,
                [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally {
        $archive.Dispose()
    }
}
finally {
    $stream.Dispose()
}

$check = [System.IO.Compression.ZipFile]::OpenRead($temporaryPath)
try {
    $entryNames = @($check.Entries | ForEach-Object FullName)
    $expected = @(
        "BepInEx/",
        "BepInEx/plugins/",
        "BepInEx/plugins/VeilSight/",
        "BepInEx/plugins/VeilSight/VeilSight.dll",
        "README.txt",
        "CHANGELOG.txt",
        "LICENSE.txt")

    if ($entryNames.Count -ne $expected.Count -or
        (Compare-Object $entryNames $expected) -or
        ($entryNames | Where-Object { $_.Contains("\") })) {
        throw "Release archive layout validation failed."
    }
}
finally {
    $check.Dispose()
}

[System.IO.File]::Copy($temporaryPath, $archivePath, $true)
[System.IO.File]::Delete($temporaryPath)

$hash = Get-FileHash -LiteralPath $archivePath -Algorithm SHA256
Write-Output "Created $archivePath"
Write-Output "SHA256 $($hash.Hash)"
