using System;
using System.Threading.Tasks;
using Enigma.MobileTasks.Core.Model;
using Enigma.MobileTasks.Core.UnitTests.Infrastructure;
using Enigma.MobileTasks.Core.Workspace;
using Xunit;

namespace Enigma.MobileTasks.Core.UnitTests;

/// <summary>
/// The debounce between ticking a box and the file on disk.
/// </summary>
public sealed class SaveSchedulerTests
{
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(40);
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

    private readonly RecordingWorkspaceStore _store = new();

    [Fact]
    public async Task Schedule_WritesOnceTheChangesStop()
    {
        await using SaveScheduler scheduler = new(_store, Delay);
        WorkspaceEntry entry = Entry("a.json");

        scheduler.Schedule(entry);

        await WaitUntil(() => _store.Saved.Count == 1);
        Assert.Equal(["a.json"], _store.Saved);
        Assert.False(scheduler.HasPending);
    }

    [Fact]
    public async Task Schedule_CoalescesABurstIntoOneWrite()
    {
        await using SaveScheduler scheduler = new(_store, TimeSpan.FromSeconds(30));
        WorkspaceEntry entry = Entry("a.json");

        scheduler.Schedule(entry);
        scheduler.Schedule(entry);
        scheduler.Schedule(entry);

        Assert.True(scheduler.HasPending);
        await scheduler.FlushAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["a.json"], _store.Saved);
        Assert.False(scheduler.HasPending);
    }

    [Fact]
    public async Task Schedule_KeepsSeveralListsApart()
    {
        await using SaveScheduler scheduler = new(_store, TimeSpan.FromSeconds(30));

        scheduler.Schedule(Entry("a.json"));
        scheduler.Schedule(Entry("b.json"));

        await scheduler.FlushAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, _store.Saved.Count);
        Assert.Contains("a.json", _store.Saved);
        Assert.Contains("b.json", _store.Saved);
    }

    [Fact]
    public async Task FlushAsync_WithNothingWaiting_DoesNothing()
    {
        await using SaveScheduler scheduler = new(_store, Delay);

        await scheduler.FlushAsync(TestContext.Current.CancellationToken);

        Assert.Empty(_store.Saved);
    }

    [Fact]
    public async Task DisposeAsync_WritesWhatIsStillWaiting()
    {
        SaveScheduler scheduler = new(_store, TimeSpan.FromSeconds(30));
        scheduler.Schedule(Entry("a.json"));

        await scheduler.DisposeAsync();

        Assert.Equal(["a.json"], _store.Saved);
    }

    [Fact]
    public async Task AFailedWrite_IsReportedRatherThanThrownAtWhoeverWasTyping()
    {
        await using SaveScheduler scheduler = new(_store, Delay);
        _store.Failure = new UnauthorizedAccessException("lecture seule");
        WorkspaceSaveFailedEventArgs? failure = null;
        scheduler.SaveFailed += (_, e) => failure = e;

        scheduler.Schedule(Entry("a.json"));

        await WaitUntil(() => failure is not null);
        Assert.Equal("lecture seule", failure?.Exception.Message);
        Assert.Equal("a.json", failure?.Entry.Path);
    }

    private static WorkspaceEntry Entry(string path) => new(path, new TaskList("Essai"));

    /// <summary>
    /// Waits for something the scheduler does on its own timer, polling rather than sleeping a
    /// fixed time so the test is neither slow nor flaky.
    /// </summary>
    /// <param name="condition">What to wait for.</param>
    /// <returns>A task that completes once the condition holds.</returns>
    private static async Task WaitUntil(Func<bool> condition)
    {
        DateTime deadline = DateTime.UtcNow + Patience;

        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "The scheduler did not get there in time.");
            await Task.Delay(5, TestContext.Current.CancellationToken);
        }
    }
}
