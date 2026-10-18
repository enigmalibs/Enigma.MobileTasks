using Enigma.MobileTasks.Core.Model;
using Xunit;

namespace Enigma.MobileTasks.Core.UnitTests;

/// <summary>
/// Guards the constants that define the file format, and doubles as the smoke test proving the
/// solution builds and the test runner works.
/// </summary>
public sealed class ListSchemaTests
{
    [Fact]
    public void Version_IsTheFormatTheWebAppWrites()
        => Assert.Equal(1, ListSchema.Version);

    [Fact]
    public void Defaults_AreTheInterfaceWording()
    {
        Assert.Equal("New list", ListSchema.DefaultListName);
        Assert.Equal("New category", ListSchema.DefaultCategoryName);
        Assert.Equal("Uncategorized", ListSchema.LooseCategoryName);
    }
}
