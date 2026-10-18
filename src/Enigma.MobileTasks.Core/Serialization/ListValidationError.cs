namespace Enigma.MobileTasks.Core.Serialization;

/// <summary>
/// One reason a payload cannot be read as a list.
/// </summary>
/// <param name="Code">
/// A stable machine-readable code — the same vocabulary the Listes web app uses, so the two
/// applications describe a broken file the same way.
/// </param>
/// <param name="Path">Where in the payload the problem is, e.g. <c>categories[2].items[0].text</c>.</param>
/// <param name="Message">What to tell the user, in English.</param>
public sealed record ListValidationError(string Code, string Path, string Message);
