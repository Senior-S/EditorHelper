using EditorHelper.Editor;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper.Patchs;

[HarmonyPatch]
public class EditorEnvironmentNodesUIPatchs
{
    [HarmonyPatch(typeof(EditorEnvironmentNodesUI), MethodType.Constructor)]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void Constructor(EditorEnvironmentNodesUI __instance)
    {
        EditorHelper.Instance.NodesManager = new NodesManager();
        
        EditorHelper.Instance.NodesManager.Initialize(ref __instance);
    }
}