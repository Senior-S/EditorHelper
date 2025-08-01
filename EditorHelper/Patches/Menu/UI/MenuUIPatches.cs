using EditorHelper.Extras;
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
}