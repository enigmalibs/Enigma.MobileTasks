using System;
using System.Threading.Tasks;
using Enigma.MobileTasks.App.Services;
using Xunit;

namespace Enigma.MobileTasks.App.UnitTests;

/// <summary>
/// The snackbar: what it shows, what its action does, and that it goes away by itself.
/// </summary>
public sealed class NotificationServiceTests
{
    private readonly NotificationService _notifications = new();

    [Fact]
    public void Show_DisplaysTheMessageWithoutAnAction()
    {
        _notifications.Show("Saved.");

        Assert.True(_notifications.IsVisible);
        Assert.Equal("Saved.", _notifications.Message);
        Assert.False(_notifications.HasAction);
    }

    [Fact]
    public void Action_RunsOnceAndDismisses()
    {
        int runs = 0;
        _notifications.Show("Item deleted.", "Undo", () => runs++);

        Assert.True(_notifications.HasAction);
        Assert.Equal("Undo", _notifications.ActionText);

        _notifications.ActionCommand.Execute(null);
        _notifications.ActionCommand.Execute(null);

        Assert.Equal(1, runs);
        Assert.False(_notifications.IsVisible);
    }

    [Fact]
    public void ActionTextWithoutAnAction_ShowsNoButton()
    {
        _notifications.Show("Nothing to undo.", "Undo");

        Assert.False(_notifications.HasAction);
    }

    [Fact]
    public void Show_ReplacesTheMessageAndItsAction()
    {
        int runs = 0;
        _notifications.Show("First.", "Undo", () => runs++);
        _notifications.Show("Second.");

        _notifications.ActionCommand.Execute(null);

        Assert.Equal("Second.", _notifications.Message);
        Assert.Equal(0, runs);
    }

    [Fact]
    public async Task AMessage_GoesAwayByItself()
    {
        _notifications.MessageDuration = TimeSpan.FromMilliseconds(20);
        _notifications.Show("Saved.");

        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (_notifications.IsVisible && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        Assert.False(_notifications.IsVisible);
    }

    [Fact]
    public async Task AReplacedMessage_IsNotDismissedByTheOldTimer()
    {
        _notifications.MessageDuration = TimeSpan.FromMilliseconds(50);
        _notifications.Show("First.");
        _notifications.MessageDuration = TimeSpan.FromSeconds(30);
        _notifications.Show("Second.");

        await Task.Delay(200, TestContext.Current.CancellationToken);

        Assert.True(_notifications.IsVisible);
        Assert.Equal("Second.", _notifications.Message);
    }
}
