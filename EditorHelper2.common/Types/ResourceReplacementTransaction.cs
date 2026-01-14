using System;
using System.Collections.Generic;
using SDG.Framework.Devkit.Transactions;
using SDG.Framework.Foliage;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.common.Types;

public struct ReplacedResourceData
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
    public Guid OriginalAssetGuid;
    public Guid NewAssetGuid;
    public bool IsGenerated;
}

public struct ReplacedFoliageData
{
    public FoliageCoord TileCoord;
    public Matrix4x4 OriginalMatrix;
    public Matrix4x4 NewMatrix;
    public Guid OriginalAssetGuid;
    public Guid NewAssetGuid;
    public bool ClearWhenBaked;
}

public class ResourceReplacementTransaction : IDevkitTransaction
{
    private readonly List<ReplacedResourceData> _replacedResources;
    private readonly List<ReplacedFoliageData> _replacedFoliage;

    public bool delta => true;

    public ResourceReplacementTransaction(
        List<ReplacedResourceData> replacedResources,
        List<ReplacedFoliageData> replacedFoliage)
    {
        _replacedResources = replacedResources;
        _replacedFoliage = replacedFoliage;
    }

    public void undo()
    {
        UnturnedLog.info($"[ResourceReplacementTransaction] Undoing {_replacedResources.Count} resources and {_replacedFoliage.Count} foliage changes");

        foreach (ReplacedResourceData data in _replacedResources)
        {
            List<ResourceSpawnpoint> allTrees = [];
            LevelGround.GatherAllTrees(allTrees);

            ResourceSpawnpoint? existing = allTrees.Find(t =>
                t.asset != null &&
                t.asset.GUID == data.NewAssetGuid &&
                Vector3.Distance(t.point, data.Position) < 0.01f);

            if (existing != null)
            {
                UnturnedLog.info($"[ResourceReplacementTransaction] Removing new resource at {data.Position}");
                existing.destroy();
                Vector2Int coord = Regions.GetCoordinateVector2Int(existing.point);
                List<ResourceSpawnpoint>? regionTrees = LevelGround.GetTreesOrNullInRegion(coord);
                regionTrees?.Remove(existing);
            }
            else
            {
                UnturnedLog.warn($"[ResourceReplacementTransaction] Could not find resource to remove at {data.Position}");
            }

            UnturnedLog.info($"[ResourceReplacementTransaction] Restoring original resource {data.OriginalAssetGuid} at {data.Position}");
            LevelGround.addSpawn(data.Position, data.Rotation, data.Scale, data.OriginalAssetGuid, data.IsGenerated);
        }

        foreach (ReplacedFoliageData data in _replacedFoliage)
        {
            if (!FoliageSystem.tiles.TryGetValue(data.TileCoord, out FoliageTile? tile))
            {
                 UnturnedLog.warn($"[ResourceReplacementTransaction] Tile not found at {data.TileCoord}");
                 continue;
            }

            UnturnedLog.info($"[ResourceReplacementTransaction] Undoing foliage at {data.TileCoord}");

            AssetReference<FoliageInstancedMeshInfoAsset> newRef = new(data.NewAssetGuid);
            if (tile.instances.TryGetValue(newRef, out FoliageInstanceList? newList))
            {
                for (int i = 0; i < newList.matrices.Count; i++)
                {
                    for (int j = newList.matrices[i].Count - 1; j >= 0; j--)
                    {
                        if (MatricesEqual(newList.matrices[i][j], data.NewMatrix))
                        {
                            tile.removeInstance(newList, i, j);
                            break;
                        }
                    }
                }
            }

            AssetReference<FoliageInstancedMeshInfoAsset> originalRef = new(data.OriginalAssetGuid);
            FoliageInstanceGroup originalInstance = new(originalRef, data.OriginalMatrix, data.ClearWhenBaked);
            tile.addInstance(originalInstance);
        }
    }

    public void redo()
    {
        foreach (ReplacedResourceData data in _replacedResources)
        {
            List<ResourceSpawnpoint> allTrees = [];
            LevelGround.GatherAllTrees(allTrees);

            ResourceSpawnpoint? existing = allTrees.Find(t =>
                t.asset != null &&
                t.asset.GUID == data.OriginalAssetGuid &&
                Vector3.Distance(t.point, data.Position) < 0.01f);

            if (existing != null)
            {
                existing.destroy();
                Vector2Int coord = Regions.GetCoordinateVector2Int(existing.point);
                List<ResourceSpawnpoint>? regionTrees = LevelGround.GetTreesOrNullInRegion(coord);
                regionTrees?.Remove(existing);
            }

            ResourceAsset? targetAsset = SDG.Unturned.Assets.find<ResourceAsset>(data.NewAssetGuid);
            if (targetAsset != null)
            {
                targetAsset.GetLegacyRotationAndScale(data.Position, out Quaternion rotation, out Vector3 scale);
                LevelGround.addSpawn(data.Position, rotation, scale, data.NewAssetGuid, data.IsGenerated);
            }
        }

        foreach (ReplacedFoliageData data in _replacedFoliage)
        {
            if (!FoliageSystem.tiles.TryGetValue(data.TileCoord, out FoliageTile? tile))
                continue;

            AssetReference<FoliageInstancedMeshInfoAsset> originalRef = new(data.OriginalAssetGuid);
            if (tile.instances.TryGetValue(originalRef, out FoliageInstanceList? originalList))
            {
                for (int i = 0; i < originalList.matrices.Count; i++)
                {
                    for (int j = originalList.matrices[i].Count - 1; j >= 0; j--)
                    {
                        if (MatricesEqual(originalList.matrices[i][j], data.OriginalMatrix))
                        {
                            tile.removeInstance(originalList, i, j);
                            break;
                        }
                    }
                }
            }

            AssetReference<FoliageInstancedMeshInfoAsset> newRef = new(data.NewAssetGuid);
            FoliageInstanceGroup newInstance = new(newRef, data.NewMatrix, data.ClearWhenBaked);
            tile.addInstance(newInstance);
        }
    }

    public void begin()
    {
    }

    public void end()
    {
    }

    public void forget()
    {
    }

    private static bool MatricesEqual(Matrix4x4 a, Matrix4x4 b)
    {
        return Vector3.Distance(a.GetPosition(), b.GetPosition()) < 0.01f;
    }
}
