using System.Reflection;
using System.Text.Json;
using MegaCrit.Sts2.Core.Logging;

namespace FirstMod;

/// <summary>
/// Loads embedded tier-list JSON files and provides per-card tier lookups.
/// Two sources: Mobalytics and slaythespire-2.com wiki.
/// </summary>
public static class TierData
{
    // character (lowercase) → card name (lowercase) → tier letter
    private static Dictionary<string, Dictionary<string, string>> _mobaIndex = new();
    private static Dictionary<string, Dictionary<string, string>> _wikiIndex = new();

    private static readonly Dictionary<string, int> TierScore = new()
    {
        ["S"] = 92, ["A"] = 78, ["B"] = 64, ["C"] = 50, ["D"] = 36, ["F"] = 22,
    };

    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            _mobaIndex = LoadEmbeddedTierList("mobalytics_cards.json");
            Log.Info($"[BoberInSpire] TierData: loaded Mobalytics ({_mobaIndex.Count} characters)");
        }
        catch (Exception ex) { Log.Error($"[BoberInSpire] TierData mobalytics load: {ex.Message}"); }

        try
        {
            _wikiIndex = LoadEmbeddedTierList("slaythespire2_com_cards.json");
            Log.Info($"[BoberInSpire] TierData: loaded Wiki ({_wikiIndex.Count} characters)");
        }
        catch (Exception ex) { Log.Error($"[BoberInSpire] TierData wiki load: {ex.Message}"); }
    }

    public record TierResult(string? MobaTier, string? WikiTier, string BlendedTier, int BlendedScore);

    public static TierResult GetTiers(string character, string cardName)
    {
        var charKey = NormalizeCharacter(character);
        var cardKey = NormalizeCardName(cardName);

        var mobaTier = LookupTier(_mobaIndex, charKey, cardKey);
        var wikiTier = LookupTier(_wikiIndex, charKey, cardKey);

        // Fallback: if character unknown, search all characters
        if (mobaTier == null && wikiTier == null && (string.IsNullOrEmpty(charKey) || charKey == "unknown"))
        {
            foreach (var ch in _mobaIndex.Keys)
            {
                mobaTier = LookupTier(_mobaIndex, ch, cardKey);
                if (mobaTier != null) break;
            }
            foreach (var ch in _wikiIndex.Keys)
            {
                wikiTier = LookupTier(_wikiIndex, ch, cardKey);
                if (wikiTier != null) break;
            }
        }

        int score;
        if (mobaTier != null && wikiTier != null)
            score = (TierScore.GetValueOrDefault(mobaTier, 50) + TierScore.GetValueOrDefault(wikiTier, 50)) / 2;
        else if (mobaTier != null)
            score = TierScore.GetValueOrDefault(mobaTier, 50);
        else if (wikiTier != null)
            score = TierScore.GetValueOrDefault(wikiTier, 50);
        else
            score = -1;

        var blended = score switch
        {
            >= 85 => "S",
            >= 71 => "A",
            >= 57 => "B",
            >= 43 => "C",
            >= 29 => "D",
            >= 0 => "F",
            _ => "?",
        };

        return new TierResult(mobaTier, wikiTier, blended, score);
    }

    private static string? LookupTier(Dictionary<string, Dictionary<string, string>> index, string charKey, string cardKey)
    {
        if (!index.TryGetValue(charKey, out var cards)) return null;
        if (cards.TryGetValue(cardKey, out var tier)) return tier;
        // Try without spaces/symbols for fuzzy match
        var collapsed = CollapseKey(cardKey);
        foreach (var (k, v) in cards)
        {
            if (CollapseKey(k) == collapsed) return v;
        }
        return null;
    }

    private static string NormalizeCardName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        var s = name.TrimEnd('+').Trim();
        return s.ToLowerInvariant();
    }

    private static string NormalizeCharacter(string character)
    {
        if (string.IsNullOrEmpty(character)) return "";
        var s = character;
        // Handle "CHARACTER.SILENT (18436160)" format
        var parenIdx = s.IndexOf('(');
        if (parenIdx > 0) s = s.Substring(0, parenIdx).Trim();
        // Handle "CHARACTER.SILENT" format
        var dotIdx = s.LastIndexOf('.');
        if (dotIdx >= 0) s = s.Substring(dotIdx + 1);
        // Handle "IroncladPlayer" format
        s = s.Replace("Player", "").Trim();
        return s.ToLowerInvariant();
    }

    private static string CollapseKey(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var c in s)
        {
            if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    private static Dictionary<string, Dictionary<string, string>> LoadEmbeddedTierList(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Embedded resource '{resourceName}' not found");

        using var doc = JsonDocument.Parse(stream);
        var root = doc.RootElement;
        var characters = root.GetProperty("characters");

        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var charProp in characters.EnumerateObject())
        {
            var charName = charProp.Name.ToLowerInvariant();
            var cardMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var tierProp in charProp.Value.EnumerateObject())
            {
                var tierLetter = tierProp.Name; // S, A, B, C, D, F
                foreach (var card in tierProp.Value.EnumerateArray())
                {
                    var cardName = card.GetString();
                    if (!string.IsNullOrEmpty(cardName))
                        cardMap[cardName.ToLowerInvariant()] = tierLetter;
                }
            }

            result[charName] = cardMap;
        }

        return result;
    }
}
