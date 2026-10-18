using System.Text.Json.Serialization;

namespace Enigma.MobileTasks.Core.Settings;

/// <summary>
/// The source-generated serializer for the settings file.
/// </summary>
/// <remarks>
/// Generated rather than reflection-based because an Android Release build is trimmed, and a
/// trimmed application turns reflection-based <c>System.Text.Json</c> off: a
/// <c>JsonSerializer.Serialize&lt;AppSettings&gt;</c> without this context compiles, passes every test
/// on a development machine, and throws on the phone.
/// </remarks>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext
{
}
