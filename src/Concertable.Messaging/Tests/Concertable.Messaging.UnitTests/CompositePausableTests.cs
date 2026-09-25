using Concertable.Messaging.Contracts;
using Concertable.Messaging.Infrastructure;
using Concertable.Messaging.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Concertable.Messaging.UnitTests;

public sealed class CompositePausableTests
{
    private readonly List<string> log = new();

    [Fact]
    public async Task PauseAsync_PausesEveryRegisteredPausable()
    {
        var host = new CompositePausable([Recording("a"), Recording("b")], []);

        await host.PauseAsync();

        Assert.Equal(["pause:a", "pause:b"], log);
    }

    [Fact]
    public async Task ResumeAsync_ResumesInReverseOrder()
    {
        var host = new CompositePausable([Recording("a"), Recording("b")], []);

        await host.ResumeAsync();

        Assert.Equal(["resume:b", "resume:a"], log);
    }

    [Fact]
    public async Task PauseAsync_WhenOneThrows_ResumesThoseAlreadyPausedAndRethrows()
    {
        var host = new CompositePausable(
            [Recording("a"), Recording("b", throwOnPause: true), Recording("c")], []);

        await Assert.ThrowsAsync<InvalidOperationException>(() => host.PauseAsync());

        Assert.Equal(["pause:a", "pause:b", "resume:a"], log);
    }

    [Fact]
    public async Task PauseAsync_DiscoversHostedServicesThatArePausable_AndSkipsPlainOnes()
    {
        var hosted = new RecordingHostedPausable(log, "hosted");
        var host = new CompositePausable([Recording("http")], [hosted, new PlainHostedService()]);

        await host.PauseAsync();

        Assert.Equal(["pause:http", "pause:hosted"], log);
    }

    [Fact]
    public async Task PauseAsync_DoesNotDoublePauseOneRegisteredBothWays()
    {
        var both = new RecordingHostedPausable(log, "both");
        var host = new CompositePausable([both], [both]);

        await host.PauseAsync();

        Assert.Equal(["pause:both"], log);
    }

    [Fact]
    public void AddCompositePausable_ResolvesTheCompositeWithoutRegisteringItAsAPausable()
    {
        var provider = new ServiceCollection()
            .AddSingleton<IPausable>(Recording("a"))
            .AddCompositePausable()
            .BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<CompositePausable>());
        Assert.DoesNotContain(provider.GetServices<IPausable>(), pausable => pausable is CompositePausable);
    }

    private RecordingPausable Recording(string name, bool throwOnPause = false) =>
        new(log, name, throwOnPause);

    private sealed class PlainHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken ct) => Task.CompletedTask;

        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class RecordingHostedPausable : IPausable, IHostedService
    {
        private readonly List<string> log;
        private readonly string name;

        public RecordingHostedPausable(List<string> log, string name)
        {
            this.log = log;
            this.name = name;
        }

        public Task PauseAsync(CancellationToken ct = default)
        {
            log.Add($"pause:{name}");
            return Task.CompletedTask;
        }

        public Task ResumeAsync(CancellationToken ct = default)
        {
            log.Add($"resume:{name}");
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken ct) => Task.CompletedTask;

        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class RecordingPausable : IPausable
    {
        private readonly List<string> log;
        private readonly string name;
        private readonly bool throwOnPause;

        public RecordingPausable(List<string> log, string name, bool throwOnPause)
        {
            this.log = log;
            this.name = name;
            this.throwOnPause = throwOnPause;
        }

        public Task PauseAsync(CancellationToken ct = default)
        {
            log.Add($"pause:{name}");
            if (throwOnPause)
                throw new InvalidOperationException(name);
            return Task.CompletedTask;
        }

        public Task ResumeAsync(CancellationToken ct = default)
        {
            log.Add($"resume:{name}");
            return Task.CompletedTask;
        }
    }
}
