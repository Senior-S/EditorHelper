using EditorHelper.Editor;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper.Patches;

[HarmonyPatch]
public class EditorTerrainDetailsUIPatches
{
    [HarmonyPatch(typeof(EditorTerrainDetailsUI), MethodType.Constructor)]
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void Constructor(EditorTerrainDetailsUI __instance)
    {
        EditorHelper.Instance.CollectionManager = new CollectionManager();
        EditorHelper.Instance.CollectionManager.Initialize(ref __instance);
    }
    
    [HarmonyPatch(typeof(EditorTerrainDetailsUI), "UpdateOffsets")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void Update(EditorTerrainDetailsUI __instance)
    {
        if (EditorHelper.Instance.CollectionManager == null) return;
        
        EditorHelper.Instance.CollectionManager.CustomUpdate(__instance);
    }
}