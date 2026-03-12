using SDG.Unturned;

namespace EditorHelper2.common.Types;

public class ObjectCopyExtraData(LevelObject levelObject)
{
    public AssetReference<MaterialPaletteAsset> MaterialPaletteAsset { get; } = levelObject.customMaterialOverride;
    public int MaterialPaletteIndex { get; } = levelObject.materialIndexOverride;
    public bool OwnedCullingVolumeEnabled { get; } = levelObject.isOwnedCullingVolumeAllowed;

    public void Apply(LevelObject levelObject)
    {
        levelObject.customMaterialOverride = MaterialPaletteAsset;
        levelObject.materialIndexOverride = MaterialPaletteIndex;
        levelObject.ReapplyMaterialOverrides();

        levelObject.isOwnedCullingVolumeAllowed = OwnedCullingVolumeEnabled;
        levelObject.ReapplyOwnedCullingVolumeAllowed();
    }
}
