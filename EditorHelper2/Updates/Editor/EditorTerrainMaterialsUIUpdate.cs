using EditorHelper2.Extensions.Terrain.Materials;
using EditorHelper2.Loader;

namespace EditorHelper2.Updates.Editor;

public static class EditorTerrainMaterialsUIUpdate
{
    public static void Update()
    {
        #region BrushExtension
        if (ExtensionManager.TryGetInstance(out BrushExtension? brushExtension))
        {
            brushExtension?.CustomUpdate();
        }
        #endregion BrushExtension
    }
}