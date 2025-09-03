using DanielWillett.UITools;
using EditorHelper2.Extensions.Level.Objects;
using EditorHelper2.Extensions.Terrain.Materials;

namespace EditorHelper2.Updates.Editor;

public static class EditorTerrainMaterialsUIUpdate
{
    public static void Update()
    {
        #region BrushExtension
        BrushExtension? brushExtension = UnturnedUIToolsNexus.UIExtensionManager.GetInstance<BrushExtension>();
        brushExtension?.CustomUpdate();
        #endregion FoliageManagerExtension
    }
}