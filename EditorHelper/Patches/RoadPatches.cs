using System;
using System.Linq;
using EditorHelper.Editor;
using EditorHelper.Editor.Managers;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Patches;

[HarmonyPatch]
public class RoadPatches
{
    [HarmonyPatch(typeof(Road), "addVertex")]
    [HarmonyPrefix]
    [UsedImplicitly]
    static bool addVertex(Road __instance, int vertexIndex, Vector3 point)
    {
        int totalJoints = LevelRoads.roads.Sum(c => c.joints.Count);

        if (totalJoints + 1 > ushort.MaxValue)
        {
            EditorHelper.Instance.EditorManager.DisplayAlert($"You can't place more road joints.{Environment.NewLine}You have exceeded the max amount of total joints ({ushort.MaxValue}).");
            return false;
        }
        
        if (__instance.joints.Count + 1 > ushort.MaxValue)
        {
            EditorHelper.Instance.EditorManager.DisplayAlert($"You can't place more road joints.{Environment.NewLine}You have exceeded the max amount of joints per road ({ushort.MaxValue}).");
            return false;
        }
        
        foreach (var road in LevelRoads.roads)
        foreach (var path in road.paths)
            path.unhighlightVertex(); 

        RoadsManager.SelectedJoints.Clear();
        RoadsManager.SelectedPaths.Clear();
        RoadsManager._primaryJoint = null;
        RoadsManager._otherOffsets.Clear();

        EditorRoads.deselect();
        
        return true;
    }
}