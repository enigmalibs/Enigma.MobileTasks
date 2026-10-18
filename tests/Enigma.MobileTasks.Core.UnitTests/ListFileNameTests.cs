using System.IO;
using Enigma.MobileTasks.Core.UnitTests.Infrastructure;
using Enigma.MobileTasks.Core.Workspace;
using Xunit;

namespace Enigma.MobileTasks.Core.UnitTests;

/// <summary>
/// The rule that turns a list's name into the file it lives in.
/// </summary>
public sealed class ListFileNameTests
{
    [Theory]
    [InlineData("Liste Judith", "liste-judith")]
    [InlineData("Écharpe & Gants", "echarpe-gants")]
    [InlineData("  Vacances  d'été  ", "vacances-d-ete")]
    [InlineData("Été 2026", "ete-2026")]
    [InlineData("A/B\\C:D", "a-b-c-d")]
    [InlineData("???", "list")]
    [InlineData("", "list")]
    [InlineData(null, "list")]
    public void Slug_FoldsANameDownToSomethingEveryFileSystemAccepts(string? name, string expected)
        => Assert.Equal(expected, ListFileName.Slug(name));

    [Fact]
    public void Suggest_AppendsTheExtension()
    {
        Assert.Equal("liste-judith.json", ListFileName.Suggest("Liste Judith"));
        Assert.Equal("liste-judith.md", ListFileName.Suggest("Liste Judith", ListFileName.MarkdownExtension));
    }

    [Fact]
    public void Unique_NumbersTheOnesThatWouldCollide()
    {
        using TempWorkspace workspace = new();

        string first = ListFileName.Unique(workspace.Root, "Liste Judith");
        File.WriteAllText(first, "{}");

        string second = ListFileName.Unique(workspace.Root, "Liste Judith");
        File.WriteAllText(second, "{}");

        string third = ListFileName.Unique(workspace.Root, "Liste Judith");

        Assert.Equal("liste-judith.json", Path.GetFileName(first));
        Assert.Equal("liste-judith-2.json", Path.GetFileName(second));
        Assert.Equal("liste-judith-3.json", Path.GetFileName(third));
    }

    [Fact]
    public void Unique_DoesNotCountTheFileBeingRenamed()
    {
        using TempWorkspace workspace = new();

        string existing = ListFileName.Unique(workspace.Root, "Liste Judith");
        File.WriteAllText(existing, "{}");

        Assert.Equal(existing, ListFileName.Unique(workspace.Root, "LISTE judith", ignore: existing));
    }
}
