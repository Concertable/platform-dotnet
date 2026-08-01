using Concertable.Kernel.Functional;

namespace Concertable.Kernel.UnitTests;

public sealed class ResultCollectionExtensionsTests
{
    [Fact]
    public void Sequence_Successes_PreservesInputOrder()
    {
        var source = new[]
        {
            Result.Success<int, string>(3),
            Result.Success<int, string>(1),
            Result.Success<int, string>(2)
        };

        var result = source.Sequence();

        Assert.True(result.TryGetValue(out var values));
        Assert.Equal([3, 1, 2], values);
    }

    [Fact]
    public void Sequence_EmptyInput_ReturnsEmptySuccess()
    {
        var result = Array.Empty<Result<int, string>>().Sequence();

        Assert.True(result.TryGetValue(out var values));
        Assert.Empty(values);
    }

    [Fact]
    public void Sequence_FirstFailure_StopsEnumeration()
    {
        var enumerated = 0;

        IEnumerable<Result<int, string>> Source()
        {
            enumerated++;
            yield return Result.Success<int, string>(1);
            enumerated++;
            yield return Result.Failure<int, string>("first");
            enumerated++;
            yield return Result.Failure<int, string>("second");
        }

        var result = Source().Sequence();

        Assert.Equal(Result.Failure<IReadOnlyList<int>, string>("first"), result);
        Assert.Equal(2, enumerated);
    }

    [Fact]
    public void Sequence_UninitializedElement_ThrowsInvalidOperationException()
    {
        var source = new[] { default(Result<int, string>) };

        Assert.Throws<InvalidOperationException>(() => source.Sequence());
    }

    [Fact]
    public void Traverse_Successes_PreservesOrderAndInvokesOnce()
    {
        var visited = new List<int>();

        var result = new[] { 3, 1, 2 }.Traverse<int, string, string>(value =>
        {
            visited.Add(value);
            return Result.Success<string, string>(value.ToString());
        });

        Assert.True(result.TryGetValue(out var values));
        Assert.Equal(["3", "1", "2"], values);
        Assert.Equal([3, 1, 2], visited);
    }

    [Fact]
    public void Traverse_FirstFailure_StopsSelector()
    {
        var visited = new List<int>();

        var result = new[] { 1, 2, 3 }.Traverse<int, int, string>(value =>
        {
            visited.Add(value);
            return value == 2
                ? Result.Failure<int, string>("first")
                : Result.Success<int, string>(value);
        });

        Assert.Equal(Result.Failure<IReadOnlyList<int>, string>("first"), result);
        Assert.Equal([1, 2], visited);
    }

    [Fact]
    public void Traverse_EmptyInput_DoesNotInvokeSelector()
    {
        var invoked = false;

        var result = Array.Empty<int>().Traverse<int, int, string>(value =>
        {
            invoked = true;
            return Result.Success<int, string>(value);
        });

        Assert.True(result.TryGetValue(out var values));
        Assert.Empty(values);
        Assert.False(invoked);
    }

    [Fact]
    public async Task TraverseAsync_Successes_RunsSequentiallyInOrder()
    {
        var visited = new List<int>();
        var active = 0;
        var maximumActive = 0;

        var result = await new[] { 3, 1, 2 }.TraverseAsync<int, string, string>(
            async (value, _) =>
            {
                active++;
                maximumActive = Math.Max(maximumActive, active);
                visited.Add(value);
                await Task.Yield();
                active--;
                return Result.Success<string, string>(value.ToString());
            });

        Assert.True(result.TryGetValue(out var values));
        Assert.Equal(["3", "1", "2"], values);
        Assert.Equal([3, 1, 2], visited);
        Assert.Equal(1, maximumActive);
    }

    [Fact]
    public async Task TraverseAsync_FirstFailure_StopsSelector()
    {
        var visited = new List<int>();

        var result = await new[] { 1, 2, 3 }.TraverseAsync<int, int, string>(
            (value, _) =>
            {
                visited.Add(value);
                return Task.FromResult(
                    value == 2
                        ? Result.Failure<int, string>("first")
                        : Result.Success<int, string>(value));
            });

        Assert.Equal(Result.Failure<IReadOnlyList<int>, string>("first"), result);
        Assert.Equal([1, 2], visited);
    }

    [Fact]
    public async Task TraverseAsync_PreCancelledToken_DoesNotInvokeSelector()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var invoked = false;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new[] { 1 }.TraverseAsync<int, int, string>(
                (value, _) =>
                {
                    invoked = true;
                    return Task.FromResult(Result.Success<int, string>(value));
                },
                cancellation.Token));

        Assert.False(invoked);
    }

    [Fact]
    public async Task TraverseAsync_CancellationDuringTraversal_StopsImmediately()
    {
        using var cancellation = new CancellationTokenSource();
        var visited = new List<int>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new[] { 1, 2, 3 }.TraverseAsync<int, int, string>(
                (value, _) =>
                {
                    visited.Add(value);

                    if (value == 2)
                        cancellation.Cancel();

                    return Task.FromResult(Result.Success<int, string>(value));
                },
                cancellation.Token));

        Assert.Equal([1, 2], visited);
    }

    [Fact]
    public async Task TraverseAsync_FaultedAndCancelledSelectorTasks_Propagate()
    {
        var expected = new TestException();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Same(
            expected,
            await Assert.ThrowsAsync<TestException>(
                () => new[] { 1 }.TraverseAsync<int, int, string>(
                    (_, _) => Task.FromException<Result<int, string>>(expected))));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new[] { 1 }.TraverseAsync<int, int, string>(
                (_, _) => Task.FromCanceled<Result<int, string>>(cancellation.Token)));
    }

    [Fact]
    public void Combine_EachCase_ReturnsFirstFailureOrSuccess()
    {
        var success = new[]
        {
            Result.Success<string>(),
            Result.Success<string>()
        }.Combine();
        var failure = new[]
        {
            Result.Success<string>(),
            Result.Failure<string>("first"),
            Result.Failure<string>("second")
        }.Combine();
        var empty = Array.Empty<Result<string>>().Combine();

        Assert.Equal(Result.Success<string>(), success);
        Assert.Equal(Result.Failure<string>("first"), failure);
        Assert.Equal(Result.Success<string>(), empty);
    }

    [Fact]
    public async Task CollectionOperations_NullArguments_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => ResultCollectionExtensions.Sequence<int, string>(null!));
        Assert.Throws<ArgumentNullException>(
            () => ResultCollectionExtensions.Traverse<int, int, string>(null!, value => Result.Success<int, string>(value)));
        Assert.Throws<ArgumentNullException>(
            () => new[] { 1 }.Traverse<int, int, string>(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => ResultCollectionExtensions.TraverseAsync<int, int, string>(null!, (value, _) => Task.FromResult(Result.Success<int, string>(value))));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => new[] { 1 }.TraverseAsync<int, int, string>(null!));
        Assert.Throws<ArgumentNullException>(
            () => ResultCollectionExtensions.Combine<string>(null!));
    }

    private sealed class TestException : Exception;
}
