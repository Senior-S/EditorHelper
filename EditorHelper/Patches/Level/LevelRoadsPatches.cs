using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper.Patches.Level;

[HarmonyPatch]
public class LevelRoadsPatches
{
    [HarmonyPatch(typeof(LevelRoads), "save")]
    [HarmonyPrefix]
    [UsedImplicitly]
    private static bool save(LevelRoads __instance)
    {
        if (LevelRoads.materials.Length > byte.MaxValue)
        {
            EditorHelper.Instance.EditorManager.DisplayAlert($"You have exceeded the max amount of road materials ({byte.MaxValue}). Roads won't be saved.");
            return false;
        }

        int totalJoints = LevelRoads.roads.Sum(c => c.joints.Count);
        if (totalJoints > ushort.MaxValue)
        {
            EditorHelper.Instance.EditorManager.DisplayAlert($"You have exceeded the max amount of total road joints ({ushort.MaxValue}). Roads won't be saved.");
            return false;
        }

        List<Road> wrongRoads = LevelRoads.roads.Where(c => c.joints.Count > ushort.MaxValue).ToList();
        if (wrongRoads.Count > 0)
        {
            EditorHelper.Instance.EditorManager.DisplayAlert($"You have exceeded the max amount of joints per road ({ushort.MaxValue}). Roads won't be saved.");
            return false;
        }
        
        return true;
    }
}