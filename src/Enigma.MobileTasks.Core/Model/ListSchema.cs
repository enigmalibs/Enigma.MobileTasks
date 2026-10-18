namespace Enigma.MobileTasks.Core.Model;

/// <summary>
/// The list file format this application reads and writes.
/// </summary>
/// <remarks>
/// <see cref="Version"/> is the compatibility contract with the Enigma.Tasks desktop application
/// and the Listes web app, and cannot be changed without changing the file format. The three default names are not: they are the
/// wording this application substitutes for a missing or blank name, they are written into a
/// file as ordinary names, and a file carrying either application's defaults opens unchanged in
/// the other. They are in English because the interface is.
/// </remarks>
public static class ListSchema
{
    /// <summary>
    /// The schema version written into every file, and the highest one that can be read.
    /// </summary>
    public const int Version = 1;

    /// <summary>
    /// The name given to a list that has none.
    /// </summary>
    public const string DefaultListName = "New list";

    /// <summary>
    /// The name given to a category that has none.
    /// </summary>
    public const string DefaultCategoryName = "New category";

    /// <summary>
    /// The category items get when a Markdown file lists them before its first heading.
    /// </summary>
    public const string LooseCategoryName = "Uncategorized";
}
