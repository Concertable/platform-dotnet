# Concertable platform (.NET)

The domain-neutral .NET platform every Concertable service builds on, and the sole publisher of its
package ids. The extraction preserved authorship and history with `git filter-repo`.

## Layout

```text
src/Concertable.Build/            build law, packaged for every other repository
src/Concertable.Shared/           Kernel, Contracts, Grpc, Shared.Api, capabilities, Seed, test primitives
src/Concertable.Messaging/        Inbox/Outbox contracts, domain, infrastructure, Azure Service Bus
src/Concertable.DataAccess/       repository capability hierarchy
src/Concertable.ServiceDefaults/  service host defaults
src/Concertable.AppHost.Shared/   product-neutral Aspire resource and topology primitives
src/Concertable.Frontend.Hosting/ SPA hosting primitives
BannedSymbols.txt                 repository-wide banned-symbol list
BannedSymbols.UnitTests.txt       unit-tier banned-symbol list
TestConventions.targets           test-tier gate
eng/                              published baseline, version guard, build-law proof
```

`Concertable.Build` packs the three build-law files at the root into `build/`, so a consuming
repository gets the test-tier gate and both banned-symbol lists from one `PackageReference` rather
than a copy that drifts. `python eng/verify-build-law.py` proves it against generated consumers that
inherit nothing from this repository.

## Building

Restoring the four service `*.Hosting` packages needs a feed credential:

```sh
export GITHUB_PACKAGES_TOKEN=<a PAT with read:packages>
dotnet restore Concertable.Platform.slnx
dotnet build Concertable.Platform.slnx --configuration Release
dotnet test Concertable.Platform.slnx --configuration Release
```

## PostGIS data retention

`AddPostgresContainer().WithPostGis()` uses PostGIS 17 and mounts the checkout's named volume at
`/var/lib/postgresql/data`. `WithPostGis()` also retargets an existing PostgreSQL 18 parent-directory
mount while preserving its source and access mode. Apply it after configuring the PostgreSQL volume.

For an existing container, inspect both `SHOW data_directory` and Docker's mounts before recreating it.
An anonymous volume at the effective data directory can contain the database even when a named volume
is mounted at its parent. Keep that container and both volumes until a backup has been restored and
verified against the expected database contents; changing the mount does not transfer existing data.

## Versioning

Repository-local MinVer on tags prefixed `v`, floored at `0.2`. The preceding package train published
these ids as `0.1.0-alpha.0.<height>`, so this train starts above that mark. `eng/published-baseline.json`
records the mark per id; `python eng/check-version.py` refuses to publish a version that does not advance
it or that the feed already has.
