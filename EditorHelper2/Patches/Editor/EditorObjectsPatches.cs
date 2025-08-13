using System;
using EditorHelper2.Updates.Editor;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper2.Patches.Editor;

[HarmonyPatch(typeof(EditorObjects))]
public class EditorObjectsPatches
{
    /// <summary>
    /// Event invoked after <see cref="EditorObjects.calculateHandleOffsets"/> is called.
    /// </summary>
    public static event Action<EditorObjects> OnCalculateHandleOffsets;
    
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
        OnCalculateHandleOffsets?.Invoke(__instance);
    }
}