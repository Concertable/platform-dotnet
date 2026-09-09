"""Prove Concertable.Build delivers the build law through a PackageReference alone.

Each consumer below is generated outside the repository and inherits nothing from it, so a pass
means the law arrived in the package. Every case is expected to FAIL to build: a case that compiles
means the rule it exercises is not being enforced, which is the silent no-op this package exists to
end.

    python eng/verify-build-law.py [--package-dir artifacts/packages]
"""

from __future__ import annotations

import argparse
import pathlib
import re
import shutil
import subprocess
import sys
import tempfile
import zipfile

REPO_ROOT = pathlib.Path(__file__).resolve().parents[1]

NUGET_CONFIG = """<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="candidate" value="{package_dir}" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
    <packageSource key="candidate">
      <package pattern="Concertable.*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
"""

CASES = [
    {
        "name": "Consumer.Library",
        "proves": "BannedSymbols.txt (repository-wide list) is live",
        "expect": "RS0030",
        "properties": "",
        "items": '<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.3" />',
        "source": """using Microsoft.EntityFrameworkCore;

namespace Consumer.Library;

public static class BannedRepoWide
{
    public static IQueryable<T> Escape<T>(IQueryable<T> source)
        where T : class => source.IgnoreQueryFilters();
}
""",
    },
    {
        # A FrameworkReference, not one of the host packages the tier gate rejects by package id:
        # this case must prove the unit-tier SYMBOL list fires, not the package check.
        "name": "Consumer.UnitTests",
        "proves": "BannedSymbols.UnitTests.txt (unit-tier list) is live",
        "expect": "RS0030",
        "properties": "<IsTestProject>true</IsTestProject>",
        "items": '<FrameworkReference Include="Microsoft.AspNetCore.App" />',
        "source": """using Microsoft.AspNetCore.Builder;

namespace Consumer.UnitTests;

public static class BannedInUnitTier
{
    public static WebApplicationBuilder Build() => WebApplication.CreateBuilder();
}
""",
    },
    {
        "name": "Consumer.Untiered",
        "proves": "the test-tier gate fails a test project whose name states no tier",
        "expect": "Test-tier violation",
        "properties": "<IsTestProject>true</IsTestProject>",
        "items": "",
        "source": "namespace Consumer.Untiered;\n\npublic static class Placeholder\n{\n}\n",
    },
]

PROJECT = """<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    {properties}
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Concertable.Build" Version="{version}" />
    {items}
  </ItemGroup>

</Project>
"""


def packaged_version(package_dir: pathlib.Path) -> str:
    candidates = sorted(package_dir.glob("Concertable.Build.*.nupkg"))
    candidates = [c for c in candidates if not c.name.endswith(".symbols.nupkg")]
    if len(candidates) != 1:
        raise SystemExit(
            f"expected exactly one Concertable.Build nupkg in {package_dir}, found {len(candidates)}"
        )
    with zipfile.ZipFile(candidates[0]) as package:
        nuspec = next(n for n in package.namelist() if n.endswith(".nuspec"))
        text = package.read(nuspec).decode("utf-8")
    match = re.search(r"<version>([^<]+)</version>", text)
    if not match:
        raise SystemExit(f"no <version> in {candidates[0].name}")
    return match.group(1)


def build(case: dict, root: pathlib.Path, version: str) -> tuple[bool, str]:
    project_dir = root / case["name"]
    project_dir.mkdir(parents=True)
    (project_dir / f"{case['name']}.csproj").write_text(
        PROJECT.format(properties=case["properties"], items=case["items"], version=version),
        encoding="utf-8",
    )
    (project_dir / "Subject.cs").write_text(case["source"], encoding="utf-8")

    result = subprocess.run(
        ["dotnet", "build", str(project_dir / f"{case['name']}.csproj"), "--configuration", "Release"],
        capture_output=True,
        text=True,
        cwd=root,
    )
    output = result.stdout + result.stderr
    return result.returncode == 0, output


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--package-dir", default=str(REPO_ROOT / "artifacts" / "packages"))
    args = parser.parse_args()

    package_dir = pathlib.Path(args.package_dir).resolve()
    version = packaged_version(package_dir)
    print(f"Concertable.Build under test: {version}")
    print(f"candidate feed              : {package_dir}\n")

    root = pathlib.Path(tempfile.mkdtemp(prefix="concertable-build-law-"))
    failures: list[str] = []
    try:
        (root / "nuget.config").write_text(
            NUGET_CONFIG.format(package_dir=package_dir.as_posix()), encoding="utf-8"
        )
        for case in CASES:
            succeeded, output = build(case, root, version)
            if succeeded:
                failures.append(f"{case['name']}: built successfully — {case['proves']} is NOT enforced")
                verdict = "NOT ENFORCED"
            elif case["expect"] not in output:
                failures.append(
                    f"{case['name']}: failed without '{case['expect']}' — the wrong rule stopped it"
                )
                verdict = "WRONG FAILURE"
            else:
                verdict = "enforced"
            print(f"  [{verdict:^13}] {case['name']}: {case['proves']}")
    finally:
        shutil.rmtree(root, ignore_errors=True)

    print()
    if failures:
        for failure in failures:
            print(f"FAIL {failure}")
        return 1
    print(f"All {len(CASES)} build-law rules arrive through the Concertable.Build PackageReference.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
