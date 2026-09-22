# Code review — Fix/BusQuiescenceHostedDescriptor

> **This file is a work order, not a discussion.** If you're handed this file, fix the open `[ ]`
> findings directly and report what changed. Tick each `[x]` as you land it. Pause only for a genuinely
> irreversible or ambiguous finding: record its durable disposition, take the safe path, and keep going.

**Review status:** `complete`
**Reviewed up to commit:** `53157935810ebc8f69590b8df75fa84ac1a6b2ce`  `(2026-09-22)`
**Judgment:** `approved`

## Review pass — 2026-09-22 — native/general

**Candidate base:** `65aa0fd89621ce7d2b4f8ee410a68a98337115e8`
**Candidate head:** `53157935810ebc8f69590b8df75fa84ac1a6b2ce`
**Candidate branch:** `Fix/BusQuiescenceHostedDescriptor`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:06d5788ca5bfcc2720fbccd235945de96f1df3e0ecd5bcc78c8160e8543b5803` `(2 paths)`
**Work-order path:** `reviews/Fix-BusQuiescenceHostedDescriptor.md`
**Work-order mode:** `new`
**Pass judgment:** `approved`

### Findings

No findings.

### Notes

This repairs a regression introduced one commit earlier by `Feature/BusQuiescence`.

`IntegrationTestHostExtensions.RemoveAzureServiceBus` identifies the receiver by an `IHostedService`
descriptor whose `ImplementationType` is named `AzureServiceBusReceiver`. Registering that hosted service
through a factory sets `ImplementationType` to null, so the removal silently matched nothing: every
integration host kept a live receiver, resolved a `ServiceBusClient` against a host that deliberately
supplies no connection string in the Integration environment, and failed. The break was invisible in this
repository — it only surfaced in a consumer, which is what the two-step publish exists to expose.

The descriptor is typed again, and `IBusQuiescence` now reaches the running receiver through
`GetServices<IHostedService>().OfType<AzureServiceBusReceiver>()` rather than constructing its own. That
keeps the single-instance guarantee the capability depends on while leaving the removal contract intact.
Resolving `IBusQuiescence` in a host that removed the receiver now throws a named
`InvalidOperationException` instead of a null reference.

`Receiver_IsRegisteredWithAnImplementationType_SoAHostCanRemoveIt` pins the descriptor shape that
`RemoveAzureServiceBus` depends on, so this cannot regress silently again. Sixteen unit tests pass.
