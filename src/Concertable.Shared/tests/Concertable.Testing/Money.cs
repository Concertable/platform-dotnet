using System.Globalization;

namespace Concertable.Testing;

public static class Money
{
    // Matches the web SPA's `£{n.toFixed(2)}` display: 2 dp, no thousands separator, invariant so the
    // dot decimal separator holds under any culture. Use in UI assertions on rendered £ amounts.
    public static string Pounds(decimal amount) => $"£{amount.ToString("0.00", CultureInfo.InvariantCulture)}";
}
