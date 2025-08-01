using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Patches;

[HarmonyPatch]
public class EditorRoadsPatches
{
    [HarmonyPatch(typeof(EditorRoads), "select")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void select(Transform target)
    {
        EditorHelper.Instance.RoadsManager.Select();
    }
    
    [HarmonyPatch(typeof(EditorRoads), "deselect")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void deselect()
    {
        if (EditorHelper.Instance.RoadsManager == null)
        {
            return;
        }
        
        EditorHelper.Instance.RoadsManager.ClearSelection();
    }

    [HarmonyPatch(typeof(EditorRoads), "Update")]
    [HarmonyPrefix]
    [UsedImplicitly]
    static bool Update()
    {
        EditorHelper.Instance.RoadsManager.CustomUpdate();
        return false;
    }
}