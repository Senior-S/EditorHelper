using EditorHelper.Editor;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper.Patches;

[HarmonyPatch]
public class EditorDashboardUIPatches
{
    [HarmonyPatch(typeof(EditorDashboardUI), MethodType.Constructor)]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void Constructor()
    {
        EditorHelper.Instance.EditorManager = new EditorManager();
        EditorHelper.Instance.EditorManager.Initialize();
    }
}