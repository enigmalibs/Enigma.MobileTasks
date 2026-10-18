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

    /// <summary>The Lists page's "new list" button.</summary>
    public const string NewList = "New list";

    /// <summary>The title of the dialog that names a new list.</summary>
    public const string NewListTitle = "New list";

    /// <summary>The placeholder of a list-name box.</summary>
    public const string ListNamePlaceholder = "List name";

    /// <summary>The confirm button that creates something.</summary>
    public const string Create = "Create";

    /// <summary>The menu entry and confirm button that renames something.</summary>
    public const string Rename = "Rename";

    /// <summary>The title of the dialog that renames a list.</summary>
    public const string RenameListTitle = "Rename the list";

    /// <summary>The menu entry that duplicates a list.</summary>
    public const string Duplicate = "Duplicate";

    /// <summary>The menu entry and confirm button that deletes something.</summary>
    public const string Delete = "Delete";

    /// <summary>The title of the dialog confirming a list deletion; {0} is the list.</summary>
    public const string DeleteListTitle = "Delete \"{0}\"?";

    /// <summary>The explanation of the dialog confirming a list deletion.</summary>
    public const string DeleteListMessage =
        "The list is removed from this phone. Export it first if you want to keep a copy.";

    /// <summary>The accessible name of a list card's menu button; {0} is the list.</summary>
    public const string ListActionsFor = "Actions for {0}";

    /// <summary>The message after a list was deleted; {0} is the list.</summary>
    public const string ListDeleted = "\"{0}\" deleted.";

    /// <summary>The message after a list was duplicated; {0} is the copy.</summary>
    public const string ListDuplicated = "\"{0}\" created.";

    /// <summary>The message when a list could not be created; {0} is why.</summary>
    public const string CreateFailed = "The list could not be created: {0}";

    /// <summary>The message when a list could not be duplicated; {0} is why.</summary>
    public const string DuplicateFailed = "The list could not be duplicated: {0}";

    /// <summary>The message when a list could not be renamed; {0} is why.</summary>
    public const string RenameFailed = "The list could not be renamed: {0}";

    /// <summary>The message when a list could not be deleted; {0} is why.</summary>
    public const string DeleteFailed = "The list could not be deleted: {0}";

    /// <summary>The Checklist page's empty-state title.</summary>
    public const string EmptyListTitle = "This list is empty";

    /// <summary>The Checklist page's empty-state explanation.</summary>
    public const string EmptyListHint = "Add a first category — \"Clothes\", \"Pharmacy\" — then the items under it.";

    /// <summary>The edit-mode toggle's accessible name.</summary>
    public const string EditList = "Edit the list";

    /// <summary>The accessible name of the Checklist page's menu button.</summary>
    public const string MoreActions = "More actions";

    /// <summary>The menu entry that unticks every item of the list or of a category.</summary>
    public const string UntickAll = "Untick everything";

    /// <summary>The menu entry that renames the open list.</summary>
    public const string RenameListMenu = "Rename the list";

    /// <summary>The button that adds a category.</summary>
    public const string AddCategory = "Add a category";

    /// <summary>The title of the dialog that names a new category.</summary>
    public const string AddCategoryTitle = "New category";

    /// <summary>The placeholder of a category-name box.</summary>
    public const string CategoryNamePlaceholder = "Category name";

    /// <summary>The title of the dialog that renames a category.</summary>
    public const string RenameCategoryTitle = "Rename the category";

    /// <summary>The title of the dialog that renames an item.</summary>
    public const string RenameItemTitle = "Rename the item";

    /// <summary>The placeholder of an item box.</summary>
    public const string ItemPlaceholder = "Add an item";

    /// <summary>The accessible name of the quick-add button.</summary>
    public const string AddItem = "Add the item";

    /// <summary>The menu entry that moves something up.</summary>
    public const string MoveUp = "Move up";

    /// <summary>The menu entry that moves something down.</summary>
    public const string MoveDown = "Move down";

    /// <summary>The title of the dialog confirming a non-empty category deletion; {0} is the category.</summary>
    public const string DeleteCategoryTitle = "Delete \"{0}\"?";

    /// <summary>The explanation of that dialog; {0} is how many items it holds.</summary>
    public const string DeleteCategoryMessage = "It still holds {0} items. You can undo this right after.";

    /// <summary>The accessible name of a category's menu button; {0} is the category.</summary>
    public const string CategoryActionsFor = "Actions for {0}";

    /// <summary>The accessible name of an item's menu button; {0} is the item.</summary>
    public const string ItemActionsFor = "Actions for {0}";

    /// <summary>The message after something was deleted; {0} is what.</summary>
    public const string Deleted = "\"{0}\" deleted.";

    /// <summary>The message after a deletion was undone; {0} is what.</summary>
    public const string Restored = "\"{0}\" restored.";

    /// <summary>The snackbar action that puts a deletion back.</summary>
    public const string Undo = "Undo";

    /// <summary>The menu entry and button that exports a list.</summary>
    public const string Export = "Export as JSON";

    /// <summary>The title of the system picker that saves an exported list.</summary>
    public const string ExportPickerTitle = "Export the list";

    /// <summary>The button that imports lists.</summary>
    public const string Import = "Import lists";

    /// <summary>The empty state's import button.</summary>
    public const string ImportFromFile = "Import from a file";

    /// <summary>The title of the system picker that opens lists to import.</summary>
    public const string ImportPickerTitle = "Import lists";

    /// <summary>The name of the JSON file type in the pickers.</summary>
    public const string JsonFileType = "JSON list";

    /// <summary>The message after an export; {0} is the list, {1} the file written.</summary>
    public const string Exported = "\"{0}\" exported as {1}.";

    /// <summary>The message when an export failed; {0} is why.</summary>
    public const string ExportFailed = "The list could not be exported: {0}";

    /// <summary>The message after one list was imported; {0} is the list.</summary>
    public const string ImportedOne = "\"{0}\" imported.";

    /// <summary>The message after several lists were imported; {0} is how many.</summary>
    public const string ImportedMany = "{0} lists imported.";

    /// <summary>The message about one file that could not be imported; {0} is the file, {1} why.</summary>
    public const string ImportRefused = "{0} could not be imported: {1}";

    /// <summary>The message about several files that could not be imported; {0} is how many.</summary>
    public const string ImportRefusedMany = "{0} files could not be imported — they are not lists this app can read.";
}
