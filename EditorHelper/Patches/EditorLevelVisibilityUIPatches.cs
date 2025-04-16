using EditorHelper.Editor;
using HarmonyLib;
using SDG.Unturned;

namespace EditorHelper.Patches;

[HarmonyPatch]
public class EditorLevelVisibilityUIPatches
{
    [HarmonyPatch(typeof(EditorLevelVisibilityUI), MethodType.Constructor)]
    [HarmonyPostfix]
    static void Constructor()
    {
        EditorHelper.Instance.VisibilityManager ??= new VisibilityManager();
        
        EditorHelper.Instance.VisibilityManager.Initialize();
    }
}