using DanielWillett.UITools;
using EditorHelper2.Extensions.Level.Objects;
using SDG.Unturned;

namespace EditorHelper2.Updates.Editor;

public static class EditorTerrainDetailsUIUpdate
{
    public static void Update()
    {
        UnturnedLog.info("UPDATE CLASS METHOD CALLED");
        
        #region CollectionManagerExtension
        CollectionManagerExtension? collectionManagerExtension = UnturnedUIToolsNexus.UIExtensionManager.GetInstance<CollectionManagerExtension>();
        collectionManagerExtension?.CustomUpdate();
        #endregion
    }
}