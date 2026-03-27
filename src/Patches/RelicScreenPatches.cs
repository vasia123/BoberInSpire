using FirstMod;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;

namespace FirstMod.Patches;

/// <summary>
/// Attach tier badges when the relic reward selection screen opens (post-combat).
/// </summary>
[HarmonyPatch(typeof(NChooseARelicSelection), nameof(NChooseARelicSelection.AfterOverlayOpened))]
public static class RelicRewardScreenOpenPatch
{
    [HarmonyPostfix]
    public static void AfterOpened(NChooseARelicSelection __instance)
    {
        RelicBadgeOverlay.ClearBadges();
        RelicBadgeOverlay.AttachBadgesDeferred(__instance);
    }
}

/// <summary>
/// Clear relic badges when reward screen closes.
/// </summary>
[HarmonyPatch(typeof(NChooseARelicSelection), nameof(NChooseARelicSelection.AfterOverlayClosed))]
public static class RelicRewardScreenClosePatch
{
    [HarmonyPostfix]
    public static void AfterClosed()
    {
        RelicBadgeOverlay.ClearBadges();
    }
}

/// <summary>
/// Attach tier badges to treasure room relic holders after they are initialized.
/// </summary>
[HarmonyPatch(typeof(NTreasureRoomRelicCollection), "InitializeRelics")]
public static class TreasureRelicInitPatch
{
    [HarmonyPostfix]
    public static void AfterInit(NTreasureRoomRelicCollection __instance)
    {
        RelicBadgeOverlay.ClearBadges();
        RelicBadgeOverlay.AttachBadgesDeferred(__instance);
    }
}

/// <summary>
/// Attach tier badges in the relic compendium when it opens.
/// </summary>
[HarmonyPatch(typeof(NRelicCollection), "OnSubmenuOpened")]
public static class RelicCollectionOpenPatch
{
    [HarmonyPostfix]
    public static void AfterOpened(NRelicCollection __instance)
    {
        RelicBadgeOverlay.ClearBadges();
        RelicBadgeOverlay.AttachBadgesDeferred(__instance);
    }
}

/// <summary>
/// Attach relic badges when merchant opens (alongside card badges).
/// </summary>
[HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Open))]
public static class MerchantRelicBadgePatch
{
    [HarmonyPostfix]
    public static void AfterOpen(NMerchantInventory __instance)
    {
        // Card badges are handled by CardScreenPatches.MerchantOpenBadgePatch
        // Here we only handle relic badges
        RelicBadgeOverlay.ClearBadges();
        RelicBadgeOverlay.AttachBadgesDeferred(__instance);
    }
}

/// <summary>
/// Clear relic badges when merchant closes.
/// </summary>
[HarmonyPatch(typeof(NMerchantInventory), "Close")]
public static class MerchantRelicClosePatch
{
    [HarmonyPostfix]
    public static void AfterClose()
    {
        RelicBadgeOverlay.ClearBadges();
    }
}
