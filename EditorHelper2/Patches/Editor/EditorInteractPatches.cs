using EditorHelper2.Updates.Editor;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper2.Patches.Editor;

[HarmonyPatch(typeof(EditorInteract))]
public class EditorInteractPatches
{
    [HarmonyPatch(nameof(EditorInteract.Update))]
    [HarmonyPrefix]
    [UsedImplicitly]
    private static bool Update(EditorInteract __instance)
    {
        EditorInteractUpdate.Update(__instance);

        return false;
    }
}