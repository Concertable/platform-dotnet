using Concertable.Messaging.Contracts;
using Concertable.Messaging.Infrastructure;
using Concertable.Messaging.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Concertable.Messaging.UnitTests;

public sealed class HostQuiescenceTests
{
    private readonly List<string> log = new();

    [Fact]
    public async Task PauseAsync_PausesEveryRegisteredParticipant()
    {
        var host = new HostQuiescence([Recording("a"), Recording("b")], []);

        await host.PauseAsync();

        Assert.Equal(["pause:a", "pause:b"], log);
    }

    [Fact]
    public async Task ResumeAsync_ResumesParticipantsInReverseOrder()
    {
        var host = new HostQuiescence([Recording("a"), Recording("b")], []);

        await host.ResumeAsync();

        Assert.Equal(["resume:b", "resume:a"], log);
    }

    [Fact]
    public async Task PauseAsync_WhenAParticipantThrows_ResumesThoseAlreadyPausedAndRethrows()
    {
        var host = new HostQuiescence(
            [Recording("a"), Recording("b", throwOnPause: true), Recording("c")], []);

        await Assert.ThrowsAsync<InvalidOperationException>(() => host.PauseAsync());

        Assert.Equal(["pause:a", "pause:b", "resume:a"], log);
    }

    [Fact]
    public async Task PauseAsync_DiscoversHostedServicesThatAreParticipants_AndSkipsPlainOnes()
    {
        var hosted = new RecordingHostedQuiescer(log, "hosted");
        var host = new HostQuiescence([Recording("http")], [hosted, new PlainHostedService()]);

        await host.PauseAsync();

        Assert.Equal(["pause:http", "pause:hosted"], log);
    }

    [Fact]
    public async Task PauseAsync_DoesNotDoublePauseAParticipantRegisteredBothWays()
    {
        var both = new RecordingHostedQuiescer(log, "both");
        var host = new HostQuiescence([both], [both]);

        await host.PauseAsync();

        Assert.Equal(["pause:both"], log);
    }

    [Fact]
    public void AddHostQuiescence_ResolvesTheAggregate()
    {
        var provider = new ServiceCollection()
            .AddSingleton<IIngressQuiescer>(Recording("a"))
            .AddHostQuiescence()
            .BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IHostQuiescence>());
    }

    private RecordingQuiescer Recording(string name, bool throwOnPause = false) =>
        new(log, name, throwOnPause);

    private sealed class PlainHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken ct) => Task.CompletedTask;

        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class RecordingHostedQuiescer : IIngressQuiescer, IHostedService
    {
        private readonly List<string> log;
        private readonly string name;

        public RecordingHostedQuiescer(List<string> log, string name)
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

    private sealed class RecordingQuiescer : IIngressQuiescer
    {
        private readonly List<string> log;
        private readonly string name;
        private readonly bool throwOnPause;

        public RecordingQuiescer(List<string> log, string name, bool throwOnPause)
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
