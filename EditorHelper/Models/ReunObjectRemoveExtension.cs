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

        levelObject.customMaterialOverride = _customMaterialOverride;
        levelObject.materialIndexOverride = _materialIndexOverride;
        
        levelObject.ReapplyMaterialOverrides();
    }

    public ReunObjectRemoveExtension(Transform transform)
    {
        LevelObject levelObject = LevelObjects.FindLevelObject(transform.gameObject);

        _customMaterialOverride = levelObject.customMaterialOverride;
        _materialIndexOverride = levelObject.materialIndexOverride;
    }
}