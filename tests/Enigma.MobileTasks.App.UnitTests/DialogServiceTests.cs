using System.Threading;
using System.Threading.Tasks;
using Enigma.MobileTasks.App.Services;
using Xunit;

namespace Enigma.MobileTasks.App.UnitTests;

/// <summary>
/// The modal questions every ViewModel asks through, answered the way the overlay answers them.
/// </summary>
public sealed class DialogServiceTests
{
    private readonly DialogService _dialog = new();

    [Fact]
    public async Task ConfirmAsync_OpensTheOverlayAndReturnsTheAnswer()
    {
        Task<bool> answer = _dialog.ConfirmAsync("Delete?", "It goes for good.", "Delete", destructive: true, TestContext.Current.CancellationToken);

        Assert.True(_dialog.IsOpen);
        Assert.False(_dialog.IsPrompt);
        Assert.True(_dialog.IsDestructive);
        Assert.True(_dialog.HasMessage);
        Assert.Equal("Delete", _dialog.ConfirmText);

        _dialog.ConfirmCommand.Execute(null);

        Assert.True(await answer);
        Assert.False(_dialog.IsOpen);
    }

    [Fact]
    public async Task ConfirmAsync_Cancelled_ReturnsFalse()
    {
        Task<bool> answer = _dialog.ConfirmAsync("Delete?", "It goes for good.", "Delete", cancellationToken: TestContext.Current.CancellationToken);

        _dialog.CancelCommand.Execute(null);

        Assert.False(await answer);
    }

    [Fact]
    public async Task PromptAsync_ReturnsTheTrimmedText()
    {
        Task<string?> answer = _dialog.PromptAsync("Rename", "Avant", "Rename", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_dialog.IsPrompt);
        Assert.Equal("Avant", _dialog.InputText);

        _dialog.InputText = "  Après  ";
        _dialog.ConfirmCommand.Execute(null);

        Assert.Equal("Après", await answer);
    }

    [Fact]
    public void PromptAsync_CannotBeConfirmedBlank()
    {
        _ = _dialog.PromptAsync("New list", null, "Create", cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(_dialog.ConfirmCommand.CanExecute(null));

        _dialog.InputText = "   ";
        Assert.False(_dialog.ConfirmCommand.CanExecute(null));

        _dialog.InputText = "Vacances";
        Assert.True(_dialog.ConfirmCommand.CanExecute(null));
    }

    [Fact]
    public async Task PromptAsync_Cancelled_ReturnsNull()
    {
        Task<string?> answer = _dialog.PromptAsync("Rename", "Avant", "Rename", cancellationToken: TestContext.Current.CancellationToken);
        _dialog.InputText = "Après";

        Assert.True(_dialog.TryCancel());

        Assert.Null(await answer);
    }

    [Fact]
    public void TryCancel_WithNothingOpen_SaysSo()
        => Assert.False(_dialog.TryCancel());

    [Fact]
    public async Task AskingAgain_AnswersTheOpenDialogNo()
    {
        Task<bool> first = _dialog.ConfirmAsync("One?", "First", "Yes", cancellationToken: TestContext.Current.CancellationToken);
        Task<bool> second = _dialog.ConfirmAsync("Two?", "Second", "Yes", cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(await first);
        Assert.Equal("Two?", _dialog.Title);
        Assert.True(_dialog.IsOpen);

        _dialog.ConfirmCommand.Execute(null);
        Assert.True(await second);
    }

    [Fact]
    public async Task Cancellation_ClosesTheDialogAsCancelled()
    {
        using CancellationTokenSource cancellation = new();
        Task<bool> answer = _dialog.ConfirmAsync("Delete?", "It goes for good.", "Delete", cancellationToken: cancellation.Token);

        await cancellation.CancelAsync();

        Assert.False(await answer);
        Assert.False(_dialog.IsOpen);
    }
}
