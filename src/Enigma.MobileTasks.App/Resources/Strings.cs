namespace Enigma.MobileTasks.App.Resources;

/// <summary>
/// Every piece of wording the interface shows, in one place.
/// </summary>
/// <remarks>
/// English, as Enigma.Tasks is. Constants rather than a resource file: there is one language, and a
/// constant is what a XAML <c>{x:Static}</c> and a ViewModel can both read without a lookup that can
/// silently miss.
/// </remarks>
public static class Strings
{
    /// <summary>The application's name, as the launcher shows it.</summary>
    public const string AppName = "Lists";

    /// <summary>The Lists page's header.</summary>
    public const string ListsHeader = "Lists";

    /// <summary>The Lists page's empty-state title.</summary>
    public const string NoListsTitle = "No lists yet";

    /// <summary>The Lists page's empty-state explanation.</summary>
    public const string NoListsHint =
        "Create a list for the things you pack, shop or check again and again — or import one exported from Enigma.Tasks.";

    /// <summary>A dialog's cancel button.</summary>
    public const string Cancel = "Cancel";

    /// <summary>A dialog's default confirm button.</summary>
    public const string Ok = "OK";

    /// <summary>The message shown when one list file could not be read; {0} is the file, {1} why.</summary>
    public const string OneFileUnreadable = "{0} could not be read and was left untouched: {1}";

    /// <summary>The message shown when several list files could not be read; {0} is how many.</summary>
    public const string SomeFilesUnreadable = "{0} list files could not be read and were left untouched.";

    /// <summary>The message shown when the lists folder itself could not be read; {0} is why.</summary>
    public const string LoadFailed = "Your lists could not be read: {0}";

    /// <summary>The message shown when a scheduled save failed; {0} is the list, {1} why.</summary>
    public const string SaveFailed = "\"{0}\" could not be saved: {1}";
}
