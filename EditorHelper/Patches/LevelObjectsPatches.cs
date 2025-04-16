using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper.Patches;

[HarmonyPatch]
public class LevelObjectsPatches
{
    [HarmonyPatch(typeof(LevelObjects), "save")]
    [HarmonyPrefix]
    [UsedImplicitly]
    private static bool save()
    {
        if (LevelObjects.objects.Cast<List<LevelObject>>().Any(c => c.Count > ushort.MaxValue))
        {
            EditorHelper.Instance.EditorManager.DisplayAlert($"You have exceeded the max amount of objects on a region ({ushort.MaxValue}). Objects won't be saved.");
            return false;
        }
        if (LevelObjects.buildables.Cast<List<LevelBuildableObject>>().Any(c => c.Count > ushort.MaxValue))
        {
            EditorHelper.Instance.EditorManager.DisplayAlert($"You have exceeded the max amount of buildable objects on a region ({ushort.MaxValue}). Objects won't be saved.");
            return false;
        }
        
        return true;
    }
}
