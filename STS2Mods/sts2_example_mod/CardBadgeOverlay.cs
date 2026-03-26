using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace FirstMod;

/// <summary>
/// Continuously monitors the scene tree for NGridCardHolder nodes and attaches
/// tier badges (colored circle + score) to any card that has tier data.
/// Works on reward screens, deck view, merchant, etc.
/// </summary>
public static class CardBadgeOverlay
{
    private static readonly HashSet<ulong> _badgedHolders = new();
    private static readonly List<Node> _badges = new();

    private static readonly Dictionary<string, Color> TierColors = new()
    {
        ["S"] = new Color("ff8000"),  // orange — legendary
        ["A"] = new Color("a335ee"),  // purple — epic
        ["B"] = new Color("0070dd"),  // blue — rare
        ["C"] = new Color("1eff00"),  // green — uncommon
        ["D"] = new Color("9d9d9d"),  // grey — poor
        ["F"] = new Color("9d9d9d"),  // grey
        ["?"] = new Color("666666"),
    };

    private static bool _running;
    private static string _character = "";

    /// <summary>
    /// Start the background monitor that continuously scans for card holders.
    /// Called once at mod init. Safe to call multiple times.
    /// </summary>
    public static void StartMonitor()
    {
        if (_running) return;
        _running = true;

        Task.Run(async () =>
        {
            while (_running)
            {
                await Task.Delay(500);
                Callable.From(() =>
                {
                    try { ScanAndBadge(); }
                    catch (Exception ex) { Log.Error($"[BoberInSpire] CardBadge scan: {ex.Message}"); }
                }).CallDeferred();
            }
        });
    }

    private static void ScanAndBadge()
    {
        var root = (Engine.GetMainLoop() as SceneTree)?.Root;
        if (root == null) return;

        // Resolve character if not set yet
        if (string.IsNullOrEmpty(_character) || _character == "Unknown")
            _character = CombatExporter.ResolveCharacterName();

        // Find all NGridCardHolder nodes in the tree
        var holders = new List<(Node holder, string cardName)>();
        FindAllCardHolders(root, holders, 0);

        // Remove badges for holders that no longer exist
        CleanupStale();

        // Attach badges to new holders
        foreach (var (holder, cardName) in holders)
        {
            var id = holder.GetInstanceId();
            if (_badgedHolders.Contains(id)) continue;

            var tiers = TierData.GetTiers(_character, cardName);
            if (tiers.BlendedScore < 0) continue;

            var badge = CreateBadge(tiers, holder);
            if (badge != null)
            {
                _badges.Add(badge);
                _badgedHolders.Add(id);
            }
        }
    }

    private static void CleanupStale()
    {
        for (int i = _badges.Count - 1; i >= 0; i--)
        {
            var badge = _badges[i];
            if (!GodotObject.IsInstanceValid(badge) || !badge.IsInsideTree())
            {
                try { if (GodotObject.IsInstanceValid(badge)) badge.QueueFree(); }
                catch { }
                _badges.RemoveAt(i);
            }
        }

        // Rebuild holder ID set from surviving badges
        _badgedHolders.Clear();
        foreach (var badge in _badges)
        {
            var parent = badge.GetParent();
            if (parent != null)
                _badgedHolders.Add(parent.GetInstanceId());
        }
    }

    private static void FindAllCardHolders(Node parent, List<(Node, string)> results, int depth)
    {
        if (depth > 15) return;
        foreach (var child in parent.GetChildren())
        {
            if (child == null) continue;
            try
            {
                if (child.GetType().Name == "NGridCardHolder")
                {
                    // Only badge visible holders
                    if (child is Control ctrl && !ctrl.Visible) continue;

                    var cardName = ExtractCardNameFromHolder(child);
                    if (!string.IsNullOrEmpty(cardName))
                    {
                        results.Add((child, cardName!));
                        continue; // don't recurse into holder
                    }
                }
                FindAllCardHolders(child, results, depth + 1);
            }
            catch { }
        }
    }

    private static PanelContainer? CreateBadge(TierData.TierResult tiers, Node cardHolder)
    {
        try
        {
            var color = TierColors.GetValueOrDefault(tiers.BlendedTier, TierColors["?"]);

            var badge = new PanelContainer();
            badge.Name = "BoberTierBadge";
            badge.MouseFilter = Control.MouseFilterEnum.Ignore;
            badge.ZIndex = 50;

            // Dark semi-transparent pill background
            var style = new StyleBoxFlat();
            style.BgColor = new Color(0.05f, 0.05f, 0.1f, 0.75f);
            style.CornerRadiusBottomLeft = 14;
            style.CornerRadiusBottomRight = 14;
            style.CornerRadiusTopLeft = 14;
            style.CornerRadiusTopRight = 14;
            style.ContentMarginLeft = 4;
            style.ContentMarginRight = 8;
            style.ContentMarginTop = 2;
            style.ContentMarginBottom = 2;
            badge.AddThemeStyleboxOverride("panel", style);

            var hbox = new HBoxContainer();
            hbox.AddThemeConstantOverride("separation", 4);
            hbox.MouseFilter = Control.MouseFilterEnum.Ignore;

            // Colored circle with tier letter
            var circle = new PanelContainer();
            circle.CustomMinimumSize = new Vector2(28, 28);
            circle.MouseFilter = Control.MouseFilterEnum.Ignore;
            var circleStyle = new StyleBoxFlat();
            circleStyle.BgColor = color;
            circleStyle.CornerRadiusBottomLeft = 14;
            circleStyle.CornerRadiusBottomRight = 14;
            circleStyle.CornerRadiusTopLeft = 14;
            circleStyle.CornerRadiusTopRight = 14;
            circle.AddThemeStyleboxOverride("panel", circleStyle);

            var tierLabel = new Label();
            tierLabel.Text = tiers.BlendedTier;
            tierLabel.HorizontalAlignment = HorizontalAlignment.Center;
            tierLabel.VerticalAlignment = VerticalAlignment.Center;
            var textColor = (tiers.BlendedTier is "A" or "B" or "D" or "F" or "?")
                ? new Color(1f, 1f, 1f)
                : new Color(0.05f, 0.05f, 0.1f);
            tierLabel.AddThemeColorOverride("font_color", textColor);
            tierLabel.AddThemeFontSizeOverride("font_size", 16);
            tierLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            circle.AddChild(tierLabel);

            // Score label
            var scoreLabel = new Label();
            scoreLabel.Text = $"{tiers.BlendedScore}";
            scoreLabel.VerticalAlignment = VerticalAlignment.Center;
            scoreLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
            scoreLabel.AddThemeFontSizeOverride("font_size", 14);
            scoreLabel.MouseFilter = Control.MouseFilterEnum.Ignore;

            hbox.AddChild(circle);
            hbox.AddChild(scoreLabel);
            badge.AddChild(hbox);

            // NGridCardHolder origin is at card center; place near top-right
            badge.Position = new Vector2(100, -200);

            cardHolder.AddChild(badge);
            return badge;
        }
        catch (Exception ex)
        {
            Log.Error($"[BoberInSpire] CreateBadge: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Extract English card name from NGridCardHolder node name.
    /// "GridCardHolder-CARD_SWORD_BOOMERANG" → "Sword Boomerang"
    /// </summary>
    private static string? ExtractCardNameFromHolder(Node holder)
    {
        var nodeName = holder.Name.ToString();
        const string prefix = "GridCardHolder-CARD_";
        if (!nodeName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var raw = nodeName.Substring(prefix.Length);
        var words = raw.Split('_', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
            words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
        return string.Join(" ", words);
    }
}
