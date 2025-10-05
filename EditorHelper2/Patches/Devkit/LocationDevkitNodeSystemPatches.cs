using EditorHelper2.Updates.Devkit;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper2.Patches.Devkit;

[HarmonyPatch(typeof(LocationDevkitNodeSystem))]
public class LocationDevkitNodeSystemPatches
{
    [HarmonyPatch(nameof(LocationDevkitNodeSystem.OnUpdateGizmos))]
    [HarmonyPrefix]
    [UsedImplicitly]
    static bool OnUpdateGizmos(LocationDevkitNodeSystem __instance)
    {
        return LocationDevkitNodeSystemUpdate.Update(__instance);;
    }
}