﻿using EditorHelper.Editor;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper.Patches;

[HarmonyPatch]
public class EditorSpawnsPatches
{
    [HarmonyPatch(typeof(EditorSpawns), MethodType.Constructor)]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void constructor(EditorSpawns __instance)
    {
        EditorHelper.Instance.VehicleSpawnsManager ??= new VehicleSpawnsManager();
    }
    
    [HarmonyPatch(typeof(EditorSpawns), "Update")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void Update(EditorSpawns __instance)
    {
        if (EditorHelper.Instance.VehicleSpawnsManager == null) return;
        
        EditorHelper.Instance.VehicleSpawnsManager.CustomUpdateUI();
    }
}