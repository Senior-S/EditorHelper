using System;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Patches;

[HarmonyPatch]
public class ReunObjectRemovePatches
{
    [HarmonyPatch(typeof(ReunObjectRemove), MethodType.Constructor)]
    // If I don't specify the method name and the argument types the patch doesn't work (: 
    [HarmonyPatch("ReunObjectRemove")]
    [HarmonyPatch([
        typeof(int),
        typeof(Transform),
        typeof(ObjectAsset),
        typeof(ItemAsset),
        typeof(Vector3),
        typeof(Quaternion),
        typeof(Vector3)
    ])]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void Constructor(ReunObjectRemove __instance, int newStep, Transform newModel, ObjectAsset newObjectAsset, ItemAsset newItemAsset, Vector3 newPosition, Quaternion newRotation, Vector3 newScale)
    {
        if (EditorHelper.Instance.ObjectsManager == null) return;
        
        EditorHelper.Instance.ObjectsManager.OnObjectRemoved(__instance, newModel);
    }

    [HarmonyPatch(typeof(ReunObjectRemove), "undo")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void undo(ReunObjectRemove __instance)
    {
        if (EditorHelper.Instance.ObjectsManager == null) return;
        
        EditorHelper.Instance.ObjectsManager.OnObjectRemovedUndo(__instance, __instance.model);
    }
}