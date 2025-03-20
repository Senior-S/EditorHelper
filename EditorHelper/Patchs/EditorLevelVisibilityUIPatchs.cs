using EditorHelper.Editor;
using HarmonyLib;
using SDG.Unturned;

namespace EditorHelper.Patchs;

[HarmonyPatch]
public class EditorLevelVisibilityUIPatchs
{
    [HarmonyPatch(typeof(EditorLevelVisibilityUI), MethodType.Constructor)]
    [HarmonyPostfix]
    static void Constructor()
    {
        EditorHelper.Instance.VisibilityManager ??= new VisibilityManager();
        
        EditorHelper.Instance.VisibilityManager.Initialize();
    }
}