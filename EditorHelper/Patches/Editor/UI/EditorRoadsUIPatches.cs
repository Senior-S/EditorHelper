using EditorHelper.Editor.Managers;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper.Patches.Editor.UI;

[HarmonyPatch]
public class EditorRoadsUIPatches
{
    [HarmonyPatch(typeof(EditorEnvironmentRoadsUI), MethodType.Constructor)]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void constructor(EditorEnvironmentRoadsUI __instance)
    {
        EditorHelper.Instance.RoadsManager = new RoadsManager();
        
        EditorHelper.Instance.RoadsManager.Initialize();
    }
}