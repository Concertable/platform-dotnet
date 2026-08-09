using Concertable.Kernel.Functional;
using System.Reflection;

namespace Concertable.Kernel.UnitTests;

public sealed class OptionTests
{
    [Fact]
    public void Some_Value_CreatesSome()
    {
        var option = Option.Some("value");

        Assert.True(option.IsSome);
        Assert.False(option.IsNone);
        Assert.True(option.TryGetValue(out var value));
        Assert.Equal("value", value);
    }

    [Fact]
    public void Some_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Option.Some<string>(null!));
    }

    [Fact]
    public void None_NoValue_CreatesNone()
    {
        var option = Option.None<string>();

        Assert.True(option.IsNone);
        Assert.False(option.IsSome);
        Assert.False(option.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Default_AllStorageShapes_AreNone()
    {
        var array = new Option<string>[1];
        var holder = new OptionHolder();

        Assert.True(default(Option<string>).IsNone);
        Assert.True(array[0].IsNone);
        Assert.True(holder.Value.IsNone);
    }

    [Fact]
    public void FromNullable_ReferenceValue_MapsNullAndValue()
    {
        string? missing = null;
        string? present = "value";

        Assert.True(Option.FromNullable(missing).IsNone);
        Assert.Equal(Option.Some("value"), Option.FromNullable(present));
        Assert.True(missing.ToOption().IsNone);
        Assert.Equal(Option.Some("value"), present.ToOption());
    }

    [Fact]
    public void FromNullable_ValueType_MapsNullAndValue()
    {
        int? missing = null;
        int? present = 42;

        Assert.True(Option.FromNullable(missing).IsNone);
        Assert.Equal(Option.Some(42), Option.FromNullable(present));
        Assert.True(missing.ToOption().IsNone);
        Assert.Equal(Option.Some(42), present.ToOption());
    }

    [Fact]
    public void Match_Some_InvokesOnlySomeOnce()
    {
        var someInvocations = 0;
        var noneInvocations = 0;

        var result = Option.Some(2).Match(
            value =>
            {
                someInvocations++;
                return value * 2;
            },
            () =>
            {
                noneInvocations++;
                return -1;
            });

        Assert.Equal(4, result);
        Assert.Equal(1, someInvocations);
        Assert.Equal(0, noneInvocations);
    }

    [Fact]
    public void Match_None_InvokesOnlyNoneOnce()
    {
        var someInvocations = 0;
        var noneInvocations = 0;

        var result = Option.None<int>().Match(
            value =>
            {
                someInvocations++;
                return value;
            },
            () =>
            {
                noneInvocations++;
                return 7;
            });

        Assert.Equal(7, result);
        Assert.Equal(0, someInvocations);
        Assert.Equal(1, noneInvocations);
    }

    [Fact]
    public void Match_ActionOverload_InvokesSelectedBranch()
    {
        var someValue = 0;
        var noneInvocations = 0;

        Option.Some(3).Match(value => someValue = value, () => noneInvocations++);
        Option.None<int>().Match(value => someValue = value, () => noneInvocations++);

        Assert.Equal(3, someValue);
        Assert.Equal(1, noneInvocations);
    }

    [Fact]
    public void Match_NullDelegate_ThrowsArgumentNullException()
    {
        var some = Option.Some(1);

        Assert.Throws<ArgumentNullException>(() => some.Match<int>(null!, () => 0));
        Assert.Throws<ArgumentNullException>(() => some.Match(value => value, null!));
        Assert.Throws<ArgumentNullException>(() => some.Match(null!, () => { }));
        Assert.Throws<ArgumentNullException>(() => some.Match(_ => { }, null!));
    }

    [Fact]
    public void Map_EachCase_MapsOnlySome()
    {
        var invocations = 0;
        Func<int, string> map = value =>
        {
            invocations++;
            return value.ToString();
        };

        var some = Option.Some(2).Map(map);
        var none = Option.None<int>().Map(map);

        Assert.Equal(Option.Some("2"), some);
        Assert.True(none.IsNone);
        Assert.Equal(1, invocations);
    }

    [Fact]
    public void Map_NullResult_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Option.Some("value").Map(_ => (string)null!));
    }

    [Fact]
    public void Bind_EachCase_BindsOnlySome()
    {
        var invocations = 0;
        Func<int, Option<string>> bind = value =>
        {
            invocations++;
            return value > 0 ? Option.Some(value.ToString()) : Option.None<string>();
        };

        var some = Option.Some(2).Bind(bind);
        var none = Option.None<int>().Bind(bind);

        Assert.Equal(Option.Some("2"), some);
        Assert.True(none.IsNone);
        Assert.Equal(1, invocations);
    }

    [Fact]
    public void OrElse_EachCase_InvokesFallbackOnlyForNone()
    {
        var invocations = 0;
        Func<Option<string>> fallback = () =>
        {
            invocations++;
            return Option.Some("fallback");
        };

        var some = Option.Some("value").OrElse(fallback);
        var none = Option.None<string>().OrElse(fallback);

        Assert.Equal(Option.Some("value"), some);
        Assert.Equal(Option.Some("fallback"), none);
        Assert.Equal(1, invocations);
    }

    [Fact]
    public void OrFailure_EagerAndLazyFactories_MapEachCase()
    {
        var invocations = 0;
        var eagerSome = Option.Some("value").OrFailure("missing");
        var eagerNone = Option.None<string>().OrFailure("missing");
        var lazySome = Option.Some("value").OrFailure(() =>
        {
            invocations++;
            return "missing";
        });
        var lazyNone = Option.None<string>().OrFailure(() =>
        {
            invocations++;
            return "missing";
        });

        Assert.Equal(Result.Success<string, string>("value"), eagerSome);
        Assert.Equal(Result.Failure<string, string>("missing"), eagerNone);
        Assert.Equal(Result.Success<string, string>("value"), lazySome);
        Assert.Equal(Result.Failure<string, string>("missing"), lazyNone);
        Assert.Equal(1, invocations);
    }

    [Fact]
    public void ValueFallbacks_EachCase_UseFallbackOnlyForNone()
    {
        var invocations = 0;
        Func<string> fallback = () =>
        {
            invocations++;
            return "fallback";
        };

        Assert.Equal("value", Option.Some("value").ValueOr("fallback"));
        Assert.Equal("fallback", Option.None<string>().ValueOr("fallback"));
        Assert.Equal("value", Option.Some("value").ValueOrElse(fallback));
        Assert.Equal("fallback", Option.None<string>().ValueOrElse(fallback));
        Assert.Equal(1, invocations);
    }

    [Fact]
    public void Combinators_NullDelegates_ThrowArgumentNullException()
    {
        var some = Option.Some(1);

        Assert.Throws<ArgumentNullException>(() => some.Map<string>(null!));
        Assert.Throws<ArgumentNullException>(() => some.Bind<string>(null!));
        Assert.Throws<ArgumentNullException>(() => some.OrElse(null!));
        Assert.Throws<ArgumentNullException>(() => some.OrFailure<string>((Func<string>)null!));
        Assert.Throws<ArgumentNullException>(() => some.ValueOrElse(null!));
        Assert.Throws<ArgumentNullException>(() => some.OrFailure<string>((string)null!));
        Assert.Throws<ArgumentNullException>(() => Option.Some("value").ValueOr(null!));
    }

    [Fact]
    public void SelectedDelegates_ThrownException_PropagatesUnchanged()
    {
        var expected = new TestException();

        Assert.Same(expected, Assert.Throws<TestException>(() => Option.Some(1).Map<string>(_ => throw expected)));
        Assert.Same(expected, Assert.Throws<TestException>(() => Option.Some(1).Bind<string>(_ => throw expected)));
        Assert.Same(expected, Assert.Throws<TestException>(() => Option.None<int>().OrElse(() => throw expected)));
        Assert.Same(expected, Assert.Throws<TestException>(() => Option.None<int>().OrFailure<string>(() => throw expected)));
        Assert.Same(expected, Assert.Throws<TestException>(() => Option.None<int>().ValueOrElse(() => throw expected)));
    }

    [Fact]
    public void EqualityHashingOperatorsAndFormatting_IncludeCaseAndValue()
    {
        var first = Option.Some("value");
        var same = Option.Some("value");
        var different = Option.Some("other");
        var none = Option.None<string>();

        Assert.Equal(first, same);
        Assert.Equal(first.GetHashCode(), same.GetHashCode());
        Assert.True(first == same);
        Assert.True(first != different);
        Assert.NotEqual(first, none);
        Assert.Equal("Some(value)", first.ToString());
        Assert.Equal("None", none.ToString());
    }

    [Fact]
    public void PublicSurface_HasNoConstructorFieldsOrImplicitConversions()
    {
        var type = typeof(Option<string>);

        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));
        Assert.DoesNotContain(type.GetMethods(BindingFlags.Public | BindingFlags.Static), method => method.Name == "op_Implicit");
    }

    [Fact]
    public void FunctorLaws_IdentityAndComposition_Hold()
    {
        Func<int, int> first = value => value + 1;
        Func<int, string> second = value => $"{value}!";

        foreach (var option in new[] { Option.Some(2), Option.None<int>() })
        {
            Assert.Equal(option, option.Map(value => value));
            Assert.Equal(option.Map(first).Map(second), option.Map(value => second(first(value))));
        }
    }

    [Fact]
    public void MonadLaws_LeftRightIdentityAndAssociativity_Hold()
    {
        Func<int, Option<int>> first = value => Option.Some(value + 1);
        Func<int, Option<string>> second = value => Option.Some($"{value}!");
        var value = 2;

        Assert.Equal(first(value), Option.Some(value).Bind(first));

        foreach (var option in new[] { Option.Some(value), Option.None<int>() })
        {
            Assert.Equal(option, option.Bind(Option.Some));
            Assert.Equal(
                option.Bind(first).Bind(second),
                option.Bind(item => first(item).Bind(second)));
        }
    }

    private sealed class OptionHolder
    {
        public Option<string> Value { get; }
    }

    private sealed class TestException : Exception;
}
