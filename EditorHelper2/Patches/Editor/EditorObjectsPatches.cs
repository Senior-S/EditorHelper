using EditorHelper2.API.Abstract;
using EditorHelper2.Updates.Editor;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper2.Patches.Editor;

[HarmonyPatch(typeof(EditorObjects))]
public class EditorObjectsPatches
{
    [HarmonyPatch(nameof(EditorObjects.Update))]
    [HarmonyPrefix]
    [UsedImplicitly]
    static bool PrefixUpdate(EditorObjects __instance)
    {
        EditorObjectsUpdate.Update(__instance);

        return false;
    }
    
    [HarmonyPatch(nameof(EditorObjects.calculateHandleOffsets))]
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void PostfixCalculateHandleOffsets(EditorObjects __instance)
    {
        EditorHelper.InvokeStaticEvent(typeof(LevelObjectsExtension), nameof(LevelObjectsExtension.OnCalculateHandleOffsets), __instance);
    }
}