using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper.Patches.Level;

[HarmonyPatch]
public class LevelZombiesPatches
{
    [HarmonyPatch(typeof(LevelZombies), "save")]
    [HarmonyPrefix]
    [UsedImplicitly]
    private static bool save()
    {
        if (LevelZombies.tables.Count > byte.MaxValue)
        {
            EditorHelper.Instance.EditorManager.DisplayAlert($"You have exceeded the max amount of zombie tables ({byte.MaxValue}). Zombie spawns won't be saved.");
            return false;
        }

        if (LevelZombies.tables.Any(c => c.slots.Length > byte.MaxValue))
        {
            ZombieTable table = LevelZombies.tables.First(c => c.slots.Length > byte.MaxValue);
            EditorHelper.Instance.EditorManager.DisplayAlert($"You have exceeded the max amount of zombie table slots ({byte.MaxValue} - Table: {table.name}). Zombie spawns won't be saved.");
            return false;
        }
        
        if (LevelZombies.tables.Any(c => c.slots.Any(s => s.table.Count > byte.MaxValue)))
        {
            ZombieTable table = LevelZombies.tables.First(c => c.slots.Any(s => s.table.Count > byte.MaxValue));
            ZombieSlot slot = table.slots.First(c => c.table.Count > byte.MaxValue);
            EditorHelper.Instance.EditorManager.DisplayAlert($"You have exceeded the max amount of zombie cloths for a slot table ({byte.MaxValue} - Table: {table.name} - Slot chance: {slot.chance}). Zombie spawns won't be saved.");
            return false;
        }

        if (LevelZombies.spawns.Cast<List<ZombieSpawnpoint>>().Any(c => c.Count > ushort.MaxValue))
        {
            EditorHelper.Instance.EditorManager.DisplayAlert($"You have exceeded the max amount of zombie spawns on a region ({ushort.MaxValue}). Zombie spawns won't be saved.");
            return false;
        }
        
        return true;
    }
}
