using EditorHelper2.common.Helpers;
using EditorHelper2.Extensions.Editor.Dashboard;
using EditorHelper2.Extensions.PlayerUIs;
using EditorHelper2.Loader;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EditorHelper2.Patches.Provider;

[HarmonyPatch(typeof(SDG.Unturned.Provider))]
public class ProviderPatches
{
    [HarmonyPatch("loadPlayerSpawn")]
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void LoadPlayerSpawnPostfix(SteamPlayerID playerID, ref Vector3 point, ref byte angle, ref EPlayerStance initialStance)
    {
        if (ExtensionManager.IsEnabled<SingleplayerExtension>() &&
            SingleplayerSharedClass.LevelInfo != null && SingleplayerSharedClass.LevelInfo.Equals(SDG.Unturned.Level.info))
        {
            const int layerMask = RayMasks.LARGE | RayMasks.MEDIUM | RayMasks.GROUND | RayMasks.RESOURCE | RayMasks.ENVIRONMENT | RayMasks.BARRICADE | RayMasks.STRUCTURE;
            if (Physics.Raycast(SingleplayerSharedClass.CameraPosition, Vector3.down, out var hit, PlayerMovement.HEIGHT_STAND, layerMask, QueryTriggerInteraction.Ignore))
                point = hit.point + new Vector3(0f, 0.1f, 0f);
            else
                point = SingleplayerSharedClass.CameraPosition - new Vector3(0f, PlayerMovement.HEIGHT_STAND, 0f);

            PlayerStance.getStanceForPosition(point, ref initialStance);

            angle = MeasurementTool.angleToByte(SingleplayerSharedClass.CameraRotation);

            if (!ExtensionManager.IsEnabled<EditorExtension>()) SingleplayerSharedClass.LevelInfo = null;
        }
    }
}
