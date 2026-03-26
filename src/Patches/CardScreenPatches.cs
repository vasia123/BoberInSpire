using FirstMod;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace FirstMod.Patches;

/// <summary>
/// Attach tier badges when the card reward selection screen opens (post-combat / event card pick).
/// </summary>
[HarmonyPatch(typeof(NCardRewardSelectionScreen), nameof(NCardRewardSelectionScreen.AfterOverlayOpened))]
public static class CardRewardScreenOpenPatch
{
    [HarmonyPostfix]
    public static void AfterOpened(NCardRewardSelectionScreen __instance)
    {
        CardBadgeOverlay.ActiveRewardScreen = __instance;
        CardBadgeOverlay.ClearBadges();
        CardBadgeOverlay.AttachBadgesDeferred(__instance);
    }
}

/// <summary>
/// Clear badges when card reward screen closes.
/// </summary>
[HarmonyPatch(typeof(NCardRewardSelectionScreen), nameof(NCardRewardSelectionScreen.AfterOverlayClosed))]
public static class CardRewardScreenClosePatch
{
    [HarmonyPostfix]
    public static void AfterClosed()
    {
        CardBadgeOverlay.ActiveRewardScreen = null;
        CardBadgeOverlay.ClearBadges();
    }
}

/// <summary>
/// Attach tier badges when any card grid selection screen opens (forge, transform, enchant, etc.).
/// Patches the base class NCardGridSelectionScreen to catch all grid-based selection screens.
/// NCardRewardSelectionScreen is a separate hierarchy and is NOT affected by this patch.
/// </summary>
[HarmonyPatch(typeof(NCardGridSelectionScreen), nameof(NCardGridSelectionScreen.AfterOverlayOpened))]
public static class CardGridSelectionScreenOpenPatch
{
    [HarmonyPostfix]
    public static void AfterOpened(NCardGridSelectionScreen __instance)
    {
        CardBadgeOverlay.ActiveGridSelection = __instance;
        CardBadgeOverlay.ClearBadges();
        CardBadgeOverlay.AttachBadgesDeferred(__instance);
    }
}

/// <summary>
/// Clear badges when any card grid selection screen closes.
/// </summary>
[HarmonyPatch(typeof(NCardGridSelectionScreen), nameof(NCardGridSelectionScreen.AfterOverlayClosed))]
public static class CardGridSelectionScreenClosePatch
{
    [HarmonyPostfix]
    public static void AfterClosed()
    {
        CardBadgeOverlay.ActiveGridSelection = null;
        CardBadgeOverlay.ClearBadges();
    }
}

/// <summary>
/// Attach tier badges when the "choose a card" screen opens (e.g. after picking a bundle).
/// NChooseACardSelectionScreen is a separate hierarchy (extends Control, not NCardGridSelectionScreen).
/// </summary>
[HarmonyPatch(typeof(NChooseACardSelectionScreen), nameof(NChooseACardSelectionScreen.AfterOverlayOpened))]
public static class ChooseACardScreenOpenPatch
{
    [HarmonyPostfix]
    public static void AfterOpened(NChooseACardSelectionScreen __instance)
    {
        CardBadgeOverlay.ClearBadges();
        CardBadgeOverlay.AttachBadgesDeferred(__instance);
    }
}

/// <summary>
/// Clear badges when "choose a card" screen closes.
/// </summary>
[HarmonyPatch(typeof(NChooseACardSelectionScreen), nameof(NChooseACardSelectionScreen.AfterOverlayClosed))]
public static class ChooseACardScreenClosePatch
{
    [HarmonyPostfix]
    public static void AfterClosed()
    {
        CardBadgeOverlay.ClearBadges();
    }
}

/// <summary>
/// Attach tier badges to card bundles (stacked cards) when bundle selection screen opens.
/// </summary>
[HarmonyPatch(typeof(NChooseABundleSelectionScreen), nameof(NChooseABundleSelectionScreen.AfterOverlayOpened))]
public static class BundleScreenOpenPatch
{
    [HarmonyPostfix]
    public static void AfterOpened(NChooseABundleSelectionScreen __instance)
    {
        CardBadgeOverlay.ClearBadges();
        CardBadgeOverlay.AttachBadgesDeferred(__instance);
    }
}

/// <summary>
/// Re-badge when bundle preview cards appear (after clicking a bundle pack).
/// </summary>
[HarmonyPatch(typeof(NChooseABundleSelectionScreen), "OnBundleClicked")]
public static class BundlePreviewOpenPatch
{
    [HarmonyPostfix]
    public static void AfterBundleClicked(NChooseABundleSelectionScreen __instance)
    {
        CardBadgeOverlay.ClearBadges();
        CardBadgeOverlay.AttachBadgesDeferred(__instance);
    }
}

/// <summary>
/// Re-badge stacked bundles when preview is cancelled (back to bundle selection).
/// </summary>
[HarmonyPatch(typeof(NChooseABundleSelectionScreen), "CancelSelection")]
public static class BundleCancelPatch
{
    [HarmonyPostfix]
    public static void AfterCancel(NChooseABundleSelectionScreen __instance)
    {
        CardBadgeOverlay.ClearBadges();
        CardBadgeOverlay.AttachBadgesDeferred(__instance);
    }
}

/// <summary>
/// Attach tier badges when any card view screen opens (deck view, etc).
/// Patches the base class NCardsViewScreen to catch all card view screens.
/// </summary>
[HarmonyPatch(typeof(NCardsViewScreen), nameof(NCardsViewScreen.AfterCapstoneOpened))]
public static class CardsViewOpenPatch
{
    [HarmonyPostfix]
    public static void AfterOpened(NCardsViewScreen __instance)
    {
        CardBadgeOverlay.ClearBadges();
        CardBadgeOverlay.AttachBadgesDeferred(__instance);
    }
}

/// <summary>
/// Re-badge after deck view sorts/refreshes cards (only while screen is visible).
/// </summary>
[HarmonyPatch(typeof(NDeckViewScreen), "DisplayCards")]
public static class DeckViewDisplayCardsPatch
{
    [HarmonyPostfix]
    public static void AfterDisplayCards(NDeckViewScreen __instance)
    {
        if (!__instance.Visible) return;
        CardBadgeOverlay.ClearBadges();
        CardBadgeOverlay.AttachBadgesDeferred(__instance);
    }
}

/// <summary>
/// When card view (deck) closes, re-badge the screen underneath (merchant, reward, or grid selection).
/// </summary>
[HarmonyPatch(typeof(NCardsViewScreen), nameof(NCardsViewScreen.AfterCapstoneClosed))]
public static class CardsViewClosePatch
{
    [HarmonyPostfix]
    public static void AfterClosed()
    {
        CardBadgeOverlay.ClearBadges();

        // Re-badge the screen underneath if still open
        if (CardBadgeOverlay.ActiveMerchant != null
            && GodotObject.IsInstanceValid(CardBadgeOverlay.ActiveMerchant))
        {
            CardBadgeOverlay.AttachBadgesDeferred(CardBadgeOverlay.ActiveMerchant);
        }
        else if (CardBadgeOverlay.ActiveRewardScreen != null
            && GodotObject.IsInstanceValid(CardBadgeOverlay.ActiveRewardScreen)
            && ((Control)CardBadgeOverlay.ActiveRewardScreen).Visible)
        {
            CardBadgeOverlay.AttachBadgesDeferred(CardBadgeOverlay.ActiveRewardScreen);
        }
        else if (CardBadgeOverlay.ActiveGridSelection != null
            && GodotObject.IsInstanceValid(CardBadgeOverlay.ActiveGridSelection)
            && ((Control)CardBadgeOverlay.ActiveGridSelection).Visible)
        {
            CardBadgeOverlay.AttachBadgesDeferred(CardBadgeOverlay.ActiveGridSelection);
        }
    }
}

/// <summary>
/// Attach tier badges when merchant shop opens.
/// </summary>
[HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Open))]
public static class MerchantOpenBadgePatch
{
    [HarmonyPostfix]
    public static void AfterOpen(NMerchantInventory __instance)
    {
        CardBadgeOverlay.ActiveMerchant = __instance;
        CardBadgeOverlay.ClearBadges();
        CardBadgeOverlay.AttachBadgesDeferred(__instance);
    }
}

/// <summary>
/// Clear badges when merchant shop closes.
/// </summary>
[HarmonyPatch(typeof(NMerchantInventory), "Close")]
public static class MerchantCloseBadgePatch
{
    [HarmonyPostfix]
    public static void AfterClose()
    {
        CardBadgeOverlay.ActiveMerchant = null;
        CardBadgeOverlay.ClearBadges();
    }
}

/// <summary>
/// Attach tier badges in the card compendium (card library) after cards are filtered/displayed.
/// NCardLibraryGrid.FilterCards is called on open and every filter/sort change.
/// </summary>
[HarmonyPatch(typeof(NCardLibraryGrid), "FilterCards")]
public static class CardLibraryFilterPatch
{
    [HarmonyPostfix]
    public static void AfterFilter(NCardLibraryGrid __instance)
    {
        CardBadgeOverlay.ClearBadges();
        CardBadgeOverlay.AttachBadgesDeferred(__instance);
    }
}
