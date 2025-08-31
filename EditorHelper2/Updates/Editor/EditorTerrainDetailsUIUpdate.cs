using DanielWillett.UITools;
using EditorHelper2.Extensions.Level.Objects;

namespace EditorHelper2.Updates.Editor;

public static class EditorTerrainDetailsUIUpdate
{
    public static void Update()
    {
        #region CollectionManagerExtension
        CollectionManagerExtension? collectionManagerExtension = UnturnedUIToolsNexus.UIExtensionManager.GetInstance<CollectionManagerExtension>();
        collectionManagerExtension?.CustomUpdate();
        #endregion
    }
}