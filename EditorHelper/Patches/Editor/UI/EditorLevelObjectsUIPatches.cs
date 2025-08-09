using EditorHelper.Editor.Managers;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using System.Collections.Generic;

namespace EditorHelper.Patches.Editor.UI;

[HarmonyPatch]
public class EditorLevelObjectsUIPatches
{
    [HarmonyPatch(typeof(EditorLevelObjectsUI), MethodType.Constructor)]
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void Constructor(EditorLevelObjectsUI __instance)
    {
        EditorHelper.Instance.ObjectsManager = new ObjectsManager();
        EditorHelper.Instance.ObjectsManager.Initialize(ref __instance);
    }

    [HarmonyPatch(typeof(EditorLevelObjectsUI), nameof(EditorLevelObjectsUI.OnDestroy))]
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void OnDestroyPostfix()
    {
        if (EditorHelper.Instance.ObjectsManager == null) return;

        EditorHelper.Instance.ObjectsManager.OnDestroy();
    }

    [HarmonyPatch(typeof(EditorLevelObjectsUI), nameof(EditorLevelObjectsUI.onClickedAssetButton))]
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void ClickedAssetButtonPostfix(ISleekElement button)
    {
        if (EditorHelper.Instance.ObjectsManager == null) return;

        EditorHelper.Instance.ObjectsManager.OnAssetSelected();
    }

    [HarmonyPatch(typeof(EditorLevelObjectsUI), nameof(EditorLevelObjectsUI.updateSelection))]
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void UpdateSelectionPostfix(string search, bool large, bool medium, bool small, bool barricades, bool structures, bool npcs, ref List<Asset> ___assets)
    {
        if (EditorHelper.Instance.ObjectsManager == null) return;

        EditorHelper.Instance.ObjectsManager.OnUpdateObjectBrowser(___assets);
    }
}