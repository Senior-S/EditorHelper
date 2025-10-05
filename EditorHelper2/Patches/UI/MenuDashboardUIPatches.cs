using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper2.Patches.UI;

[HarmonyPatch(typeof(MenuDashboardUI))]
public class MenuDashboardUIPatches
{
    [HarmonyPatch("OnClickedBattlEyeButton")]
    [HarmonyPrefix]
    [UsedImplicitly]
    private static bool OnClickedBattlEyeButton(ISleekElement element)
    {
        Provider.provider.browserService.open("https://discord.gg/Y3jD5K2Q8C");

        return false;
    }
}