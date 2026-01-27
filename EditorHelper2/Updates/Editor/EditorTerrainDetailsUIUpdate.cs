using EditorHelper2.Extensions.Level.Objects;
using EditorHelper2.Extensions.Terrain.Foliage;
using EditorHelper2.Loader;

namespace EditorHelper2.Updates.Editor;

public static class EditorTerrainDetailsUIUpdate
{
    public static void Update()
    {
        #region CollectionManagerExtension
        if (ExtensionManager.TryGetInstance(out CollectionManagerExtension? collectionManagerExtension))
        {
            collectionManagerExtension?.CustomUpdate();
        }
        #endregion CollectionManagerExtension

        #region FoliageManagerExtension
        if (ExtensionManager.TryGetInstance(out FoliageManagerExtension? foliageManagerExtension))
        {
            foliageManagerExtension?.CustomUpdate();
        }
        #endregion FoliageManagerExtension

        #region ResourceReplacerExtension
        if (ExtensionManager.TryGetInstance(out ResourceReplacerExtension? resourceReplacerExtension))
        {
            resourceReplacerExtension?.CustomUpdate();
        }
        #endregion ResourceReplacerExtension
    }
}