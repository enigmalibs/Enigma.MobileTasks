namespace Enigma.MobileTasks.Core.Identity;

/// <summary>
/// Mints the identifiers categories and items carry.
/// </summary>
/// <remarks>
/// An interface rather than a static helper because the generator holds a counter, and because
/// a test that wants to read its own output needs to hand out predictable identifiers.
/// </remarks>
public interface IListIdGenerator
{
    /// <summary>
    /// Produces an identifier.
    /// </summary>
    /// <param name="prefix">
    /// What the identifier starts with — <c>c</c> for a category, <c>i</c> for an item,
    /// <c>l</c> for a list.
    /// </param>
    /// <returns>An identifier that has not been handed out before.</returns>
    string NewId(string prefix);
}
