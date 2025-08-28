using EditorHelper.Editor;
using EditorHelper.Editor.Managers;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper.Patches.Editor.UI;

[HarmonyPatch]
public class EditorTerrainMaterialsUIPatches
{
    [HarmonyPatch(typeof(EditorTerrainMaterialsUI), MethodType.Constructor)]
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void Constructor(EditorTerrainMaterialsUI __instance)
    {
        EditorHelper.Instance.MaterialAssetManager = new MaterialAssetManager();
        EditorHelper.Instance.MaterialAssetManager.Initialize(ref __instance);
    }
    
    [HarmonyPatch(typeof(EditorTerrainMaterialsUI), "OnUpdate")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void Update(EditorTerrainMaterialsUI __instance)
    {
        if (EditorHelper.Instance.MaterialAssetManager == null) return;
        
        EditorHelper.Instance.MaterialAssetManager.CustomUpdate(__instance);
    }
}