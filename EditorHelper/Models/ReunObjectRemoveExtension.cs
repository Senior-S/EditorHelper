using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Models;

public class ReunObjectRemoveExtension
{
    private readonly AssetReference<MaterialPaletteAsset> _customMaterialOverride;

    private readonly int _materialIndexOverride;

    public void Undo(Transform transform)
    {
        LevelObject levelObject = LevelObjects.FindLevelObject(transform.gameObject);
        if (levelObject == null) return; // It's a barricade or structure so let's ignore it

        levelObject.customMaterialOverride = _customMaterialOverride;
        levelObject.materialIndexOverride = _materialIndexOverride;
        
        levelObject.ReapplyMaterialOverrides();
    }

    public ReunObjectRemoveExtension(Transform transform)
    {
        LevelObject levelObject = LevelObjects.FindLevelObject(transform.gameObject);
        if (levelObject == null) return; // It's a barricade or structure so let's ignore it

        _customMaterialOverride = levelObject.customMaterialOverride;
        _materialIndexOverride = levelObject.materialIndexOverride;
    }
}