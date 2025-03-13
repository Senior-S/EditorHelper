using EditorHelper.Editor;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Patchs;

[HarmonyPatch]
public class EditorRoadsUIPatchs
{
    [HarmonyPatch(typeof(EditorEnvironmentRoadsUI), MethodType.Constructor)]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void constructor(EditorEnvironmentRoadsUI __instance)
    {
        EditorHelper.Instance.RoadsManager = new RoadsManager();
        
        EditorHelper.Instance.RoadsManager.Initialize();
    }
}