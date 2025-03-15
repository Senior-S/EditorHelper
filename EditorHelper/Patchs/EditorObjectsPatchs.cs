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
        EditorHelper.Instance.ObjectsManager.LateUpdate();
    }
    
    [HarmonyPatch(typeof(EditorObjects), "calculateHandleOffsets")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void calculateHandleOffsets()
    {
        if (EditorHelper.Instance.ObjectsManager == null)
        {
            return;
        }
        
        EditorHelper.Instance.ObjectsManager.UpdateSelectedObject();
    }
}