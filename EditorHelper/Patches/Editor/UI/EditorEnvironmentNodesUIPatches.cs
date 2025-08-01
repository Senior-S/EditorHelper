using EditorHelper.Editor.Managers;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper.Patches.Editor.UI;

[HarmonyPatch]
public class EditorEnvironmentNodesUIPatches
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