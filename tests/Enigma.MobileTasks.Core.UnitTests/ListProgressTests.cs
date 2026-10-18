using Enigma.MobileTasks.Core.Model;
using Enigma.MobileTasks.Core.Operations;
using Enigma.MobileTasks.Core.UnitTests.Infrastructure;
using Xunit;

namespace Enigma.MobileTasks.Core.UnitTests;

/// <summary>
/// The counter behind every progress bar in the application.
/// </summary>
public sealed class ListProgressTests
{
    private readonly SequentialIdGenerator _ids = new();

    [Fact]
    public void Of_CountsTheWholeList()
    {
        TaskList list = TestLists.Sample(_ids);

        ListProgress progress = ListProgress.Of(list);

        Assert.Equal(1, progress.Done);
        Assert.Equal(3, progress.Total);
        Assert.Equal(1d / 3d, progress.Ratio, 10);
        Assert.False(progress.IsComplete);
    }

    [Fact]
    public void Of_CountsOneCategory()
    {
        TaskList list = TestLists.Sample(_ids);

        ListProgress progress = ListProgress.Of(list.Categories[0]);

        Assert.Equal(1, progress.Done);
        Assert.Equal(2, progress.Total);
    }

    [Fact]
    public void Of_AnEmptyListIsNotComplete()
    {
        ListOperations operations = new(_ids);
        TaskList list = operations.CreateList("Vide");

        ListProgress progress = ListProgress.Of(list);

        Assert.Equal(0, progress.Total);
        Assert.Equal(0d, progress.Ratio);
        Assert.False(progress.IsComplete);
    }

    [Fact]
    public void Of_AFullyTickedListIsComplete()
    {
        TaskList list = TestLists.Sample(_ids);
        ListOperations.SetAllDone(list, true);

        Assert.True(ListProgress.Of(list).IsComplete);
    }
}
