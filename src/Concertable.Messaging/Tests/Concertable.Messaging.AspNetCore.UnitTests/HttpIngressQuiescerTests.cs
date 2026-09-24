using Microsoft.AspNetCore.Http;

namespace Concertable.Messaging.AspNetCore.UnitTests;

public sealed class HttpIngressQuiescerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task PauseAsync_WithNothingInFlight_ReturnsImmediately()
    {
        var quiescer = new HttpIngressQuiescer(new HttpContextAccessor());

        var pause = quiescer.PauseAsync();

        await pause.WaitAsync(Timeout);
        Assert.True(pause.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task PauseAsync_ExcludesTheRequestDrivingThePause()
    {
        var pauser = new DefaultHttpContext();
        var quiescer = new HttpIngressQuiescer(new HttpContextAccessor { HttpContext = pauser });
        await quiescer.EnterAsync(pauser, CancellationToken.None);

        var pause = quiescer.PauseAsync();

        await pause.WaitAsync(Timeout);
        Assert.True(pause.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task PauseAsync_WaitsForAnUnrelatedInFlightRequestToExit()
    {
        var pauser = new DefaultHttpContext();
        var quiescer = new HttpIngressQuiescer(new HttpContextAccessor { HttpContext = pauser });
        var other = new DefaultHttpContext();
        await quiescer.EnterAsync(other, CancellationToken.None);
        await quiescer.EnterAsync(pauser, CancellationToken.None);

        var pause = quiescer.PauseAsync();
        await Task.Delay(100);
        Assert.False(pause.IsCompleted);

        quiescer.Exit(other);

        await pause.WaitAsync(Timeout);
        Assert.True(pause.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task PauseAsync_OverlappingCalls_BothCompleteWhenTheInFlightRequestExits()
    {
        var quiescer = new HttpIngressQuiescer(new HttpContextAccessor());
        var other = new DefaultHttpContext();
        await quiescer.EnterAsync(other, CancellationToken.None);

        var pauseA = quiescer.PauseAsync();
        var pauseB = quiescer.PauseAsync();
        await Task.Delay(100);
        Assert.False(pauseA.IsCompleted);
        Assert.False(pauseB.IsCompleted);

        quiescer.Exit(other);

        await Task.WhenAll(pauseA, pauseB).WaitAsync(Timeout);
    }

    [Fact]
    public async Task ResumeAsync_WhileAPauseIsStillDraining_ReleasesTheWaitingPause()
    {
        var quiescer = new HttpIngressQuiescer(new HttpContextAccessor());
        var other = new DefaultHttpContext();
        await quiescer.EnterAsync(other, CancellationToken.None);

        var pause = quiescer.PauseAsync();
        await Task.Delay(100);
        Assert.False(pause.IsCompleted);

        await quiescer.ResumeAsync();

        await pause.WaitAsync(Timeout);
        Assert.True(pause.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task EnterAsync_WhilePaused_WaitsForResume()
    {
        var quiescer = new HttpIngressQuiescer(new HttpContextAccessor());
        await quiescer.PauseAsync();

        var entry = quiescer.EnterAsync(new DefaultHttpContext(), CancellationToken.None);
        await Task.Delay(100);
        Assert.False(entry.IsCompleted);

        await quiescer.ResumeAsync();

        await entry.WaitAsync(Timeout);
        Assert.True(entry.IsCompletedSuccessfully);
    }
}
