# Stripe card-tab UI E2E failure

PR #452 remains open after merge-group run `31345695038` failed UI job `93328557471`.
API E2E passed. B2B UI finished with 17 failed and 14 passed scenarios; every failed path
attempted to enter a new Stripe card.

## Failure

`StripeCardEntry.SelectCardAsync()` resolves Stripe's actual accessible Card tab:

```html
<button role="tab" data-testid="card" aria-selected="false">Card</button>
```

Playwright then waits 30 seconds for the button to become visible, enabled, and stable without
performing the click. The failure screenshot shows the fixed cookie-consent banner covering the
Stripe Payment Element at that scroll position.

## What was tried

- Clicking Stripe's nested `Card` text scrolled the iframe and hit the application's sticky header,
  navigating Customer checkout to `/find`.
- Pressing Enter on the semantic Card tab changed `aria-selected` to `true` but did not mount the
  card fields in B2B.
- Clicking the semantic Card tab only when unselected preserves Customer checkout, but B2B cannot
  action the covered button while the cookie banner remains visible.

## Required fix

Establish denied cookie consent when ordinary E2E browser contexts are created so the global banner
does not obstruct unrelated workflows. Exclude the dedicated cookie-consent scenarios, which must
continue to start without a stored decision and test the banner itself. Keep the conditional semantic
Card-tab locator; do not force-click, use coordinates, increase timeouts, or add another Stripe-only
scroll workaround.

Verify the B2B new-card, declined-card, and 3DS paths and the Customer checkout paths before
re-enqueueing PR #452.
