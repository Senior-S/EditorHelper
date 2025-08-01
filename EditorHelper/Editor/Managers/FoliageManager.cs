using System.Collections.Generic;
using System.Linq;
using SDG.Framework.Devkit;
using SDG.Framework.Devkit.Tools;
using SDG.Framework.Foliage;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Editor.Managers;

public class FoliageManager
{
    public FoliageManager()
    {
	    
    }
    
    public void CustomUpdate(FoliageEditor foliageEditorInstance)
    {
        Ray ray = EditorInteract.ray;
        foliageEditorInstance.isPointerOnWorld = Physics.Raycast(ray, out RaycastHit hitInfo, 8192f, (int)DevkitFoliageToolOptions.instance.surfaceMask);
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
				foliageEditorInstance.brushFalloff = Mathf.Clamp01((foliageEditorInstance.changePlanePosition - foliageEditorInstance.brushWorldPosition).magnitude / foliageEditorInstance.brushRadius);
			}
			if (foliageEditorInstance.isChangingBrushStrength)
			{
				foliageEditorInstance.brushStrength = (foliageEditorInstance.changePlanePosition - foliageEditorInstance.brushWorldPosition).magnitude / foliageEditorInstance.brushRadius;
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
			Bounds worldBounds = new Bounds(foliageEditorInstance.brushWorldPosition, new Vector3(foliageEditorInstance.brushRadius * 2f, 0f, foliageEditorInstance.brushRadius * 2f));
			float num = foliageEditorInstance.brushRadius * foliageEditorInstance.brushRadius;
			float num2 = num * foliageEditorInstance.brushFalloff * foliageEditorInstance.brushFalloff;
			float num3 = Mathf.PI * foliageEditorInstance.brushRadius * foliageEditorInstance.brushRadius;
			bool key = InputEx.GetKey(KeyCode.LeftShift);
			bool key2 = InputEx.GetKey(KeyCode.LeftControl);
			bool key3 = InputEx.GetKey(KeyCode.LeftAlt);
			if (key2 || key3 || key)
			{
				bool key4 = InputEx.GetKey(KeyCode.Mouse0);
				FoliageEditor.EFoliageRemovalFilter eFoliageRemovalFilter = FoliageEditor.EFoliageRemovalFilter.None;
				if (key)
				{
					eFoliageRemovalFilter |= FoliageEditor.EFoliageRemovalFilter.ManuallyPlaced;
				}
				if (key3)
				{
					eFoliageRemovalFilter |= FoliageEditor.EFoliageRemovalFilter.Baked;
				}
				if (key2 && eFoliageRemovalFilter == FoliageEditor.EFoliageRemovalFilter.None)
				{
					eFoliageRemovalFilter |= FoliageEditor.EFoliageRemovalFilter.ManuallyPlaced;
				}
				foliageEditorInstance.removeWeight += DevkitFoliageToolOptions.removeSensitivity * num3 * foliageEditorInstance.brushStrength * Time.deltaTime;
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
						if (key2)
						{
							if (foliageEditorInstance.selectedInstanceAsset != null)
							{
								if (tile.instances.TryGetValue(foliageEditorInstance.selectedInstanceAsset.getReferenceTo<FoliageInstancedMeshInfoAsset>(), out FoliageInstanceList? value))
								{
									foliageEditorInstance.removeInstances(tile, value, num, num2, key4, eFoliageRemovalFilter, ref sampleCount);
								}
							}
							else
							{
								if (foliageEditorInstance.selectedCollectionAsset == null)
								{
									continue;
								}
								foreach (FoliageInfoCollectionAsset.FoliageInfoCollectionElement element in foliageEditorInstance.selectedCollectionAsset.elements)
								{
									if (Assets.find(element.asset) is FoliageInstancedMeshInfoAsset foliageInstancedMeshInfoAsset && tile.instances.TryGetValue(foliageInstancedMeshInfoAsset.getReferenceTo<FoliageInstancedMeshInfoAsset>(), out FoliageInstanceList? value2))
									{
										foliageEditorInstance.removeInstances(tile, value2, num, num2, key4, eFoliageRemovalFilter, ref sampleCount);
									}
								}
							}
							continue;
						}
						foreach (KeyValuePair<AssetReference<FoliageInstancedMeshInfoAsset>, FoliageInstanceList> instance in tile.instances)
						{
							FoliageInstanceList value3 = instance.Value;
							foliageEditorInstance.removeInstances(tile, value3, num, num2, key4, eFoliageRemovalFilter, ref sampleCount);
						}
					}
				}
				{
					foreach (Vector2Int item in Regions.GetCoordinateBoundsInt(worldBounds))
					{
						List<ResourceSpawnpoint> treesOrNullInRegion = LevelGround.GetTreesOrNullInRegion(item);
						if (treesOrNullInRegion != null)
						{
							for (int num4 = treesOrNullInRegion.Count - 1; num4 >= 0; num4--)
							{
								ResourceSpawnpoint resourceSpawnpoint = treesOrNullInRegion[num4];
								FoliageEditor.EFoliageRemovalFilter eFoliageRemovalFilter2 = ((!resourceSpawnpoint.isGenerated) ? FoliageEditor.EFoliageRemovalFilter.ManuallyPlaced : FoliageEditor.EFoliageRemovalFilter.Baked);
								if (!eFoliageRemovalFilter.HasFlag(eFoliageRemovalFilter2))
								{
									continue;
								}
								if (key2)
								{
									if (foliageEditorInstance.selectedInstanceAsset != null)
									{
										if (!(foliageEditorInstance.selectedInstanceAsset is FoliageResourceInfoAsset foliageResourceInfoAsset) || !foliageResourceInfoAsset.resource.isReferenceTo(resourceSpawnpoint.asset))
										{
											continue;
										}
									}
									else if (foliageEditorInstance.selectedCollectionAsset != null)
									{
										bool flag = false;
										foreach (FoliageInfoCollectionAsset.FoliageInfoCollectionElement element2 in foliageEditorInstance.selectedCollectionAsset.elements)
										{
											if (Assets.find(element2.asset) is FoliageResourceInfoAsset foliageResourceInfoAsset2 && foliageResourceInfoAsset2.resource.isReferenceTo(resourceSpawnpoint.asset))
											{
												flag = true;
												break;
											}
										}
										if (!flag)
										{
											continue;
										}
									}
								}
								float sqrMagnitude = (resourceSpawnpoint.point - foliageEditorInstance.brushWorldPosition).sqrMagnitude;
								if (sqrMagnitude < num)
								{
									bool flag2 = sqrMagnitude < num2;
									foliageEditorInstance.previewSamples.Add(new FoliagePreviewSample(resourceSpawnpoint.point, flag2 ? Color.red : (Color.red / 2f)));
									if (key4 && flag2 && sampleCount > 0)
									{
										resourceSpawnpoint.destroy();
										treesOrNullInRegion.RemoveAt(num4);
										sampleCount--;
									}
								}
							}
						}
						if (!Regions.TryConvertVector2IntCoord(item, out byte x, out byte y))
						{
							continue;
						}
						bool flag3 = false;
						List<LevelObject> list = LevelObjects.objects[x, y];
						for (int num5 = list.Count - 1; num5 >= 0; num5--)
						{
							LevelObject levelObject = list[num5];
							if (levelObject.placementOrigin != ELevelObjectPlacementOrigin.PAINTED)
							{
								continue;
							}
							if (key2)
							{
								if (foliageEditorInstance.selectedInstanceAsset != null)
								{
									if (!(foliageEditorInstance.selectedInstanceAsset is FoliageObjectInfoAsset foliageObjectInfoAsset) || !foliageObjectInfoAsset.obj.isReferenceTo(levelObject.asset))
									{
										continue;
									}
								}
								else if (foliageEditorInstance.selectedCollectionAsset != null)
								{
									bool flag4 = false;
									foreach (FoliageInfoCollectionAsset.FoliageInfoCollectionElement element3 in foliageEditorInstance.selectedCollectionAsset.elements)
									{
										if (Assets.find(element3.asset) is FoliageObjectInfoAsset foliageObjectInfoAsset2 && foliageObjectInfoAsset2.obj.isReferenceTo(levelObject.asset))
										{
											flag4 = true;
											break;
										}
									}
									if (!flag4)
									{
										continue;
									}
								}
							}
							float sqrMagnitude2 = (levelObject.transform.position - foliageEditorInstance.brushWorldPosition).sqrMagnitude;
							if (sqrMagnitude2 < num)
							{
								bool flag5 = sqrMagnitude2 < num2;
								foliageEditorInstance.previewSamples.Add(new FoliagePreviewSample(levelObject.transform.position, flag5 ? Color.red : (Color.red / 2f)));
								if (key4 && flag5 && sampleCount > 0)
								{
									flag3 = true;
									LevelObjects.removeObject(levelObject.transform);
									sampleCount--;
								}
							}
						}
						if (flag3)
						{
							LevelHierarchy.MarkDirty();
						}
					}
					return;
				}
			}
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
				foreach (FoliageInfoCollectionAsset.FoliageInfoCollectionElement element4 in foliageEditorInstance.selectedCollectionAsset.elements)
				{
					foliageEditorInstance.addFoliage(Assets.find(element4.asset), element4.weight);
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
					foliageEditorInstance.selectedInstanceAsset.addFoliageToSurface(hitInfo.point, hitInfo.normal, clearWhenBaked: false, followRules: false, doCollisionChecks: false);
					LevelHierarchy.MarkDirty();
				}
			}
			else if (foliageEditorInstance.selectedCollectionAsset != null)
			{
				FoliageInfoAsset foliageInfoAsset = Assets.find(foliageEditorInstance.selectedCollectionAsset.elements[UnityEngine.Random.Range(0, foliageEditorInstance.selectedCollectionAsset.elements.Count)].asset);
				if (foliageInfoAsset != null)
				{
					foliageInfoAsset.addFoliageToSurface(hitInfo.point, hitInfo.normal, clearWhenBaked: false, followRules: false, doCollisionChecks: false);
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