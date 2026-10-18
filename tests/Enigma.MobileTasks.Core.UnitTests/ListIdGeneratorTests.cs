using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Enigma.MobileTasks.Core.Identity;
using Xunit;

namespace Enigma.MobileTasks.Core.UnitTests;

/// <summary>
/// The identifiers a list written on the phone carries — shaped like the ones Enigma.Tasks and the
/// Listes web app write, so a file does not say which program touched it last.
/// </summary>
public sealed partial class ListIdGeneratorTests
{
    private readonly ListIdGenerator _generator = new();

    [Theory]
    [InlineData("c")]
    [InlineData("i")]
    [InlineData("l")]
    public void NewId_IsAPrefixTimeCounterAndTailInBase36(string prefix)
    {
        string id = _generator.NewId(prefix);

        Assert.Matches(IdPattern(), id);
        Assert.StartsWith(prefix + "-", id, StringComparison.Ordinal);
    }

    [Fact]
    public void NewId_NeverRepeatsItself()
    {
        List<string> ids = [.. Enumerable.Range(0, 1000).Select(_ => _generator.NewId("i"))];

        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void NewId_RefusesAnEmptyPrefix()
        => Assert.Throws<ArgumentException>(() => _generator.NewId(string.Empty));

    [GeneratedRegex("^[cil]-[0-9a-z]+-[0-9a-z]+-[0-9a-z]+$")]
    private static partial Regex IdPattern();
}
