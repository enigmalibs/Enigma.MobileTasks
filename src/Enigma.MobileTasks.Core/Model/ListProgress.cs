using System.Collections.Generic;

namespace Enigma.MobileTasks.Core.Model;

/// <summary>
/// How much of a list — or of one category — is ticked off.
/// </summary>
/// <param name="Done">How many items are ticked.</param>
/// <param name="Total">How many items there are.</param>
public readonly record struct ListProgress(int Done, int Total)
{
    /// <summary>
    /// Gets the ticked share, between 0 and 1. An empty list counts as 0, not as finished:
    /// there is nothing to be finished with.
    /// </summary>
    public double Ratio => Total == 0 ? 0d : (double)Done / Total;

    /// <summary>
    /// Gets a value indicating whether the list holds items and every one of them is ticked.
    /// </summary>
    public bool IsComplete => Total > 0 && Done == Total;

    /// <summary>
    /// Counts a whole list.
    /// </summary>
    /// <param name="list">The list to count.</param>
    /// <returns>Its progress.</returns>
    public static ListProgress Of(TaskList list)
        => list is null ? default : Of(list.Categories);

    /// <summary>
    /// Counts one category.
    /// </summary>
    /// <param name="category">The category to count.</param>
    /// <returns>Its progress.</returns>
    public static ListProgress Of(TaskCategory category)
        => category is null ? default : Of([category]);

    private static ListProgress Of(IEnumerable<TaskCategory> categories)
    {
        int done = 0;
        int total = 0;

        foreach (TaskCategory category in categories)
        {
            foreach (TaskItem item in category.Items)
            {
                total++;

                if (item.Done)
                {
                    done++;
                }
            }
        }

        return new ListProgress(done, total);
    }
}
