using DanielWillett.UITools;
using EditorHelper2.Extensions.Visibility;

namespace EditorHelper2.Updates.Editor;

public static class EditorLevelVisibilityUIUpdate
{
    public static void Update()
    {
        #region RegionsExtension
        RegionsExtension? regionsExtension = UnturnedUIToolsNexus.UIExtensionManager.GetInstance<RegionsExtension>();
        regionsExtension?.CustomUpdate();
        #endregion FoliageManagerExtension
    }
}