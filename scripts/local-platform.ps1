#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'

$invocationArguments = @($args)
$Command = if ($invocationArguments.Count -gt 0) { [string]$invocationArguments[0] } else { $null }
$Target = if ($invocationArguments.Count -gt 1) { [string]$invocationArguments[1] } else { $null }
$Rest = if ($invocationArguments.Count -gt 2) { [string[]]$invocationArguments[2..($invocationArguments.Count - 1)] } else { @() }

$usage = @'
Local platform inner loop - verify a platform change against a consumer with no publish.

  ./scripts/local-platform.ps1 pack [--force]
  ./scripts/local-platform.ps1 restore <project-or-solution> [dotnet args...]
  ./scripts/local-platform.ps1 build   <project-or-solution> [dotnet args...]
  ./scripts/local-platform.ps1 test    <project-or-solution> [dotnet args...]
  ./scripts/local-platform.ps1 clean

Run from anywhere; <project-or-solution> resolves against your current directory, so a consumer
in a sibling repository is addressed directly:

  pwsh ./scripts/local-platform.ps1 pack
  pwsh ./scripts/local-platform.ps1 test ../b2b/api/src/Modules/Tenant/Tests/Concertable.B2B.Tenant.IntegrationTests/Concertable.B2B.Tenant.IntegrationTests.csproj

GITHUB_PACKAGES_TOKEN (a PAT with read:packages) is still required: a consumer resolves its
cross-service packages from the org feed, and only the platform train comes from the local one.
'@

if ($Command -notin @('pack', 'restore', 'build', 'test', 'clean')) {
    Write-Host $usage
    throw "Command must be one of: pack, restore, build, test, clean."
}

$repoRoot = Split-Path $PSScriptRoot -Parent
$solution = Join-Path $repoRoot 'Concertable.Platform.Packages.slnx'
$feedRoot = Join-Path $repoRoot 'artifacts/local-platform'
$packagesRoot = Join-Path $feedRoot 'packages'
$configPath = Join-Path $feedRoot 'nuget.config'
$versionPath = Join-Path $feedRoot 'version.txt'
$globalPackagesRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $HOME '.nuget/packages' }

function Invoke-DotNet([string[]]$Arguments) {
    Write-Host "dotnet $($Arguments -join ' ')"
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

function Get-PackableProjectName {
    $document = [xml](Get-Content -Raw -LiteralPath $solution)
    return @($document.SelectNodes('//Project') | ForEach-Object {
        [System.IO.Path]::GetFileNameWithoutExtension($_.GetAttribute('Path'))
    })
}

function Get-LocalPlatformVersion {
    if (-not (Test-Path -LiteralPath $versionPath)) {
        throw "No local platform is packed. Run './scripts/local-platform.ps1 pack' first."
    }

    return (Get-Content -Raw -LiteralPath $versionPath).Trim()
}

function Remove-CachedLocalPlatform {
    if (-not (Test-Path -LiteralPath $globalPackagesRoot)) { return }

    $removed = 0
    foreach ($package in Get-ChildItem -LiteralPath $globalPackagesRoot -Directory -Filter 'concertable.*') {
        foreach ($version in Get-ChildItem -LiteralPath $package.FullName -Directory) {
            if ($version.Name -notlike '*-local.*') { continue }
            try {
                Remove-Item -LiteralPath $version.FullName -Recurse -Force
                $removed++
            }
            catch {
                Write-Warning "Could not remove $($version.FullName): $($_.Exception.Message)"
            }
        }
    }

    Write-Host "Removed $removed cached local-platform version(s) from $globalPackagesRoot."
}

function Write-NuGetConfig {
    $escaped = [System.Security.SecurityElement]::Escape((Resolve-Path -LiteralPath $packagesRoot).Path)
    $content = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="local-platform" value="$escaped" />
    <add key="github" value="https://nuget.pkg.github.com/Concertable/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
    <packageSource key="local-platform">
      <package pattern="Concertable.*" />
    </packageSource>
    <packageSource key="github">
      <package pattern="Concertable.*" />
    </packageSource>
  </packageSourceMapping>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="Concertable" />
      <add key="ClearTextPassword" value="%GITHUB_PACKAGES_TOKEN%" />
    </github>
  </packageSourceCredentials>
</configuration>
"@
    [System.IO.File]::WriteAllText($configPath, $content, [System.Text.UTF8Encoding]::new($false))
}

function Invoke-Pack {
    $expected = Get-PackableProjectName
    if ($expected.Count -eq 0) {
        throw "Parsed no projects out of $solution."
    }

    # A fresh version every pack. A folder feed is extracted into the global packages folder on first
    # restore, so repacking changed content at a version already cached there is silently ignored.
    # 9999 keeps the train above every published version: packages from other repositories depend on
    # platform ids at their published versions, and a lower local pin is an NU1605 downgrade.
    $version = "9999.0.0-local.$([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds())"

    Remove-CachedLocalPlatform
    if (Test-Path -LiteralPath $packagesRoot) {
        Remove-Item -LiteralPath $packagesRoot -Recurse -Force
    }
    New-Item -ItemType Directory -Path $packagesRoot -Force | Out-Null
    Write-NuGetConfig

    Invoke-DotNet @(
        'pack', $solution,
        '--configuration', 'Release',
        '--output', $packagesRoot,
        "-p:MinVerVersionOverride=$version"
    )

    $produced = @(Get-ChildItem -LiteralPath $packagesRoot -Filter '*.nupkg' |
        ForEach-Object { $_.Name -replace "\.$([regex]::Escape($version))\.nupkg$", '' })
    $missing = @($expected | Where-Object { $produced -notcontains $_ })
    $unexpected = @($produced | Where-Object { $expected -notcontains $_ })

    if ($missing.Count -gt 0 -or $unexpected.Count -gt 0) {
        $detail = @()
        if ($missing.Count -gt 0) { $detail += "missing: $(($missing | Sort-Object) -join ', ')" }
        if ($unexpected.Count -gt 0) { $detail += "unexpected: $(($unexpected | Sort-Object) -join ', ')" }
        throw "The packed set does not match $([System.IO.Path]::GetFileName($solution)) ($($expected.Count) projects, $($produced.Count) packages); $($detail -join '; '). A consumer takes the whole train or none, so a partial set cannot be used."
    }

    [System.IO.File]::WriteAllText($versionPath, $version, [System.Text.UTF8Encoding]::new($false))
    Write-Host "Packed $($produced.Count) packages at $version into $packagesRoot."
}

function Assert-ResolvedPlatformVersion([string]$Project, [string]$Version) {
    if ([System.IO.Path]::GetExtension($Project) -notmatch '^\.(cs|fs|vb)proj$') {
        Write-Host "Skipped the resolved-version check: $Project is not a single project."
        return
    }

    $assetsPath = Join-Path (Split-Path (Resolve-Path -LiteralPath $Project).Path -Parent) 'obj/project.assets.json'
    if (-not (Test-Path -LiteralPath $assetsPath)) {
        throw "Expected restored assets at $assetsPath."
    }

    $platformIds = Get-PackableProjectName
    $assets = Get-Content -Raw -LiteralPath $assetsPath | ConvertFrom-Json
    $platform = @($assets.libraries.PSObject.Properties.Name | Where-Object { $platformIds -contains $_.Split('/')[0] })
    $local = @($platform | Where-Object { $_.Split('/')[1] -eq $Version })
    $feed = @($platform | Where-Object { $_.Split('/')[1] -ne $Version })

    if ($local.Count -eq 0) {
        throw "No platform package resolved at $Version in $assetsPath. The local feed did not take."
    }

    Write-Host "Verified $($local.Count) platform package(s) resolved at $Version."
    if ($feed.Count -gt 0) {
        # A platform id the consumer does not pin arrives transitively at whatever version its supplier
        # asked for. Legitimate, but it is the one place a mixed train can hide.
        Write-Host "Still on a published version (not pinned by this consumer): $(($feed | Sort-Object) -join ', ')."
    }
}

switch ($Command) {
    'pack' {
        Invoke-Pack
    }
    'clean' {
        Remove-CachedLocalPlatform
        if (Test-Path -LiteralPath $feedRoot) {
            Remove-Item -LiteralPath $feedRoot -Recurse -Force
        }
        Write-Host "Removed $feedRoot."
    }
    default {
        if ([string]::IsNullOrWhiteSpace($Target)) { throw "$Command requires a project or solution target." }
        $version = Get-LocalPlatformVersion
        Write-NuGetConfig

        Invoke-DotNet @(
            'restore', $Target,
            '--configfile', $configPath,
            "-p:ConcertableDotNetPlatformVersion=$version"
        )
        Assert-ResolvedPlatformVersion $Target $version

        if ($Command -ne 'restore') {
            Invoke-DotNet (@(
                $Command, $Target,
                '--no-restore',
                "-p:ConcertableDotNetPlatformVersion=$version"
            ) + $Rest)
        }
    }
}
