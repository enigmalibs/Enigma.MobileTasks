using System;
using System.IO;

namespace Enigma.MobileTasks.App.UnitTests.Infrastructure;

/// <summary>
/// The reference list the UI tests load — the same file Enigma.Tasks proves its format against.
/// </summary>
public static class TestLists
{
    /// <summary>
    /// Gets the path of the reference list.
    /// </summary>
    public static string JudithJsonPath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "liste-judith.json");

    /// <summary>
    /// Reads the reference list's text exactly as it sits on disk.
    /// </summary>
    /// <returns>The file's text.</returns>
    public static string ReadJudithJson() => File.ReadAllText(JudithJsonPath);
}
