using System.Globalization;
using Enigma.MobileTasks.Core.Identity;

namespace Enigma.MobileTasks.Core.UnitTests.Infrastructure;

/// <summary>
/// Hands out predictable identifiers — <c>c-1</c>, <c>i-2</c>, … — so a test can assert on what
/// a normalisation or a duplication produced instead of only on its shape.
/// </summary>
public sealed class SequentialIdGenerator : IListIdGenerator
{
    private int _counter;

    /// <summary>
    /// Gets how many identifiers have been handed out.
    /// </summary>
    public int Count => _counter;

    /// <inheritdoc />
    public string NewId(string prefix)
    {
        _counter++;

        return string.Create(CultureInfo.InvariantCulture, $"{prefix}-{_counter}");
    }
}
