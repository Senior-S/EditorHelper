using HarmonyLib;
using JetBrains.Annotations;
using SDG.Framework.Devkit;
using SDG.Unturned;

namespace EditorHelper.Patchs;

[HarmonyPatch]
public class LocationDevkitNodeSystemPatchs
{
    [HarmonyPatch(typeof(LocationDevkitNodeSystem), "OnUpdateGizmos")]
    [HarmonyPrefix]
    [UsedImplicitly]
    static bool OnUpdateGizmos(LocationDevkitNodeSystem __instance)
    {
        if (!SpawnpointSystemV2.Get().IsVisible || !Level.isEditor || EditorHelper.Instance.NodesManager == null)
        {
            return true;
        }
        
        EditorHelper.Instance.NodesManager.CustomUpdate();
        return false;
    }
}