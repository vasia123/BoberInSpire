using System;
using System.Linq;

namespace FirstMod.Patches;

/// <summary>Shared filters for card titles exported to the overlay (rewards + merchant).</summary>
internal static class CardTitleHeuristics
{
    internal static bool IsPlausibleCardTitle(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();
        if (s.Length is < 2 or > 48) return false;
        if (LooksLikeNoise(s)) return false;
        if (s.Equals("True", StringComparison.OrdinalIgnoreCase) || s.Equals("False", StringComparison.OrdinalIgnoreCase))
            return false;
        if (s.Equals("Gold", StringComparison.OrdinalIgnoreCase) || s.Equals("Potion", StringComparison.OrdinalIgnoreCase))
            return false;
        if (s.Equals("Card", StringComparison.OrdinalIgnoreCase))
            return false;
        if (int.TryParse(s, out _) || long.TryParse(s, out _))
            return false;
        if (s.Contains('(') && s.Contains(')'))
            return false;
        if (s.StartsWith("POTION.", StringComparison.OrdinalIgnoreCase))
            return false;
        if (s.StartsWith("CHARACTER.", StringComparison.OrdinalIgnoreCase))
            return false;
        if (s.Contains("MONSTER.", StringComparison.OrdinalIgnoreCase))
            return false;
        var letterCount = s.Count(char.IsLetter);
        if (letterCount < 2)
            return false;
        return true;
    }

    private static bool LooksLikeNoise(string s)
    {
        if (s.Length > 80) return true;
        if (s.Contains("MegaCrit", StringComparison.Ordinal)) return true;
        if (s.StartsWith("System.", StringComparison.Ordinal)) return true;
        return false;
    }
}
