using System;
using System.Text;
using System.Threading;

namespace Enigma.MobileTasks.Core.Identity;

/// <summary>
/// Produces identifiers shaped exactly like the ones the Listes web app writes —
/// <c>i-mtzxeu79-3-9zk2</c>: a prefix, the current time, a counter and a random tail, all in
/// base 36.
/// </summary>
/// <remarks>
/// The shape is not decoration. Both applications write into the same files, and an identifier
/// that looked foreign would make a file say which program touched it last. Uniqueness comes
/// from the counter within a process and from the time and random tail across processes.
/// </remarks>
public sealed class ListIdGenerator : IListIdGenerator
{
    private const string Digits = "0123456789abcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// The exclusive upper bound of the random tail — 36^4, so the tail is at most four
    /// base-36 digits, as in the web app.
    /// </summary>
    private const int RandomBound = 1679616;

    private int _counter;

    /// <inheritdoc />
    public string NewId(string prefix)
    {
        ArgumentException.ThrowIfNullOrEmpty(prefix);

        int counter = Interlocked.Increment(ref _counter);

        return string.Concat(
            prefix,
            "-",
            ToBase36(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
            "-",
            ToBase36(counter),
            "-",
            ToBase36(Random.Shared.Next(RandomBound)));
    }

    /// <summary>
    /// Writes a non-negative value in base 36, lower case — JavaScript's
    /// <c>Number.prototype.toString(36)</c>.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <returns>Its base-36 representation.</returns>
    private static string ToBase36(long value)
    {
        if (value == 0)
        {
            return "0";
        }

        StringBuilder builder = new(13);

        while (value > 0)
        {
            builder.Insert(0, Digits[(int)(value % 36)]);
            value /= 36;
        }

        return builder.ToString();
    }
}
