using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Patchs;

[HarmonyPatch]
public class EditorObjectsPatchs
{
    [HarmonyPatch(typeof(EditorObjects), "pointSelection")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void pointSelection()
    {
        ProjectMain.UnhighlightAll();
    }

    [HarmonyPatch(typeof(EditorObjects), "addSelection")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void addSelection(Transform select)
    {
        ProjectMain.UnhighlightAll(select);
    }
    
    [HarmonyPatch(typeof(EditorObjects), "clearSelection")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void clearSelection()
    {
        ProjectMain.UnhighlightAll();
    }
}