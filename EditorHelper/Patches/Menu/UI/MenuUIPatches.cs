using EditorHelper.Extras;
using EditorHelper.Menu;
using EditorHelper.Menu.UI;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper.Patches.Menu.UI;

[HarmonyPatch]
public class MenuUIPatches
{
    [HarmonyPatch(typeof(MenuUI), "tickInput")]
    [HarmonyPrefix]
    [UsedImplicitly]
    static bool tickInput(MenuUI __instance)
    {
        return !UpdaterCore.IsOutDated;
    }
    
    [HarmonyPatch(typeof(MenuUI), "customStart")]
    [HarmonyPrefix]
    [UsedImplicitly]
    private static void customStart(MenuUI __instance)
    {
        EditorHelper.Instance.BarnAssetManager = new BarnAssetManager();
    }
}