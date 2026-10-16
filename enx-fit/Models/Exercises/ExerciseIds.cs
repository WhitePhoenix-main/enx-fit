using System.Globalization;

namespace enx_fit.Models;

/// <summary>Stable UUIDs for existing exercises, shared by migration and legacy clients.</summary>
public static class ExerciseIds
{
    public const string LegacyPrefix = "e71c15e5-0000-4000-8000-0000";
    public static Guid FromLegacy(int id) => id == 0 ? Guid.Empty :
        Guid.ParseExact(LegacyPrefix + unchecked((uint)id).ToString("x8", CultureInfo.InvariantCulture), "D");

    public static bool TryGetLegacy(Guid id, out int legacyId)
    {
        var text = id.ToString("D");
        if (text.StartsWith(LegacyPrefix, StringComparison.Ordinal) &&
            uint.TryParse(text.AsSpan(LegacyPrefix.Length), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
        {
            legacyId = unchecked((int)value);
            return legacyId != 0;
        }
        legacyId = 0;
        return false;
    }

    public static bool TryParse(string? text, out Guid id)
    {
        if (Guid.TryParse(text, out id)) return true;
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var legacyId)) return false;
        id = FromLegacy(legacyId);
        return true;
    }
}
