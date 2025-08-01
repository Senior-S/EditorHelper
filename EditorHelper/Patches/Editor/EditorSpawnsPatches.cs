using EditorHelper.Editor.Managers;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper.Patches.Editor;

[HarmonyPatch]
public class EditorSpawnsPatches
{
    [HarmonyPatch(typeof(EditorSpawns), MethodType.Constructor)]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void constructor(EditorSpawns __instance)
    {
        EditorHelper.Instance.VehicleSpawnsManager ??= new VehicleSpawnsManager();
        EditorHelper.Instance.AnimalSpawnsManager ??= new AnimalSpawnsManager();
    }
    
    [HarmonyPatch(typeof(EditorSpawns), "Update")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void Update(EditorSpawns __instance)
    {
        if (EditorHelper.Instance.VehicleSpawnsManager != null)
        {
            EditorHelper.Instance.VehicleSpawnsManager.CustomUpdateUI();
        }

        if (EditorHelper.Instance.AnimalSpawnsManager != null)
        {
            EditorHelper.Instance.AnimalSpawnsManager.CustomUpdate();
        }
        
    }
}