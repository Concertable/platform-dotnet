using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Concertable.Messaging.AspNetCore.UnitTests;

public sealed class GateMiddlewareTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task PauseAsync_WithNothingInFlight_ReturnsImmediately()
    {
        var gate = new GateMiddleware(new HttpContextAccessor(), Options.Create(new GateOptions()));

        var pause = gate.PauseAsync();

        await pause.WaitAsync(Timeout);
        Assert.True(pause.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task PauseAsync_ExcludesTheRequestDrivingThePause()
    {
        var pauser = new DefaultHttpContext();
        var gate = new GateMiddleware(new HttpContextAccessor { HttpContext = pauser }, Options.Create(new GateOptions()));
        await gate.EnterAsync(pauser, CancellationToken.None);

        var pause = gate.PauseAsync();

        await pause.WaitAsync(Timeout);
        Assert.True(pause.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task PauseAsync_WaitsForAnUnrelatedInFlightRequestToExit()
    {
        var pauser = new DefaultHttpContext();
        var gate = new GateMiddleware(new HttpContextAccessor { HttpContext = pauser }, Options.Create(new GateOptions()));
        var other = new DefaultHttpContext();
        await gate.EnterAsync(other, CancellationToken.None);
        await gate.EnterAsync(pauser, CancellationToken.None);

        var pause = gate.PauseAsync();
        await Task.Delay(100);
        Assert.False(pause.IsCompleted);

        gate.Exit(other);

        await pause.WaitAsync(Timeout);
        Assert.True(pause.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task PauseAsync_OverlappingCalls_BothCompleteWhenTheInFlightRequestExits()
    {
        var gate = new GateMiddleware(new HttpContextAccessor(), Options.Create(new GateOptions()));
        var other = new DefaultHttpContext();
        await gate.EnterAsync(other, CancellationToken.None);

        var pauseA = gate.PauseAsync();
        var pauseB = gate.PauseAsync();
        await Task.Delay(100);
        Assert.False(pauseA.IsCompleted);
        Assert.False(pauseB.IsCompleted);

        gate.Exit(other);

        await Task.WhenAll(pauseA, pauseB).WaitAsync(Timeout);
    }

    [Fact]
    public async Task ResumeAsync_WhileAPauseIsStillDraining_ReleasesTheWaitingPause()
    {
        var gate = new GateMiddleware(new HttpContextAccessor(), Options.Create(new GateOptions()));
        var other = new DefaultHttpContext();
        await gate.EnterAsync(other, CancellationToken.None);

        var pause = gate.PauseAsync();
        await Task.Delay(100);
        Assert.False(pause.IsCompleted);

        await gate.ResumeAsync();

        await pause.WaitAsync(Timeout);
        Assert.True(pause.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task EnterAsync_WhilePaused_WaitsForResume()
    {
        var gate = new GateMiddleware(new HttpContextAccessor(), Options.Create(new GateOptions()));
        await gate.PauseAsync();

        var entry = gate.EnterAsync(new DefaultHttpContext(), CancellationToken.None);
        await Task.Delay(100);
        Assert.False(entry.IsCompleted);

        await gate.ResumeAsync();

        await entry.WaitAsync(Timeout);
        Assert.True(entry.IsCompletedSuccessfully);
    }
}
