using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Merchant;

namespace FirstMod.Patches;

/// <summary>
/// Reads stocked card offers from <see cref="MerchantInventory"/>.
/// Card titles come from <see cref="MerchantCardEntry.CreationResult"/>.<see cref="MegaCrit.Sts2.Core.Entities.Cards.CardCreationResult.Card"/> — not from <c>GetCardName</c>'s Model/Card on the entry alone.
/// </summary>
internal static class MerchantInventoryOffers
{
    private const BindingFlags PropFlags = BindingFlags.Public | BindingFlags.Instance;

    internal static List<string> ExtractStockedCardTitles(MerchantInventory? inventory)
    {
        var titles = new List<string>();
        if (inventory == null) return titles;

        try
        {
            foreach (var e in inventory.CharacterCardEntries)
                TryAddEntry(e, titles);
            foreach (var e in inventory.ColorlessCardEntries)
                TryAddEntry(e, titles);

            // Fallback if layout/API changes (extra columns, renamed lists).
            if (titles.Count == 0)
                ExtractViaReflectionFallback(inventory, titles);
        }
        catch
        {
            /* logged by caller */
        }

        return titles
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void TryAddEntry(MerchantCardEntry? entry, List<string> titles)
    {
        if (entry == null || !entry.IsStocked) return;
        var model = entry.CreationResult?.Card;
        if (model == null) return;
        var name = model.Title ?? model.GetType().Name;
        if (CardTitleHeuristics.IsPlausibleCardTitle(name))
            titles.Add(name);
    }

    private static void ExtractViaReflectionFallback(MerchantInventory inventory, List<string> titles)
    {
        foreach (var prop in inventory.GetType().GetProperties(PropFlags))
        {
            var pn = prop.Name;
            if (!pn.Contains("Card", StringComparison.OrdinalIgnoreCase)) continue;
            if (pn.Contains("Relic", StringComparison.OrdinalIgnoreCase)) continue;
            if (string.Equals(pn, nameof(MerchantInventory.CardRemovalEntry), StringComparison.Ordinal))
                continue;

            object? val;
            try { val = prop.GetValue(inventory); }
            catch { continue; }

            if (val is string) continue;
            if (val is not IEnumerable enumerable) continue;

            foreach (var elem in enumerable)
            {
                if (elem == null) continue;
                if (elem is MerchantCardEntry mce)
                {
                    TryAddEntry(mce, titles);
                    continue;
                }

                var stocked = elem.GetType().GetProperty("IsStocked", PropFlags)?.GetValue(elem);
                if (stocked is bool okStocked && !okStocked) continue;

                var name = RewardExportPatches.GetCardName(elem);
                if (CardTitleHeuristics.IsPlausibleCardTitle(name))
                    titles.Add(name!);
            }
        }
    }
}
