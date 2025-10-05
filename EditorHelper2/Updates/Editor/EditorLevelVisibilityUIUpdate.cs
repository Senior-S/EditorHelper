using EditorHelper2.Extensions.Level.Visibility;
using EditorHelper2.Loader;

namespace EditorHelper2.Updates.Editor;

public static class EditorLevelVisibilityUIUpdate
{
    public static void Update()
    {
        #region RegionsExtension
        if (ExtensionManager.TryGetInstance(out RegionsExtension? regionsExtension) && regionsExtension != null)
        {
            regionsExtension.CustomUpdate();
        }
        #endregion RegionsExtension
    }
}