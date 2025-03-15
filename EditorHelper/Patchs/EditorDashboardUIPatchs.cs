using EditorHelper.Editor;
using HarmonyLib;
using SDG.Unturned;

namespace EditorHelper.Patchs;

[HarmonyPatch]
public class EditorDashboardUIPatchs
{
    [HarmonyPatch(typeof(EditorDashboardUI), MethodType.Constructor)]
    [HarmonyPostfix]
    static void Constructor()
    {
        EditorHelper.Instance.EditorManager = new EditorManager();
        
        EditorHelper.Instance.EditorManager.Initialize();
    }
}