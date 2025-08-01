using EditorHelper.Editor.Managers;
using HarmonyLib;
using SDG.Unturned;

namespace EditorHelper.Patches.Editor.UI;

[HarmonyPatch]
public class EditorLevelVisibilityUIPatches
{
    [HarmonyPatch(typeof(EditorLevelVisibilityUI), "open")]
    [HarmonyPostfix]
    static void openPostfix()
    {
        if (EditorHelper.Instance.VisibilityManager == null || 
            EditorHelper.Instance.VisibilityManager.LastMapPath != SDG.Unturned.Level.info.path)
        {
            EditorHelper.Instance.VisibilityManager = new VisibilityManager();
        }
        
        EditorHelper.Instance.VisibilityManager.Initialize();
    }
}