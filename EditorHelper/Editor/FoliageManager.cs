using System.Collections.Generic;
using System.Linq;
using EditorHelper.Builders;
using EditorHelper.Models;
using SDG.Framework.Devkit;
using SDG.Framework.Devkit.Tools;
using SDG.Framework.Foliage;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Editor;

public class FoliageManager
{
    public FoliageManager()
    {
    }
    
    public void CustomUpdate(FoliageEditor foliageEditorInstance)
    {
        Ray ray = EditorInteract.ray;
        foliageEditorInstance.isPointerOnWorld =
            Physics.Raycast(ray, out RaycastHit hitInfo, 8192f, (int)DevkitFoliageToolOptions.instance.surfaceMask);
        foliageEditorInstance.pointerWorldPosition = hitInfo.point;
        foliageEditorInstance.previewSamples.Clear();
        if (!EditorInteract.isFlying && Glazier.Get().ShouldGameProcessInput)
        {
            if (InputEx.GetKeyDown(KeyCode.Q))
            {
                foliageEditorInstance.mode = FoliageEditor.EFoliageMode.PAINT;
            }

            if (InputEx.GetKeyDown(KeyCode.W))
            {
                foliageEditorInstance.mode = FoliageEditor.EFoliageMode.EXACT;
            }

            if (InputEx.GetKeyDown(KeyCode.E))
            {
                foliageEditorInstance.mode = FoliageEditor.EFoliageMode.BAKE;
            }

            if (foliageEditorInstance.mode == FoliageEditor.EFoliageMode.PAINT)
            {
                if (InputEx.GetKeyDown(KeyCode.B))
                {
                    foliageEditorInstance.isChangingBrushRadius = true;
                    foliageEditorInstance.beginChangeHotkeyTransaction();
                }

                if (InputEx.GetKeyDown(KeyCode.F))
                {
                    foliageEditorInstance.isChangingBrushFalloff = true;
                    foliageEditorInstance.beginChangeHotkeyTransaction();
                }

                if (InputEx.GetKeyDown(KeyCode.V))
                {
                    foliageEditorInstance.isChangingBrushStrength = true;
                    foliageEditorInstance.beginChangeHotkeyTransaction();
                }
            }
        }

        if (InputEx.GetKeyUp(KeyCode.B))
        {
            foliageEditorInstance.isChangingBrushRadius = false;
            foliageEditorInstance.endChangeHotkeyTransaction();
        }

        if (InputEx.GetKeyUp(KeyCode.F))
        {
            foliageEditorInstance.isChangingBrushFalloff = false;
            foliageEditorInstance.endChangeHotkeyTransaction();
        }

        if (InputEx.GetKeyUp(KeyCode.V))
        {
            foliageEditorInstance.isChangingBrushStrength = false;
            foliageEditorInstance.endChangeHotkeyTransaction();
        }

        if (foliageEditorInstance.isChangingBrush)
        {
            Plane plane = default(Plane);
            plane.SetNormalAndPosition(Vector3.up, foliageEditorInstance.brushWorldPosition);
            plane.Raycast(ray, out float enter);
            foliageEditorInstance.changePlanePosition = ray.origin + ray.direction * enter;
            if (foliageEditorInstance.isChangingBrushRadius)
            {
                foliageEditorInstance.brushRadius = (foliageEditorInstance.changePlanePosition - foliageEditorInstance.brushWorldPosition).magnitude;
            }

            if (foliageEditorInstance.isChangingBrushFalloff)
            {
                foliageEditorInstance.brushFalloff =
                    Mathf.Clamp01((foliageEditorInstance.changePlanePosition - foliageEditorInstance.brushWorldPosition).magnitude /
                                  foliageEditorInstance.brushRadius);
            }

            if (foliageEditorInstance.isChangingBrushStrength)
            {
                foliageEditorInstance.brushStrength =
                    (foliageEditorInstance.changePlanePosition - foliageEditorInstance.brushWorldPosition).magnitude /
                    foliageEditorInstance.brushRadius;
            }
        }
        else
        {
            foliageEditorInstance.brushWorldPosition = foliageEditorInstance.pointerWorldPosition;
        }

        foliageEditorInstance.isBrushVisible = foliageEditorInstance.isPointerOnWorld || foliageEditorInstance.isChangingBrush;
        if (EditorInteract.isFlying || !Glazier.Get().ShouldGameProcessInput)
        {
            return;
        }
        
        if (EditorInteract.worldHit.transform)
        {
            if (EditorInteract.worldHit.transform.CompareTag("Resource"))
            {
                Transform hit = EditorInteract.worldHit.transform;
                if (hit.parent)
                {
                    while (hit.parent)
                    {
                        hit = hit.parent;
                    }
                }

                ResourceSpawnpoint spawnpoint = GetTree(hit);
                if (spawnpoint is { asset: not null })
                {
                    EditorUI.hint(EEditorMessage.FOCUS, spawnpoint.asset.resourceName + "\n" + spawnpoint.asset.GetOriginName());
                }
            }
        }
        
        if (foliageEditorInstance.mode == FoliageEditor.EFoliageMode.PAINT)
        {
            Bounds worldBounds = new Bounds(foliageEditorInstance.brushWorldPosition,
                new Vector3(foliageEditorInstance.brushRadius * 2f, 0f, foliageEditorInstance.brushRadius * 2f));
            float num = foliageEditorInstance.brushRadius * foliageEditorInstance.brushRadius;
            float num2 = num * foliageEditorInstance.brushFalloff * foliageEditorInstance.brushFalloff;
            float num3 = Mathf.PI * foliageEditorInstance.brushRadius * foliageEditorInstance.brushRadius;
            bool key = InputEx.GetKey(KeyCode.LeftControl);
            bool flag = key || InputEx.GetKey(KeyCode.LeftAlt);
            if (key || flag || InputEx.GetKey(KeyCode.LeftShift))
            {
                foliageEditorInstance.removeWeight +=
                    DevkitFoliageToolOptions.removeSensitivity * num3 * foliageEditorInstance.brushStrength * Time.deltaTime;
                int sampleCount = 0;
                if (foliageEditorInstance.removeWeight > 1f)
                {
                    sampleCount = Mathf.FloorToInt(foliageEditorInstance.removeWeight);
                    foliageEditorInstance.removeWeight -= sampleCount;
                }

                FoliageBounds foliageBounds = new FoliageBounds(worldBounds);
                for (int i = foliageBounds.min.x; i <= foliageBounds.max.x; i++)
                {
                    for (int j = foliageBounds.min.y; j <= foliageBounds.max.y; j++)
                    {
                        FoliageTile tile = FoliageSystem.getTile(new FoliageCoord(i, j));
                        if (tile == null)
                        {
                            continue;
                        }

                        if (key)
                        {
                            if (foliageEditorInstance.selectedInstanceAsset != null)
                            {
                                if (tile.instances.TryGetValue(
                                        foliageEditorInstance.selectedInstanceAsset.getReferenceTo<FoliageInstancedMeshInfoAsset>(), out var value))
                                {
                                    foliageEditorInstance.removeInstances(tile, value, num, num2, flag, ref sampleCount);
                                }
                            }
                            else
                            {
                                if (foliageEditorInstance.selectedCollectionAsset == null)
                                {
                                    continue;
                                }

                                foreach (FoliageInfoCollectionAsset.FoliageInfoCollectionElement element in foliageEditorInstance
                                             .selectedCollectionAsset.elements)
                                {
                                    if (Assets.find(element.asset) is FoliageInstancedMeshInfoAsset foliageInstancedMeshInfoAsset &&
                                        tile.instances.TryGetValue(foliageInstancedMeshInfoAsset.getReferenceTo<FoliageInstancedMeshInfoAsset>(),
                                            out var value2))
                                    {
                                        foliageEditorInstance.removeInstances(tile, value2, num, num2, flag, ref sampleCount);
                                    }
                                }
                            }

                            continue;
                        }

                        foreach (KeyValuePair<AssetReference<FoliageInstancedMeshInfoAsset>, FoliageInstanceList> instance in tile.instances)
                        {
                            FoliageInstanceList value3 = instance.Value;
                            foliageEditorInstance.removeInstances(tile, value3, num, num2, flag, ref sampleCount);
                        }
                    }
                }

                RegionBounds regionBounds = new RegionBounds(worldBounds);
                for (byte b = regionBounds.min.x; b <= regionBounds.max.x; b++)
                {
                    for (byte b2 = regionBounds.min.y; b2 <= regionBounds.max.y; b2++)
                    {
                        List<ResourceSpawnpoint> list = LevelGround.trees[b, b2];
                        for (int num4 = list.Count - 1; num4 >= 0; num4--)
                        {
                            ResourceSpawnpoint resourceSpawnpoint = list[num4];
                            if (resourceSpawnpoint.isGenerated && !flag)
                            {
                                continue;
                            }

                            if (key)
                            {
                                if (foliageEditorInstance.selectedInstanceAsset != null)
                                {
                                    if (!(foliageEditorInstance.selectedInstanceAsset is FoliageResourceInfoAsset foliageResourceInfoAsset) ||
                                        !foliageResourceInfoAsset.resource.isReferenceTo(resourceSpawnpoint.asset))
                                    {
                                        continue;
                                    }
                                }
                                else if (foliageEditorInstance.selectedCollectionAsset != null)
                                {
                                    bool flag2 = false;
                                    foreach (FoliageInfoCollectionAsset.FoliageInfoCollectionElement element2 in foliageEditorInstance
                                                 .selectedCollectionAsset.elements)
                                    {
                                        if (Assets.find(element2.asset) is FoliageResourceInfoAsset foliageResourceInfoAsset2 &&
                                            foliageResourceInfoAsset2.resource.isReferenceTo(resourceSpawnpoint.asset))
                                        {
                                            flag2 = true;
                                            break;
                                        }
                                    }

                                    if (!flag2)
                                    {
                                        continue;
                                    }
                                }
                            }

                            float sqrMagnitude = (resourceSpawnpoint.point - foliageEditorInstance.brushWorldPosition).sqrMagnitude;
                            if (sqrMagnitude < num)
                            {
                                bool flag3 = sqrMagnitude < num2;
                                foliageEditorInstance.previewSamples.Add(new FoliagePreviewSample(resourceSpawnpoint.point,
                                    flag3 ? Color.red : (Color.red / 2f)));
                                if (InputEx.GetKey(KeyCode.Mouse0) && flag3 && sampleCount > 0)
                                {
                                    resourceSpawnpoint.destroy();
                                    list.RemoveAt(num4);
                                    sampleCount--;
                                }
                            }
                        }

                        bool flag4 = false;
                        List<LevelObject> list2 = LevelObjects.objects[b, b2];
                        for (int num5 = list2.Count - 1; num5 >= 0; num5--)
                        {
                            LevelObject levelObject = list2[num5];
                            if (levelObject.placementOrigin != ELevelObjectPlacementOrigin.PAINTED)
                            {
                                continue;
                            }

                            if (key)
                            {
                                if (foliageEditorInstance.selectedInstanceAsset != null)
                                {
                                    if (!(foliageEditorInstance.selectedInstanceAsset is FoliageObjectInfoAsset foliageObjectInfoAsset) ||
                                        !foliageObjectInfoAsset.obj.isReferenceTo(levelObject.asset))
                                    {
                                        continue;
                                    }
                                }
                                else if (foliageEditorInstance.selectedCollectionAsset != null)
                                {
                                    bool flag5 = false;
                                    foreach (FoliageInfoCollectionAsset.FoliageInfoCollectionElement element3 in foliageEditorInstance
                                                 .selectedCollectionAsset.elements)
                                    {
                                        if (Assets.find(element3.asset) is FoliageObjectInfoAsset foliageObjectInfoAsset2 &&
                                            foliageObjectInfoAsset2.obj.isReferenceTo(levelObject.asset))
                                        {
                                            flag5 = true;
                                            break;
                                        }
                                    }

                                    if (!flag5)
                                    {
                                        continue;
                                    }
                                }
                            }

                            float sqrMagnitude2 = (levelObject.transform.position - foliageEditorInstance.brushWorldPosition).sqrMagnitude;
                            if (sqrMagnitude2 < num)
                            {
                                bool flag6 = sqrMagnitude2 < num2;
                                foliageEditorInstance.previewSamples.Add(new FoliagePreviewSample(levelObject.transform.position,
                                    flag6 ? Color.red : (Color.red / 2f)));
                                if (InputEx.GetKey(KeyCode.Mouse0) && flag6 && sampleCount > 0)
                                {
                                    flag4 = true;
                                    LevelObjects.removeObject(levelObject.transform);
                                    sampleCount--;
                                }
                            }
                        }

                        if (flag4)
                        {
                            LevelHierarchy.MarkDirty();
                        }
                    }
                }
            }
            else
            {
                if (!InputEx.GetKey(KeyCode.Mouse0))
                {
                    return;
                }

                if (foliageEditorInstance.selectedInstanceAsset != null)
                {
                    foliageEditorInstance.addFoliage(foliageEditorInstance.selectedInstanceAsset, 1f);
                }
                else
                {
                    if (foliageEditorInstance.selectedCollectionAsset == null)
                    {
                        return;
                    }

                    foreach (FoliageInfoCollectionAsset.FoliageInfoCollectionElement element4 in foliageEditorInstance.selectedCollectionAsset
                                 .elements)
                    {
                        foliageEditorInstance.addFoliage(Assets.find(element4.asset), element4.weight);
                    }
                }
            }
        }
        else
        {
            if (foliageEditorInstance.mode != FoliageEditor.EFoliageMode.EXACT || !InputEx.GetKeyDown(KeyCode.Mouse0))
            {
                return;
            }

            if (foliageEditorInstance.selectedInstanceAsset != null)
            {
                if (foliageEditorInstance.selectedInstanceAsset != null)
                {
                    foliageEditorInstance.selectedInstanceAsset.addFoliageToSurface(hitInfo.point, hitInfo.normal, clearWhenBaked: false,
                        followRules: false);
                    LevelHierarchy.MarkDirty();
                }
            }
            else if (foliageEditorInstance.selectedCollectionAsset != null)
            {
                FoliageInfoAsset foliageInfoAsset = Assets.find(foliageEditorInstance.selectedCollectionAsset
                    .elements[UnityEngine.Random.Range(0, foliageEditorInstance.selectedCollectionAsset.elements.Count)].asset);
                if (foliageInfoAsset != null)
                {
                    foliageInfoAsset.addFoliageToSurface(hitInfo.point, hitInfo.normal, clearWhenBaked: false, followRules: false);
                    LevelHierarchy.MarkDirty();
                }
            }
        }
    }

    private ResourceSpawnpoint GetTree(Transform model)
    {
        List<List<ResourceSpawnpoint>> resources = LevelGround._trees.Cast<List<ResourceSpawnpoint>>().ToList();
        // I can probably convert this into a linq one line but as far as I'm aware linq may affect performance and due to this function is being
        // called in Update I prefer to avoid any type of performance issue.
        foreach (List<ResourceSpawnpoint> spawnpointList in resources)
        {
            if (spawnpointList.Any(c => c != null && c.model == model))
            {
                return spawnpointList.FirstOrDefault(c => c != null && c.model == model);
            }
        }

        return null;
    }
}