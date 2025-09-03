using EditorHelper2.Updates.Editor;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Patches.Editor.UI;

[HarmonyPatch(typeof(TerrainEditor))]
public class EditorTerrainMaterialsUIPatches
{
    [HarmonyPatch(nameof(TerrainEditor.update))]
    [HarmonyPostfix]
    [UsedImplicitly]
    public static void PostfixUpdate()
    {
        EditorTerrainMaterialsUIUpdate.Update();
    }
}