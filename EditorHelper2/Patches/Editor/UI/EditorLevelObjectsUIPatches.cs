using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text;
using Action = System.Action;

namespace EditorHelper2.Patches.Editor.UI;

[HarmonyPatch(typeof(EditorLevelObjectsUI))]
public class EditorLevelObjectsUIPatches
{
    /// <summary>
    /// Event invoked after <see cref="EditorObjects.selectedObjectAsset"/> or <see cref="EditorObjects.selectedItemAsset"/> has been set.
    /// </summary>
    public static event Action? OnObjectAssetSelected;

    /// <summary>
    /// Event invoked after <see cref="EditorLevelObjectsUI.updateSelection"/> is called.
    /// </summary>
    public static event Action<List<Asset>>? OnObjectBrowserUpdated;

    private static ISleekBox? _selectedAssetBox;

    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void PostfixConstructor(ISleekBox ___selectedBox)
    {
        _selectedAssetBox = ___selectedBox;
    }

    [HarmonyPatch(nameof(EditorLevelObjectsUI.onClickedAssetButton))]
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void PostfixClickedAssetButton(ISleekElement button)
    {
        OnObjectAssetSelected?.Invoke();
    }

    public static void SetSelectedObjectAsset(Asset? asset)
    {
        EditorObjects.selectedObjectAsset = asset as ObjectAsset;
        EditorObjects.selectedItemAsset = asset as ItemAsset;
        if (_selectedAssetBox != null)
            _selectedAssetBox.Text =
                EditorObjects.selectedObjectAsset?.FriendlyName ??
                EditorObjects.selectedItemAsset?.FriendlyName ??
                string.Empty;

        OnObjectAssetSelected?.Invoke();
    }

    [HarmonyPatch(nameof(EditorLevelObjectsUI.updateSelection))]
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void PostfixUpdateSelection(string search, bool large, bool medium, bool small, bool barricades, bool structures, bool npcs, ref List<Asset> ___assets)
    {
        OnObjectBrowserUpdated?.Invoke(___assets);
    }
}
