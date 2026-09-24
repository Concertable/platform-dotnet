# Code review — Feature/HostIngressQuiescence

> **This file is a work order, not a discussion.** If you're handed this file, fix the open `[ ]`
> findings directly and report what changed. Tick each `[x]` as you land it. Pause only for a genuinely
> irreversible or ambiguous finding: record its durable disposition, take the safe path, and keep going.

**Review status:** `complete`
**Reviewed up to commit:** `72c98b801c51`  `(2026-09-24)`
**Judgment:** `approved`

## Review pass — 2026-09-24 — native/general + concurrency + boundary

**Candidate base:** `766a0e1e07c6`
**Candidate head:** `72c98b801c51`
**Candidate branch:** `Feature/HostIngressQuiescence`
**Candidate scope:** `all`
**Work-order path:** `reviews/Feature-HostIngressQuiescence.md`
**Work-order mode:** `new`
**Pass judgment:** `approved`

All findings were remediated at commit `41b0143` on this branch; the pass is approved with every finding
terminal.

### Findings

- [x] **HQ1 — HIGH — concurrency** — `Concertable.Messaging.AspNetCore/HttpIngressQuiescer.cs`
  Overlapping `PauseAsync` calls overwrote the single `drained` source, orphaning the first caller's wait
  forever. Fixed: a second pause joins the first's drain; `drained` is re-created only on the false→true
  transition.
- [x] **HQ2 — HIGH — concurrency** — `Concertable.Messaging.AspNetCore/HttpIngressQuiescer.cs`
  `ResumeAsync` racing an in-flight `PauseAsync` stranded the pause. Fixed: `ResumeAsync` completes the drain
  source, releasing any pause still waiting.
- [x] **HQ3 — MEDIUM — correctness** — `Concertable.Messaging.Infrastructure/HostQuiescence.cs`
  Rollback stopped at the first failing `ResumeAsync` and let that error mask the original pause failure.
  Fixed: rollback resumes every already-paused participant best-effort and rethrows the original exception.
- [x] **HQ4 — HIGH — boundary/robustness** — `Concertable.Messaging.AzureServiceBus`, `.Infrastructure`
  `RemoveAzureServiceBus` (integration harness) removed the receiver hosted service but not its throwing
  `IIngressQuiescer` factory, which then threw when the aggregate enumerated it. Fixed: `HostQuiescence`
  discovers hosted-service participants through `IHostedService`, so removal drops them automatically and the
  throwing factories are gone.
- [x] **HQ5 — LOW — lifetime** — `Concertable.Messaging.Infrastructure/Outbox/OutboxDispatcher.cs`
  `Dispose` disposed the `SemaphoreSlim` before shutdown, unsafe under a shutdown-timeout race. Fixed: the
  override is removed; the semaphore needs no disposal here.
- [x] **HQ6 — LOW — naming** — `Concertable.Messaging.AspNetCore/Extensions/ApplicationBuilderExtensions.cs`
  `UseHostQuiescence` → `UseHttpRequestQuiescence` for `Add`/`Use` symmetry, reserving `HostQuiescence` for
  the aggregate.
- [x] **HQ7 — LOW — style** — `Concertable.Messaging.AspNetCore.UnitTests`
  Added the sibling global-using convention and dropped redundant per-file usings.

### Notes

`IBusQuiescence` is deleted and replaced by `IIngressQuiescer` (one ingress) plus `IHostQuiescence` (the
aggregate the reset resolves); no in-repo references to the old name remain. The consumer bump is the second
step of the two-step publish.

Group isolation holds: the new `Concertable.Messaging.AspNetCore` project references only
`Concertable.Messaging.Contracts` within the Messaging group; no existing group could host the HTTP
middleware without a first-ever cross-group edge, so same-group placement is the least-bad option.

Behavioural proof: 113 passing messaging tests (8 AspNetCore unit, 67 infrastructure unit, 16 ASB unit,
22 integration), including overlapping-pause and resume-releases-pause coverage. The end-to-end proof is
B2B's reset once it bumps the pin.
