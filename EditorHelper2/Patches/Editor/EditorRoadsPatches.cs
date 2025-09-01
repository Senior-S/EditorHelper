using System;
using EditorHelper2.Updates.Editor;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using UnityEngine;
using Action = System.Action;

namespace EditorHelper2.Patches.Editor;

[HarmonyPatch(typeof(EditorRoads))]
public class EditorRoadsPatches
{
    /// <summary>
    /// Event invoked after <see cref="EditorRoads.select"/> is called.
    /// </summary>
    public static event Action<Transform> OnRoadSelected;
    
    /// <summary>
    /// Event invoked after <see cref="EditorRoads.deselect"/> is called.
    /// </summary>
    public static event Action OnRoadDeselected;
    
    [HarmonyPatch(nameof(EditorRoads.Update))]
    [HarmonyPrefix]
    [UsedImplicitly]
    static bool PrefixUpdate(EditorRoads __instance)
    {
        EditorRoadsUpdate.Update(__instance);

        return false;
    }
    
    [HarmonyPatch(typeof(EditorRoads), "select")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void select(Transform target)
    {
        OnRoadSelected?.Invoke(target);
    }
    
    [HarmonyPatch(typeof(EditorRoads), "deselect")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void deselect()
    {
        OnRoadDeselected?.Invoke();
    }
}