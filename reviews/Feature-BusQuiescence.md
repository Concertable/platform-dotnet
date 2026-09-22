# Code review — Feature/BusQuiescence

> **This file is a work order, not a discussion.** If you're handed this file, fix the open `[ ]`
> findings directly and report what changed. Tick each `[x]` as you land it. Pause only for a genuinely
> irreversible or ambiguous finding: record its durable disposition, take the safe path, and keep going.

**Review status:** `complete`
**Reviewed up to commit:** `de012a6317df1627444aa6afa7f7d0e2bd987c4f`  `(2026-09-22)`
**Judgment:** `approved`

## Review pass — 2026-09-22 — native/general

**Candidate base:** `fef781d71ab95785491a4e4167cb95fae5af6b54`
**Candidate head:** `de012a6317df1627444aa6afa7f7d0e2bd987c4f`
**Candidate branch:** `Feature/BusQuiescence`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:72d6930cf6f6b5261a6405d19dd1a545b010546589effc891f9726c9177ed472` `(5 paths)`
**Work-order path:** `reviews/Feature-BusQuiescence.md`
**Work-order mode:** `new`
**Pass judgment:** `approved`

### Findings

No findings.

### Notes

`IBusQuiescence` is additive: it introduces a new interface and a new registration, changes no existing
signature, and requires no source change in any current consumer. The pin can therefore move on the
consumer side in one step once this publishes.

The registration is the part worth checking. `AddHostedService<AzureServiceBusReceiver>()` would have
constructed a second instance, leaving `IBusQuiescence` pausing processors nobody is running. It is now
a singleton resolved by both registrations, and `AddAzureServiceBusTransportTests` asserts
`Assert.Same(hosted, quiescence)` so the split cannot reappear silently.

`PauseAsync` awaits a `TaskCompletionSource` completed at the end of processor start-up, so a pause taken
while the host is still booting blocks rather than returning against an empty processor list. A start-up
exception is propagated onto that same source, which turns what would otherwise be a permanent hang into
the real error. Both operations are `IsProcessing`-guarded and serialized behind a `SemaphoreSlim`, so
repeated or interleaved calls are safe, and a caller whose `PauseAsync` failed part-way can still resume.

Not unit-tested: the pause/resume behaviour itself. It needs a live `ServiceBusProcessor`, which cannot be
constructed without a broker connection, so the unit tier can only assert the registration identity. The
behavioural proof is B2B's API E2E suite — the consumer this capability exists for — which exercises it on
every inter-test reset.
