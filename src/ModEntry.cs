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
        RelicTierData.Initialize();
        var harmony = new Harmony("smartpick.patch");
        try
        {
            harmony.PatchAll();
            Log.Info("[SmartPick] Harmony patches initialized.");
        }
        catch (Exception ex)
        {
            Log.Error($"[SmartPick] PatchAll FAILED: {ex.Message}\n{ex.StackTrace}");
        }
    }
}