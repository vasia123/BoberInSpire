using FirstMod;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Logging;

[ModInitializer("Initialize")]
public class ModEntry
{
    public static void Initialize()
    {
        TierData.Initialize();
        var harmony = new Harmony("smartpick.patch");
        harmony.PatchAll();
        Log.Info("[SmartPick] Harmony patches initialized.");
    }
}