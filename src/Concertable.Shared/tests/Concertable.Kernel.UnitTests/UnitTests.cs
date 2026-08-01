using Concertable.Kernel.Functional;

namespace Concertable.Kernel.UnitTests;

public sealed class UnitTests
{
    [Fact]
    public void Value_AllInstances_AreEqual()
    {
        var left = Unit.Value;
        var right = default(Unit);

        Assert.Equal(left, right);
        Assert.True(left == right);
        Assert.False(left != right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
        Assert.Equal("Unit", left.ToString());
    }
}
