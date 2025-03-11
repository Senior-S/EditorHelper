using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Patchs;

[HarmonyPatch]
public class EditorObjectsPatchs
{
    [HarmonyPatch(typeof(EditorObjects), "addSelection")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void addSelection(Transform select)
    {
        EditorHelper.Instance.ObjectsManager.SelectObject(select);
        EditorHelper.Instance.ObjectsManager.UnhighlightAll();
    }
    
    [HarmonyPatch(typeof(EditorObjects), "Update")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void Update()
    {        
        EditorHelper.Instance.ObjectsManager.ChangeButtonsVisibility(EditorObjects.selection.Count == 1);
    }
    
    [HarmonyPatch(typeof(EditorObjects), "OnHandleTransformed")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void OnHandleTransformed()
    {
        EditorHelper.Instance.ObjectsManager.UpdateSelectedObject();
    }
}