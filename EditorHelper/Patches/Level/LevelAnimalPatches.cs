using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Patches.Level;

[HarmonyPatch]
public class LevelAnimalPatches
{
    [HarmonyPatch(typeof(LevelAnimals), "addSpawn")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void addSpawn(Vector3 point)
    {
        if (EditorHelper.Instance.AnimalSpawnsManager == null) return;
        
        EditorHelper.Instance.AnimalSpawnsManager.UpdateAnimalSpawns();
    }
}