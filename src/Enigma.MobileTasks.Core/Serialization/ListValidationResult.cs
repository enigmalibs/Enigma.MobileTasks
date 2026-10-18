using System.Collections.Generic;

namespace Enigma.MobileTasks.Core.Serialization;

/// <summary>
/// The outcome of validating a payload.
/// </summary>
/// <param name="Errors">Everything wrong with it, in the order it was found.</param>
public sealed record ListValidationResult(IReadOnlyList<ListValidationError> Errors)
{
    /// <summary>
    /// A result with nothing wrong.
    /// </summary>
    public static readonly ListValidationResult Ok = new([]);

    /// <summary>
    /// Gets a value indicating whether the payload can be read.
    /// </summary>
    public bool IsValid => Errors.Count == 0;
}
