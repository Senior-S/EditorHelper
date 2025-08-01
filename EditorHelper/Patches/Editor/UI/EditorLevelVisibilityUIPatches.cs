using EditorHelper.Editor;
using HarmonyLib;
using SDG.Unturned;

namespace EditorHelper.Patches;

[HarmonyPatch]
public class EditorLevelVisibilityUIPatches
{
    [HarmonyPatch(typeof(EditorLevelVisibilityUI), "open")]
    [HarmonyPostfix]
    static void openPostfix()
    {
        if (EditorHelper.Instance.VisibilityManager == null || 
            EditorHelper.Instance.VisibilityManager.LastMapPath != Level.info.path)
        {
            EditorHelper.Instance.VisibilityManager = new VisibilityManager();
        }
        
        EditorHelper.Instance.VisibilityManager.Initialize();
    }
}