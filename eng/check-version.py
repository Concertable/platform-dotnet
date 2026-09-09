"""Refuse to publish a version that already exists, or that does not advance the recorded baseline.

The monorepo still publishes these package ids while checkpoint 7B is outstanding, so this
repository's train has to clear that train's high-water mark rather than merely be new here. A
duplicate version is not a no-op: `dotnet nuget push --skip-duplicate` accepts it silently and
leaves the stale binaries on the feed.

    python eng/check-version.py [--package-dir artifacts/packages]

Needs GITHUB_TOKEN (read:packages) to see what is already published; without one it checks the
baseline only and says so.
"""

from __future__ import annotations

import argparse
import json
import os
import pathlib
import re
import sys
import urllib.error
import urllib.request
import zipfile

REPO_ROOT = pathlib.Path(__file__).resolve().parents[1]
BASELINE = REPO_ROOT / "eng" / "published-baseline.json"
ORG = "Concertable"


def parse(version: str) -> tuple:
    """SemVer 2.0 precedence key. Release sorts above any prerelease of the same core."""
    match = re.fullmatch(r"(\d+)\.(\d+)\.(\d+)(?:-([0-9A-Za-z.-]+))?(?:\+[0-9A-Za-z.-]+)?", version)
    if not match:
        raise SystemExit(f"not a SemVer 2.0 version: {version}")
    core = tuple(int(part) for part in match.group(1, 2, 3))
    prerelease = match.group(4)
    if prerelease is None:
        return (core, 1, ())
    identifiers = []
    for item in prerelease.split("."):
        if item.isdigit():
            identifiers.append((0, int(item), ""))
        else:
            identifiers.append((1, 0, item))
    return (core, 0, tuple(identifiers))


def published(package_id: str, token: str | None) -> set[str]:
    if not token:
        return set()
    versions: set[str] = set()
    page = 1
    while True:
        url = f"https://api.github.com/orgs/{ORG}/packages/nuget/{package_id}/versions?per_page=100&page={page}"
        request = urllib.request.Request(
            url,
            headers={
                "Authorization": f"Bearer {token}",
                "Accept": "application/vnd.github+json",
                "X-GitHub-Api-Version": "2022-11-28",
            },
        )
        try:
            with urllib.request.urlopen(request) as response:
                batch = json.load(response)
        except urllib.error.HTTPError as error:
            if error.code == 404:
                return versions
            raise
        if not batch:
            return versions
        versions.update(item["name"] for item in batch)
        page += 1


def packed(package_dir: pathlib.Path) -> list[tuple[str, str]]:
    found = []
    for path in sorted(package_dir.glob("*.nupkg")):
        if path.name.endswith(".symbols.nupkg"):
            continue
        with zipfile.ZipFile(path) as package:
            nuspec = next(name for name in package.namelist() if name.endswith(".nuspec"))
            text = package.read(nuspec).decode("utf-8")
        package_id = re.search(r"<id>([^<]+)</id>", text).group(1)
        version = re.search(r"<version>([^<]+)</version>", text).group(1)
        found.append((package_id, version))
    return found


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--package-dir", default=str(REPO_ROOT / "artifacts" / "packages"))
    args = parser.parse_args()

    baseline = json.loads(BASELINE.read_text(encoding="utf-8"))
    mark = baseline["trainHighWaterMark"]
    bootstrap = baseline["bootstrapVersion"]
    token = os.environ.get("GITHUB_TOKEN") or os.environ.get("GITHUB_PACKAGES_TOKEN")

    package_dir = pathlib.Path(args.package_dir).resolve()
    candidates = packed(package_dir)
    if not candidates:
        raise SystemExit(f"no packages in {package_dir} — refusing to pass")

    print(f"recorded train high-water mark : {mark}")
    print(f"recorded bootstrap version     : {bootstrap}")
    print(f"packages to publish            : {len(candidates)}")
    if not token:
        print("WARNING: no GITHUB_TOKEN — checking the recorded baseline only, not the live feed")
    print()

    errors: list[str] = []
    for package_id, version in candidates:
        problems = []
        if parse(version) <= parse(mark):
            problems.append(f"does not advance the train high-water mark {mark}")
        if parse(version) < parse(bootstrap):
            problems.append(f"is below the bootstrap version {bootstrap}")
        recorded = baseline["packageHighWaterMarks"].get(package_id)
        if recorded and parse(version) <= parse(recorded):
            problems.append(f"does not advance this id's recorded mark {recorded}")
        if version in published(package_id, token):
            problems.append("is ALREADY PUBLISHED — pushing it would leave stale binaries on the feed")
        if problems:
            errors.append(f"{package_id} {version}: " + "; ".join(problems))
        print(f"  {'FAIL' if problems else 'ok  '}  {package_id} {version}")

    print()
    if errors:
        for error in errors:
            print(f"FAIL {error}")
        return 1
    print(f"All {len(candidates)} versions are new and advance the baseline.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
