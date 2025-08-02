using HarmonyLib;
using JetBrains.Annotations;
using SDG.Framework.Devkit;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Patches;

[HarmonyPatch]
public class CullingVolumePatches
{
    [HarmonyPatch(typeof(CullingVolume.Menu), "OnDistanceChanged")]
    [HarmonyPrefix]
    [UsedImplicitly]
    static bool OnDistanceChanged(CullingVolume.Menu __instance, ISleekFloat32Field field, float value)
    {
        __instance.volume.cullDistance = Mathf.Clamp(value, 1f, SDG.Unturned.Level.size);
        __instance.distanceField.Value = __instance.volume.cullDistance;
        LevelHierarchy.MarkDirty();
        return false;
    }
}