using EditorHelper.Editor;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper.Patches;

[HarmonyPatch]
public class FoliageEditorPatches
{
    [HarmonyPatch(typeof(FoliageEditor), "update")]
    [HarmonyPrefix]
    [UsedImplicitly]
    static bool Update(FoliageEditor __instance)
    {
        if (EditorHelper.Instance.FoliageManager == null) return true;
        
        EditorHelper.Instance.FoliageManager.CustomUpdate(__instance);
        return false;
    }

    [HarmonyPatch(typeof(FoliageEditor), "equip")]
    [HarmonyPrefix]
    [UsedImplicitly]
    static void Equip()
    {
        EditorHelper.Instance.FoliageManager = new FoliageManager();
    }
}