using EditorHelper.Editor;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Patchs;

[HarmonyPatch]
public class EditorLevelObjectsUIPatchs
{
    [HarmonyPatch(typeof(EditorLevelObjectsUI), MethodType.Constructor)]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void Constructor(EditorLevelObjectsUI __instance)
    {
        if (EditorHelper.Instance.ObjectsManager == null)
        {
            EditorHelper.Instance.ObjectsManager = new ObjectsManager();
        }
        
        EditorHelper.Instance.ObjectsManager.Initialize(ref __instance);
    }
}