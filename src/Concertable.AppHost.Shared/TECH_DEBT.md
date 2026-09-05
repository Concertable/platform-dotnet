# Concertable.AppHost.Shared — technical debt

Debt local to the reusable Aspire hosting and topology helpers.

---

## LOW

### The pinned-image composition assertions are copy-pasted into all four service architecture suites

`AssertImageEndpoint`, `AssertContainerRuntimeArgs` and `AssertUsesDeveloperCertificate` are declared verbatim in `B2BHostGraphTests`, `CustomerArchitectureTests`, `PaymentArchitectureTests` and `SearchArchitectureTests` (about 45 lines each). Their natural home is `Concertable.Testing.Architecture`, which every one of those suites already references as a published package. Moving them there means that package taking an `Aspire.Hosting` and `Concertable.AppHost.Shared` dependency — it currently has neither — so a shared-testing package would start carrying the AppHost graph vocabulary.

**Resolves when:** the three helpers exist once in `Concertable.Testing.Architecture` (or a new AppHost-graph testing package), all four suites call them from there, and no service arch suite declares its own copy.
