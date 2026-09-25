using Microsoft.AspNetCore.Http;

namespace Concertable.Messaging.AspNetCore.UnitTests;

public sealed class PausableRequestsTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task PauseAsync_WithNothingInFlight_ReturnsImmediately()
    {
        var requests = new PausableRequests(new HttpContextAccessor());

        var pause = requests.PauseAsync();

        await pause.WaitAsync(Timeout);
        Assert.True(pause.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task PauseAsync_ExcludesTheRequestDrivingThePause()
    {
        var pauser = new DefaultHttpContext();
        var requests = new PausableRequests(new HttpContextAccessor { HttpContext = pauser });
        await requests.EnterAsync(pauser, CancellationToken.None);

        var pause = requests.PauseAsync();

        await pause.WaitAsync(Timeout);
        Assert.True(pause.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task PauseAsync_WaitsForAnUnrelatedInFlightRequestToExit()
    {
        var pauser = new DefaultHttpContext();
        var requests = new PausableRequests(new HttpContextAccessor { HttpContext = pauser });
        var other = new DefaultHttpContext();
        await requests.EnterAsync(other, CancellationToken.None);
        await requests.EnterAsync(pauser, CancellationToken.None);

        var pause = requests.PauseAsync();
        await Task.Delay(100);
        Assert.False(pause.IsCompleted);

        requests.Exit(other);

        await pause.WaitAsync(Timeout);
        Assert.True(pause.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task PauseAsync_OverlappingCalls_BothCompleteWhenTheInFlightRequestExits()
    {
        var requests = new PausableRequests(new HttpContextAccessor());
        var other = new DefaultHttpContext();
        await requests.EnterAsync(other, CancellationToken.None);

        var pauseA = requests.PauseAsync();
        var pauseB = requests.PauseAsync();
        await Task.Delay(100);
        Assert.False(pauseA.IsCompleted);
        Assert.False(pauseB.IsCompleted);

        requests.Exit(other);

        await Task.WhenAll(pauseA, pauseB).WaitAsync(Timeout);
    }

    [Fact]
    public async Task ResumeAsync_WhileAPauseIsStillDraining_ReleasesTheWaitingPause()
    {
        var requests = new PausableRequests(new HttpContextAccessor());
        var other = new DefaultHttpContext();
        await requests.EnterAsync(other, CancellationToken.None);

        var pause = requests.PauseAsync();
        await Task.Delay(100);
        Assert.False(pause.IsCompleted);

        await requests.ResumeAsync();

        await pause.WaitAsync(Timeout);
        Assert.True(pause.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task EnterAsync_WhilePaused_WaitsForResume()
    {
        var requests = new PausableRequests(new HttpContextAccessor());
        await requests.PauseAsync();

        var entry = requests.EnterAsync(new DefaultHttpContext(), CancellationToken.None);
        await Task.Delay(100);
        Assert.False(entry.IsCompleted);

        await requests.ResumeAsync();

        await entry.WaitAsync(Timeout);
        Assert.True(entry.IsCompletedSuccessfully);
    }
}
