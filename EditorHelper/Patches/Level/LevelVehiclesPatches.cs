using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Patches.Level;

[HarmonyPatch]
public class LevelVehiclesPatches
{
    [HarmonyPatch(typeof(LevelVehicles), "save")]
    [UsedImplicitly]
    private static bool save()
    {
        if (LevelVehicles.tables.Count > byte.MaxValue)
        {
            EditorHelper.Instance.EditorManager.DisplayAlert($"You have exceeded the max amount of vehicle tables ({byte.MaxValue}). Vehicle spawns won't be saved.");
            return false;
        }

        if (LevelVehicles.tables.Any(c => c.tiers.Count > byte.MaxValue))
        {
            VehicleTable table = LevelVehicles.tables.First(c => c.tiers.Count > byte.MaxValue);
            EditorHelper.Instance.EditorManager.DisplayAlert($"You have exceeded the max amount of vehicle tiers on a table ({byte.MaxValue} - Table: {table.name}). Vehicle spawns won't be saved.");
            return false;
        }
        
        // This looks like shit rn, but once it gets implemented in the original code a better approach is possible
        if (LevelVehicles.tables.Any(c => c.tiers.Any(t => t.table.Count > byte.MaxValue)))
        {
            VehicleTable table = LevelVehicles.tables.First(c => c.tiers.Any(t => t.table.Count > byte.MaxValue));
            VehicleTier tier = table.tiers.First(c => c.table.Count > byte.MaxValue);
            EditorHelper.Instance.EditorManager.DisplayAlert($"You have exceeded the max amount of vehicle spawns on a tier table ({byte.MaxValue} - Table: {table.name} - Tier: {tier.name}). Vehicle spawns won't be saved.");
            return false;
        }
        
        if (LevelVehicles.spawns.Count > byte.MaxValue)
        {
            EditorHelper.Instance.EditorManager.DisplayAlert($"You have exceeded the max amount of vehicle spawns ({byte.MaxValue}). Vehicle spawns won't be saved.");
            return false;
        }
        
        return true;
    }
    
    [HarmonyPatch(typeof(LevelVehicles), "addSpawn")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void addSpawn(Vector3 point, float angle)
    {
        if (EditorHelper.Instance.VehicleSpawnsManager == null) return;
        
        EditorHelper.Instance.VehicleSpawnsManager.UpdateVehicleSpawns();
    }
}