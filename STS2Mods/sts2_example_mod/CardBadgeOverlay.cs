using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace FirstMod;

/// <summary>
/// Creates and manages tier badges on card nodes in the reward/merchant screens.
/// Badges show a colored circle (S/A/B/C/D) with a blended score next to each card.
/// </summary>
public static class CardBadgeOverlay
{
    private static readonly List<Node> _badges = new();

    private static readonly Dictionary<string, Color> TierColors = new()
    {
        ["S"] = new Color("4dff9e"),
        ["A"] = new Color("ffcc4d"),
        ["B"] = new Color("7ec8ff"),
        ["C"] = new Color("f4d080"),
        ["D"] = new Color("ff9aab"),
        ["F"] = new Color("aab8ce"),
        ["?"] = new Color("888888"),
    };

    private static string _pendingCharacter = "";
    private static bool _polling;

    /// <summary>
    /// Start watching for card nodes to appear in NCardRewardSelectionScreen.
    /// Cards appear only after the player clicks the "Card Reward" button.
    /// </summary>
    public static void AttachBadgesDeferred(Node screenNode, string character)
    {
        _pendingCharacter = character;
        if (_polling) return;
        _polling = true;

        Task.Run(async () =>
        {
            try
            {
                for (int i = 0; i < 150 && _polling; i++)
                {
                    await Task.Delay(200);
                    Callable.From(() =>
                    {
                        try { TryAttachToPreviewContainers(); }
                        catch (Exception ex) { Log.Error($"[BoberInSpire] CardBadge deferred: {ex.Message}"); }
                    }).CallDeferred();
                    await Task.Delay(50);
                    if (_badges.Count > 0) break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[BoberInSpire] CardBadge polling: {ex.Message}");
            }
            finally
            {
                _polling = false;
            }
        });
    }

    public static void ClearBadges()
    {
        _polling = false;

        foreach (var badge in _badges)
        {
            try
            {
                if (GodotObject.IsInstanceValid(badge))
                    badge.QueueFree();
            }
            catch { }
        }
        _badges.Clear();
    }

    private static bool TryAttachToPreviewContainers()
    {
        if (_badges.Count > 0) return true;

        var root = (Engine.GetMainLoop() as SceneTree)?.Root;
        if (root == null) return false;

        var containers = new List<Node>();
        FindNodesByType(root, "NCardRewardSelectionScreen", containers, 0);

        foreach (var container in containers)
        {
            if (container.GetChildCount() == 0) continue;

            var cardNodes = new List<(Node node, string cardName)>();
            FindCardHolders(container, cardNodes, 0);

            if (cardNodes.Count < 2) continue;

            foreach (var (node, cardName) in cardNodes)
            {
                var tiers = TierData.GetTiers(_pendingCharacter, cardName);
                if (tiers.BlendedScore < 0) continue;

                var badge = CreateBadge(tiers, node, cardName);
                if (badge != null)
                    _badges.Add(badge);
            }

            if (_badges.Count > 0)
            {
                Log.Info($"[BoberInSpire] CardBadge: attached {_badges.Count} badges");
                return true;
            }
        }

        return false;
    }

    private static void FindNodesByType(Node parent, string targetType, List<Node> results, int depth)
    {
        if (depth > 8) return;
        if (parent.GetType().Name == targetType)
        {
            results.Add(parent);
            return;
        }
        foreach (var child in parent.GetChildren())
        {
            if (child != null)
                FindNodesByType(child, targetType, results, depth + 1);
        }
    }

    /// <summary>
    /// Find NGridCardHolder nodes and extract card names from node names.
    /// E.g. "GridCardHolder-CARD_SWORD_BOOMERANG" → "Sword Boomerang".
    /// </summary>
    private static void FindCardHolders(Node parent, List<(Node, string)> results, int depth)
    {
        if (depth > 10) return;
        foreach (var child in parent.GetChildren())
        {
            if (child == null) continue;
            try
            {
                if (child.GetType().Name == "NGridCardHolder")
                {
                    var cardName = ExtractCardNameFromHolder(child);
                    if (!string.IsNullOrEmpty(cardName))
                    {
                        results.Add((child, cardName!));
                        continue;
                    }
                }
                FindCardHolders(child, results, depth + 1);
            }
            catch { }
        }
    }

    private static PanelContainer? CreateBadge(TierData.TierResult tiers, Node cardHolder, string cardName)
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
            tierLabel.AddThemeColorOverride("font_color", new Color(0.05f, 0.05f, 0.1f));
            tierLabel.AddThemeFontSizeOverride("font_size", 16);
            tierLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            circle.AddChild(tierLabel);

            // Blended score label
            var scoreLabel = new Label();
            scoreLabel.Text = $"{tiers.BlendedScore}";
            scoreLabel.VerticalAlignment = VerticalAlignment.Center;
            scoreLabel.AddThemeColorOverride("font_color", color);
            scoreLabel.AddThemeFontSizeOverride("font_size", 14);
            scoreLabel.MouseFilter = Control.MouseFilterEnum.Ignore;

            hbox.AddChild(circle);
            hbox.AddChild(scoreLabel);
            badge.AddChild(hbox);

            // NGridCardHolder origin is at card center; Hitbox is 300x422
            // Place badge near top-right of card, beside the card name
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
