using System;
using System.Collections.Generic;

namespace Enigma.MobileTasks.Core.Serialization;

/// <summary>
/// Thrown when a payload cannot be read as a list.
/// </summary>
/// <remarks>
/// The message is the first error's message, because that is the one a user is shown; the whole
/// list stays available on <see cref="Errors"/> for a detailed report.
/// </remarks>
public sealed class ListValidationException : Exception
{
    /// <summary>
    /// Initialises a new instance from a failed validation.
    /// </summary>
    /// <param name="errors">What is wrong with the payload; must not be empty.</param>
    public ListValidationException(IReadOnlyList<ListValidationError> errors)
        : base(First(errors).Message)
    {
        Errors = errors;
    }

    /// <summary>
    /// Initialises a new instance from a single error.
    /// </summary>
    /// <param name="error">What is wrong with the payload.</param>
    public ListValidationException(ListValidationError error)
        : this([error ?? throw new ArgumentNullException(nameof(error))])
    {
    }

    /// <summary>
    /// Gets everything wrong with the payload.
    /// </summary>
    public IReadOnlyList<ListValidationError> Errors { get; }

    private static ListValidationError First(IReadOnlyList<ListValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Count == 0)
        {
            throw new ArgumentException("A validation exception needs at least one error.", nameof(errors));
        }

        return errors[0];
    }
}
