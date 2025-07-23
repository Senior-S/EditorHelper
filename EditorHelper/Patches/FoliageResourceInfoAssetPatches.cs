using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Framework.Foliage;
using SDG.Framework.Utilities;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Patches;

// 7/22/25: This issue was reported almost 2 weeks ago to Nelson, 2 preview branch have been released without a fix for this
// So I added one rq here
// Once this issue gets solved I must remove this patch.
[HarmonyPatch]
public class FoliageResourceInfoAssetPatches
{
    [HarmonyPatch(typeof(FoliageResourceInfoAsset), "getInstanceCountInVolume")]
    [HarmonyPrefix]
    [UsedImplicitly]
    static bool GetInstanceCountInVolume(FoliageResourceInfoAsset __instance, IShapeVolume volume, ref int __result)
    {
        int num = 0;
        foreach (Vector2Int item in Regions.GetCoordinateBoundsInt(volume.worldBounds))
        {
            List<ResourceSpawnpoint> resources = LevelGround.GetTreesOrNullInRegion(item);
            if(resources == null || resources.Count < 1) continue;
            
            foreach (ResourceSpawnpoint item2 in resources)
            {
                if (__instance.resource.isReferenceTo(item2.asset) && volume.containsPoint(item2.point))
                {
                    num++;
                }
            }
        }

        __result = num;
        return false;
    }
}