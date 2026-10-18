using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.MobileTasks.Core.Model;

namespace Enigma.MobileTasks.App.ViewModels.Pages;

/// <summary>
/// One category of the open list, as its card on the Checklist page shows it.
/// </summary>
public sealed class CategoryCardViewModel : ObservableObject
{
    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="category">The category this card shows.</param>
    public CategoryCardViewModel(TaskCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);

        Category = category;
        Refresh();
    }

    /// <summary>
    /// Gets the category this card shows.
    /// </summary>
    public TaskCategory Category { get; }

    /// <summary>
    /// Gets the category's name.
    /// </summary>
    public string Name { get; private set => SetProperty(ref field, value); } = string.Empty;

    /// <summary>
    /// Gets the category's progress as text — "3 / 18".
    /// </summary>
    public string ProgressText { get; private set => SetProperty(ref field, value); } = string.Empty;

    /// <summary>
    /// Re-reads the category.
    /// </summary>
    public void Refresh()
    {
        ListProgress progress = ListProgress.Of(Category);

        Name = Category.Name;
        ProgressText = string.Create(CultureInfo.CurrentCulture, $"{progress.Done} / {progress.Total}");
    }
}
