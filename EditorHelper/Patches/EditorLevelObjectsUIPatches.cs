using EditorHelper.Editor;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper.Patches;

[HarmonyPatch]
public class EditorLevelObjectsUIPatches
{
    [HarmonyPatch(typeof(EditorLevelObjectsUI), MethodType.Constructor)]
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void Constructor(EditorLevelObjectsUI __instance)
    {
        EditorHelper.Instance.ObjectsManager = new ObjectsManager();
        EditorHelper.Instance.ObjectsManager.Initialize(ref __instance);
    }
}