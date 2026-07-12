# Concertable.E2ETests — shared E2E harness

## This project is SERVICE-AGNOSTIC. Nothing service-specific goes here. Ever.

This library holds only what **every** E2E suite needs, with no knowledge of any data service:

- Pins for **adapter services** (`PinAuthService`, `PinPaymentWeb`, `PinPaymentWorkers`, `PinStripeCli`) — Auth and Payment are adapters present in every host by architecture, so their pins are legitimately shared.
- Generic infra (`AddEphemeralSql`, `HealthWaiter`, `PollingService`, `TestTokenMinter`, `AspireResourceLogger`, the MSBuild tasks).

Things that must **never** be added here:

- **Per-service composition** (`AddB2BE2E`, `AddCustomerE2E`, `PinB2BWeb`, `PinCustomerWeb`, …). These live in the owning suite: `Concertable.B2B.E2ETests` and `Concertable.Customer.E2ETests` each carry their own `DistributedApplicationBuilderExtensions`.
- **Data-service helpers**, even when more than one suite consumes them. They get a helpers project owned by that service, referenced explicitly by the suites that need it — see `Concertable.Payment.E2ETests.Helpers` and `Concertable.Search.E2ETests.Helpers` (`AddSearchService`: both find pages are Search-backed, so both suites run Search by importing Search's own helpers project as an isolated dependency).
- Anything referencing a data service's runtime projects, types, or seed libraries.

The test for new code: *"would this file still compile and make sense if every data service moved to its own repo tomorrow?"* If a type, pin, or path in it names B2B, Customer, or Search, the answer is no — put it in that service's tree.

This rule has been violated and reverted before. Don't relitigate it: if a suite needs something service-specific, the suite (or the owning service's helpers project) is where it goes, even if that means two suites each writing three similar lines.

## Authoring scenarios — the rules for EVERY UI suite (B2B, Customer)

Scenarios live in the suites, not in this project, but the rules are identical across suites, so they
live here (the one doc both suites depend on). Each suite's own `CLAUDE.md` points here and adds only
its concrete fast-forward mechanics (its `SeedState` shape). This prevents the two suites drifting.

**A scenario tests one behaviour and starts at the nearest already-verified state.** It never
re-drives earlier stages through the browser to reach its starting line. If a happy path already
covers `create → book`, a scenario acting on a booking fast-forwards to "booked" and drives only its
own behaviour + assertion. Litmus test before writing a `When`/`And` setup step: *is this proving the
behaviour in the scenario title, or just getting me to the starting line?* If it's the latter and
another scenario already covers it, make it a fast-forward `Given`, not UI steps.

**Fast-forward via seeded state, never UI replay.** A setup `Given` reads pre-seeded data off the
suite's fixture (`fixture.App.SeedState…`) and sets the id on scenario state — no navigation, no clicks.
When the starting state you need doesn't exist yet, add the seeded state + a `Given`; don't reach it by
replaying UI steps another scenario already runs.

**The one thing you cannot seed: payment/Stripe state.** Seeding obeys production's rule (a seeder only
writes what prod writes directly), and real Payment emits only on live Stripe webhooks — so no seeder
creates a PaymentIntent/charge. A scenario whose assertion needs a real Stripe object (e.g. a refund
reversing a real charge) must run the real paying flow; it can't be pure-seed fast-forwarded. Split it:
the cheap state-transition assertion starts from seeded state, the Stripe-dependent assertion stays on
a flow that actually paid.

**Baseline discipline — `E2E_BASELINE.md` (this directory).** `./e2e.ps1 ui regress` trusts it. Two
traps: (1) when a scenario crosses the line, move it between the `passing`/`failing` blocks AND fix both
`(N)` counts and the summary table (the parser throws on a mismatch); (2) adding an assertion to an
already-green scenario can silently turn it red while the baseline still lists it as passing — the name
didn't change, but the body now fails. Re-run and reconcile; a name in `passing` is not proof the
current body passes.

**Headless by default.** Run via `./e2e.ps1 ui <cmd>` (mandatory Docker health gate); `-Headed` only
when a human is watching — it changes nothing that's asserted.
