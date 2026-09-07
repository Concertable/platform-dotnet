# Concertable.AppHost.Shared — technical debt

Debt local to the reusable Aspire hosting and topology helpers.

---

## HIGH

### A published message type that is declared nowhere fails at runtime, three services downstream

`AsbTopology.Publish<T>()`/`Subscribe<T>()` and each host's `reg.Publishes<T>()` are the only declarations of
a message URN, and nothing checks them against what the code actually publishes. Publish an integration event
through the outbox without declaring it and the transport's type dictionary has no entry, so the publish
throws `KeyNotFoundException: The given key '<urn>' was not present in the dictionary` from inside whichever
handler happened to raise the domain event. Nothing near the cause logs a payment, booking or messaging
error: the outbox message is poison, everything behind it stalls, and the first visible symptom is a
consumer's state never arriving — in the case that produced this entry, a browser scenario timing out after
60s waiting for a page navigation, with only repeated HTTP 404s to go on.

Two live instances were found this way, both silent, both costing a full diagnostic cycle each:
`concertable.payment.payment-operation-state-changed.v1` (undeclared on `main` as well as on the branch) and
`concertable.b2b.application-accepted.v1`.

The check needs no host, no container and no stack — it is reflection over the assemblies a service already
loads: every type carrying `[MessageType]` that the service publishes must appear in that service's topology
**and** in its host registration, and every type it handles must appear as a subscription. It belongs in each
service's architecture suite, which is where the equivalent contract-inventory and Reunion-ownership
invariants already live.

The startup tier stacked on PR #946 does not cover this: strict service-provider validation and composition
tests prove the DI graph resolves, and an undeclared message URN is not a DI registration.

**Resolves when:** each service's architecture suite fails when a `[MessageType]` it publishes or handles is
absent from that service's `AsbTopology` declarations or its host's registration builder, and the two
instances above are covered by it rather than by hand-added declarations.

---

## LOW

### The pinned-image composition assertions are copy-pasted into all four service architecture suites

`AssertImageEndpoint`, `AssertContainerRuntimeArgs` and `AssertUsesDeveloperCertificate` are declared verbatim in `B2BHostGraphTests`, `CustomerArchitectureTests`, `PaymentArchitectureTests` and `SearchArchitectureTests` (about 45 lines each). Their natural home is `Concertable.Testing.Architecture`, which every one of those suites already references as a published package. Moving them there means that package taking an `Aspire.Hosting` and `Concertable.AppHost.Shared` dependency — it currently has neither — so a shared-testing package would start carrying the AppHost graph vocabulary.

**Resolves when:** the three helpers exist once in `Concertable.Testing.Architecture` (or a new AppHost-graph testing package), all four suites call them from there, and no service arch suite declares its own copy.
