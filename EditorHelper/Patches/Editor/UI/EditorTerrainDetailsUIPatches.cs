using EditorHelper.Editor;
using EditorHelper.Editor.Managers;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper.Patches.Editor.UI;

[HarmonyPatch]
public class EditorTerrainDetailsUIPatches
{
    [HarmonyPatch(typeof(EditorTerrainDetailsUI), MethodType.Constructor)]
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void Constructor(EditorTerrainDetailsUI __instance)
    {
        EditorHelper.Instance.FoliageCollectionManager = new FoliageCollectionManager();
        EditorHelper.Instance.FoliageCollectionManager.Initialize(ref __instance);
        
        EditorHelper.Instance.FoliageAssetManager = new FoliageAssetManager();
        EditorHelper.Instance.FoliageAssetManager.Initialize(ref __instance);
    }
    
    [HarmonyPatch(typeof(EditorTerrainDetailsUI), "OnUpdate")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void Update(EditorTerrainDetailsUI __instance)
    {
        if (EditorHelper.Instance.FoliageCollectionManager == null) return;
        if (EditorHelper.Instance.FoliageAssetManager == null) return;
        
        EditorHelper.Instance.FoliageCollectionManager.CustomUpdate(__instance);
        EditorHelper.Instance.FoliageAssetManager.CustomUpdate(__instance);
    }
}