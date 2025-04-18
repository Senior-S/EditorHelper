using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Models;

public class EditorCopyExtension
{
    private readonly AssetReference<MaterialPaletteAsset> _customMaterialOverride;

    private readonly int _materialIndexOverride;

    public void Apply(Transform transform)
    {
        LevelObject levelObject = LevelObjects.FindLevelObject(transform.gameObject);
        if (levelObject == null) return; // It's a barricade or structure so let's ignore it

        levelObject.customMaterialOverride = _customMaterialOverride;
        levelObject.materialIndexOverride = _materialIndexOverride;
        
        levelObject.ReapplyMaterialOverrides();
    }
    
    public EditorCopyExtension(Transform transform)
    {
        LevelObject levelObject = LevelObjects.FindLevelObject(transform.gameObject);
        if (levelObject == null) return; // It's a barricade or structure so let's ignore it

        _customMaterialOverride = levelObject.customMaterialOverride;
        _materialIndexOverride = levelObject.materialIndexOverride;
    }
}