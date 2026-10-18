using System.Linq;
using Enigma.MobileTasks.Core.Model;
using Enigma.MobileTasks.Core.Operations;
using Enigma.MobileTasks.Core.UnitTests.Infrastructure;
using Xunit;

namespace Enigma.MobileTasks.Core.UnitTests;

/// <summary>
/// The rules every change to a list obeys — the port of the web app's model, checked against the
/// behaviour that app has.
/// </summary>
public sealed class ListOperationsTests
{
    private readonly SequentialIdGenerator _ids = new();
    private readonly ListOperations _operations;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    public ListOperationsTests() => _operations = new ListOperations(_ids);

    /* ------------------------------------------------------------------ creation */

    [Fact]
    public void CreateList_FallsBackToTheDefaultName()
    {
        Assert.Equal(ListSchema.DefaultListName, _operations.CreateList("   ").Name);
        Assert.Equal("Vacances", _operations.CreateList("  Vacances  ").Name);
    }

    [Fact]
    public void AddCategory_AppendsByDefaultAndInsertsWhenAsked()
    {
        TaskList list = _operations.CreateList("Essai");

        _operations.AddCategory(list, "Un");
        _operations.AddCategory(list, "Deux");
        _operations.AddCategory(list, "Zéro", 0);

        Assert.Equal(["Zéro", "Un", "Deux"], list.Categories.Select(category => category.Name));
    }

    [Fact]
    public void AddCategory_GivesEveryCategoryItsOwnIdentifier()
    {
        TaskList list = _operations.CreateList("Essai");

        TaskCategory first = _operations.AddCategory(list, "Un");
        TaskCategory second = _operations.AddCategory(list, "Deux");

        Assert.NotEqual(first.Id, second.Id);
        Assert.StartsWith("c-", first.Id, System.StringComparison.Ordinal);
    }

    [Fact]
    public void AddItem_RefusesBlankTextAndAnUnknownCategory()
    {
        TaskList list = _operations.CreateList("Essai");
        TaskCategory category = _operations.AddCategory(list, "Un");

        Assert.Null(_operations.AddItem(list, category.Id, "   "));
        Assert.Null(_operations.AddItem(list, "nope", "Bodys"));
        Assert.Empty(category.Items);
    }

    /* ----------------------------------------------------------------- renaming */

    [Fact]
    public void Rename_ReturnsThePreviousValueOnlyWhenSomethingChanged()
    {
        TaskList list = TestLists.Sample(_ids);
        TaskCategory category = list.Categories[0];
        TaskItem item = category.Items[0];

        Assert.Equal("Essai", ListOperations.RenameList(list, "Voyage"));
        Assert.Null(ListOperations.RenameList(list, "  Voyage  "));
        Assert.Null(ListOperations.RenameList(list, "   "));
        Assert.Equal("Voyage", list.Name);

        Assert.Equal("Habits", ListOperations.RenameCategory(list, category.Id, "Vêtements"));
        Assert.Null(ListOperations.RenameCategory(list, "nope", "Rien"));

        Assert.Equal("Bodys", ListOperations.RenameItem(list, item.Id, "Pyjamas"));
        Assert.Null(ListOperations.RenameItem(list, item.Id, " "));
        Assert.Equal("Pyjamas", item.Text);
    }

    /* ------------------------------------------------------ removal and restore */

    [Fact]
    public void RemoveItem_ThenRestore_PutsItBackWhereItWas()
    {
        TaskList list = TestLists.Sample(_ids);
        TaskCategory category = list.Categories[0];
        string before = Describe(list);

        ListRemoval removal = Assert.IsType<ItemRemoval>(ListOperations.RemoveItem(list, category.Items[0].Id));

        Assert.Single(category.Items);
        Assert.True(ListOperations.Restore(list, removal));
        Assert.Equal(before, Describe(list));
    }

    [Fact]
    public void RemoveCategory_ThenRestore_PutsTheWholeCardBack()
    {
        TaskList list = TestLists.Sample(_ids);
        string before = Describe(list);

        ListRemoval removal = Assert.IsType<CategoryRemoval>(ListOperations.RemoveCategory(list, list.Categories[0].Id));

        Assert.Single(list.Categories);
        Assert.Equal("Habits", removal.Label);
        Assert.True(ListOperations.Restore(list, removal));
        Assert.Equal(before, Describe(list));
    }

    [Fact]
    public void Restore_OfAnItemWhoseCategoryIsGone_DoesNothing()
    {
        TaskList list = TestLists.Sample(_ids);
        ListRemoval? removal = ListOperations.RemoveItem(list, list.Categories[0].Items[0].Id);
        ListOperations.RemoveCategory(list, list.Categories[0].Id);

        Assert.False(ListOperations.Restore(list, removal));
    }

    [Fact]
    public void Remove_OfSomethingUnknown_ReturnsNothingToUndo()
    {
        TaskList list = TestLists.Sample(_ids);

        Assert.Null(ListOperations.RemoveItem(list, "nope"));
        Assert.Null(ListOperations.RemoveCategory(list, "nope"));
        Assert.False(ListOperations.Restore(list, null));
    }

    /* ---------------------------------------------------------------- ticking */

    [Fact]
    public void Toggle_FlipsOrSetsTheState()
    {
        TaskList list = TestLists.Sample(_ids);
        string id = list.Categories[0].Items[0].Id;

        Assert.True(ListOperations.Toggle(list, id));
        Assert.False(ListOperations.Toggle(list, id));
        Assert.True(ListOperations.Toggle(list, id, true));
        Assert.True(ListOperations.Toggle(list, id, true));
        Assert.Null(ListOperations.Toggle(list, "nope"));
    }

    [Fact]
    public void SetAllDone_CountsWhatItActuallyChanged()
    {
        TaskList list = TestLists.Sample(_ids);

        Assert.Equal(2, ListOperations.SetAllDone(list, true));
        Assert.Equal(0, ListOperations.SetAllDone(list, true));
        Assert.Equal(3, ListOperations.SetAllDone(list, false));
    }

    [Fact]
    public void SetAllDone_CanBeLimitedToOneCategory()
    {
        TaskList list = TestLists.Sample(_ids);

        Assert.Equal(1, ListOperations.SetAllDone(list, true, list.Categories[0].Id));
        Assert.Equal(0, ListOperations.SetAllDone(list, true, "nope"));
        Assert.True(list.Categories[0].Items.All(item => item.Done));
        Assert.False(list.Categories[1].Items[0].Done);
    }

    /* -------------------------------------------------------------- reordering */

    [Fact]
    public void MoveCategory_IsANoOpAtTheEdges()
    {
        TaskList list = TestLists.Sample(_ids);
        string first = list.Categories[0].Id;

        Assert.False(ListOperations.MoveCategoryBy(list, first, -1));
        Assert.True(ListOperations.MoveCategoryBy(list, first, 1));
        Assert.False(ListOperations.MoveCategoryBy(list, first, 1));
        Assert.Equal(["Dormir", "Habits"], list.Categories.Select(category => category.Name));
    }

    [Fact]
    public void MoveItem_WithinItsCategory()
    {
        TaskList list = TestLists.Sample(_ids);
        TaskCategory category = list.Categories[0];

        Assert.True(ListOperations.MoveItem(list, category.Items[0].Id, category.Id, 1));
        Assert.Equal(["Chaussettes", "Bodys"], category.Items.Select(item => item.Text));
        Assert.False(ListOperations.MoveItem(list, category.Items[1].Id, category.Id, 1));
    }

    [Fact]
    public void MoveItem_IntoAnotherCategory()
    {
        TaskList list = TestLists.Sample(_ids);
        TaskCategory habits = list.Categories[0];
        TaskCategory dormir = list.Categories[1];

        Assert.True(ListOperations.MoveItem(list, habits.Items[0].Id, dormir.Id, 0));

        Assert.Equal(["Chaussettes"], habits.Items.Select(item => item.Text));
        Assert.Equal(["Bodys", "Doudou"], dormir.Items.Select(item => item.Text));
    }

    [Fact]
    public void MoveItemBy_HopsIntoTheNeighbouringCategoryAtABound()
    {
        TaskList list = TestLists.Sample(_ids);
        TaskCategory habits = list.Categories[0];
        TaskCategory dormir = list.Categories[1];
        string bodys = habits.Items[0].Id;

        // Up from the first row of the first category: nowhere to go.
        Assert.False(ListOperations.MoveItemBy(list, bodys, -1));

        Assert.True(ListOperations.MoveItemBy(list, bodys, 1));
        Assert.True(ListOperations.MoveItemBy(list, bodys, 1));
        Assert.Equal(["Bodys", "Doudou"], dormir.Items.Select(item => item.Text));

        Assert.True(ListOperations.MoveItemBy(list, bodys, -1));
        Assert.Equal(["Chaussettes", "Bodys"], habits.Items.Select(item => item.Text));
    }

    [Fact]
    public void MoveItemBy_RefusesAnythingButOneStep()
    {
        TaskList list = TestLists.Sample(_ids);

        Assert.False(ListOperations.MoveItemBy(list, list.Categories[0].Items[0].Id, 2));
        Assert.False(ListOperations.MoveItemBy(list, "nope", 1));
    }

    [Fact]
    public void MoveItemToSlot_AccountsForTheGapTheRowLeavesBehind()
    {
        TaskList list = _operations.CreateList("Essai");
        TaskCategory category = _operations.AddCategory(list, "Un");
        _operations.AddItem(list, category.Id, "A");
        _operations.AddItem(list, category.Id, "B");
        _operations.AddItem(list, category.Id, "C");

        // Dropping A into the gap below B is slot 2 on screen, which is index 1 once A is out.
        Assert.True(ListOperations.MoveItemToSlot(list, category.Items[0].Id, category.Id, 2));
        Assert.Equal(["B", "A", "C"], category.Items.Select(item => item.Text));

        // Dropping a row back into its own gap changes nothing.
        Assert.False(ListOperations.MoveItemToSlot(list, category.Items[1].Id, category.Id, 1));
        Assert.False(ListOperations.MoveItemToSlot(list, category.Items[1].Id, category.Id, 2));
    }

    [Fact]
    public void MoveCategoryToSlot_UsesTheSameConvention()
    {
        TaskList list = _operations.CreateList("Essai");
        _operations.AddCategory(list, "A");
        _operations.AddCategory(list, "B");
        _operations.AddCategory(list, "C");

        Assert.True(ListOperations.MoveCategoryToSlot(list, list.Categories[0].Id, 2));
        Assert.Equal(["B", "A", "C"], list.Categories.Select(category => category.Name));

        Assert.False(ListOperations.MoveCategoryToSlot(list, list.Categories[1].Id, 1));
        Assert.False(ListOperations.MoveCategoryToSlot(list, "nope", 0));
    }

    /* ------------------------------------------------------------------- copies */

    [Fact]
    public void Clone_CopiesEverythingAndKeepsTheIdentifiers()
    {
        TaskList list = TestLists.Sample(_ids);

        TaskList copy = _operations.Clone(list);

        Assert.Equal(Describe(list), Describe(copy));
        Assert.Equal(list.Categories[0].Id, copy.Categories[0].Id);
        Assert.NotSame(list.Categories[0], copy.Categories[0]);
    }

    [Fact]
    public void Clone_WithNewIds_SharesNoIdentifierWithTheOriginal()
    {
        TaskList list = TestLists.Sample(_ids);

        TaskList copy = _operations.Clone(list, withNewIds: true);

        // Identifiers are what a duplicate must not share, so the shapes are compared without
        // them; the intersection below is the other half of the statement.
        Assert.Equal(Shape(list), Shape(copy));
        Assert.Empty(Identifiers(list).Intersect(Identifiers(copy)));
    }

    /* ------------------------------------------------------------------ lookups */

    [Fact]
    public void Locate_SaysWhereAnItemSits()
    {
        TaskList list = TestLists.Sample(_ids);

        ItemLocation location = Assert.IsType<ItemLocation>(
            ListOperations.Locate(list, list.Categories[1].Items[0].Id));

        Assert.Equal(1, location.CategoryIndex);
        Assert.Equal(0, location.Index);
        Assert.Equal("Doudou", location.Item.Text);
        Assert.Null(ListOperations.Locate(list, "nope"));
        Assert.Null(ListOperations.Locate(list, null));
    }

    /// <summary>
    /// Describes the whole list so a whole-shape comparison reads as one assertion. Identifiers
    /// are part of the text, so a copy that loses one fails here.
    /// </summary>
    /// <param name="list">The list to describe.</param>
    /// <returns>One line per category and item, identifiers included.</returns>
    private static string Describe(TaskList list)
        => string.Join(
            "\n",
            list.Categories.SelectMany(category =>
                new[] { category.Id + " " + category.Name }
                    .Concat(category.Items.Select(item => "  " + item.Id + " " + item.Text + " " + item.Done))));

    /// <summary>
    /// Describes a list without its identifiers: names, labels and ticks only.
    /// </summary>
    /// <param name="list">The list to describe.</param>
    /// <returns>Its shape.</returns>
    private static string Shape(TaskList list)
        => string.Join(
            "\n",
            list.Categories.Select(category => string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"{category.Name}: {string.Join(", ", category.Items.Select(item => $"{item.Text}={item.Done}"))}")));

    private static System.Collections.Generic.IEnumerable<string> Identifiers(TaskList list)
        => list.Categories
            .SelectMany<TaskCategory, string>(category => [category.Id, .. category.Items.Select(item => item.Id)]);
}
