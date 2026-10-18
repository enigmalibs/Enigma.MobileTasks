using System.Linq;
using System.Threading.Tasks;
using Enigma.MobileTasks.App.Services;
using Enigma.MobileTasks.App.UnitTests.Infrastructure;
using Enigma.MobileTasks.Core.Model;
using Enigma.MobileTasks.Core.Workspace;
using Xunit;

namespace Enigma.MobileTasks.App.UnitTests;

/// <summary>
/// The editor's edges: no open list, changes that change nothing, and an undo that must not cross
/// lists.
/// </summary>
public sealed class ListEditorTests
{
    [Fact]
    public void WithNoOpenList_NothingChanges()
    {
        using TempApp app = new(RecordingNavigation.Register);
        IListEditor editor = app.Get<IListEditor>();

        Assert.Null(editor.AddCategory("Habits"));
        Assert.Equal(0, editor.SetAllDone(null, false));
        Assert.False(editor.Undo());
    }

    [Fact]
    public async Task ATickThatChangesNothing_IsNotAChange()
    {
        using TempApp app = new(RecordingNavigation.Register);
        IListEditor editor = app.Get<IListEditor>();
        TaskItem item = await OpenListWithOneItemAsync(app);
        int structural = 0;
        editor.StructureChanged += (_, _) => structural++;

        Assert.True(editor.Toggle(item, true));
        Assert.False(editor.Toggle(item, true));
        Assert.Equal(0, structural);
    }

    [Fact]
    public async Task BlankNamesAndTexts_AreRefused()
    {
        using TempApp app = new(RecordingNavigation.Register);
        IListEditor editor = app.Get<IListEditor>();
        TaskItem item = await OpenListWithOneItemAsync(app);
        TaskCategory category = app.Get<IActiveListService>().Current!.List.Categories[0];

        Assert.Null(editor.AddItem(category, "   "));
        Assert.False(editor.RenameItem(item, " "));
        Assert.False(editor.RenameCategory(category, ""));
    }

    [Fact]
    public async Task OpeningAnotherList_ForgetsTheUndo()
    {
        using TempApp app = new(RecordingNavigation.Register);
        IListEditor editor = app.Get<IListEditor>();
        IActiveListService lists = app.Get<IActiveListService>();
        TaskItem item = await OpenListWithOneItemAsync(app);

        Assert.True(editor.RemoveItem(item));
        Assert.True(editor.CanUndo);

        lists.Open(null);

        Assert.False(editor.CanUndo);
    }

    private static async Task<TaskItem> OpenListWithOneItemAsync(TempApp app)
    {
        IActiveListService lists = app.Get<IActiveListService>();
        WorkspaceEntry entry = (await app.Get<IListActions>().CreateAsync("Essai", TestContext.Current.CancellationToken))!;
        lists.Open(entry);

        IListEditor editor = app.Get<IListEditor>();
        TaskCategory category = editor.AddCategory("Habits")!;

        return editor.AddItem(category, "Bodys")!;
    }
}
