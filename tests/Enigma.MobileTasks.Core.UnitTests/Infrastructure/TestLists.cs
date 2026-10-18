using System;
using System.IO;
using Enigma.MobileTasks.Core.Identity;
using Enigma.MobileTasks.Core.Model;
using Enigma.MobileTasks.Core.Operations;

namespace Enigma.MobileTasks.Core.UnitTests.Infrastructure;

/// <summary>
/// The reference material the model and format tests work from.
/// </summary>
public static class TestLists
{
    /// <summary>
    /// Gets the path of the reference list, as the Listes web app exported it — the very file
    /// Enigma.Tasks proves its own format against, copied byte for byte.
    /// </summary>
    public static string JudithJsonPath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "liste-judith.json");

    /// <summary>
    /// Reads the reference list's text exactly as it sits on disk — no BOM, LF endings, no
    /// trailing newline.
    /// </summary>
    /// <returns>The file's text.</returns>
    public static string ReadJudithJson() => File.ReadAllText(JudithJsonPath);

    /// <summary>
    /// Builds a small list with known identifiers: two categories, three items, one of them
    /// ticked.
    /// </summary>
    /// <param name="ids">The generator to mint its identifiers with.</param>
    /// <returns>The list.</returns>
    public static TaskList Sample(IListIdGenerator ids)
    {
        ListOperations operations = new(ids);
        TaskList list = operations.CreateList("Essai");

        TaskCategory first = operations.AddCategory(list, "Habits");
        TaskCategory second = operations.AddCategory(list, "Dormir");

        operations.AddItem(list, first.Id, "Bodys");
        operations.AddItem(list, first.Id, "Chaussettes");
        operations.AddItem(list, second.Id, "Doudou");

        ListOperations.Toggle(list, first.Items[1].Id, true);

        return list;
    }
}
