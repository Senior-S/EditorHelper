using EditorHelper2.Extensions.Level.Objects;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Updates.Editor;

public static class EditorObjectsUpdate
{
    /// <summary>
    /// Main EditorObjects update method
    /// </summary>
    /// if you need to replace any part of the update, this method must be replaced instead and documented with date.
    /// 8/10/25: Added code for <see cref="PrecisionExtension"/>
    public static void Update(EditorObjects editorObjectsInstance)
    {
        if (!EditorObjects.isBuilding)
		{
			return;
		}
		if (Glazier.Get().ShouldGameProcessInput)
		{
			if (EditorInteract.isFlying)
			{
				if (editorObjectsInstance.isUsingHandle)
				{
					editorObjectsInstance.releaseHandle();
				}
				editorObjectsInstance.hasDragStart = false;
				if (editorObjectsInstance.isDragging)
				{
					editorObjectsInstance.stopDragging();
					EditorObjects.clearSelection();
				}
				return;
			}
			EditorObjects.handles.snapPositionInterval = EditorObjects.snapTransform;
			EditorObjects.handles.snapRotationIntervalDegrees = EditorObjects.snapRotation;
			if (EditorObjects.dragMode == EDragMode.TRANSFORM)
			{
				if (EditorObjects.wantsBoundsEditor)
				{
					EditorObjects.handles.SetPreferredMode(TransformHandles.EMode.PositionBounds);
					EditorObjects.handles.UpdateBoundsFromSelection(EditorObjects.EnumerateSelectedGameObjects());
				}
				else
				{
					EditorObjects.handles.SetPreferredMode(TransformHandles.EMode.Position);
				}
			}
			else if (EditorObjects.dragMode == EDragMode.SCALE)
			{
				if (EditorObjects.wantsBoundsEditor)
				{
					EditorObjects.handles.SetPreferredMode(TransformHandles.EMode.ScaleBounds);
					EditorObjects.handles.UpdateBoundsFromSelection(EditorObjects.EnumerateSelectedGameObjects());
				}
				else
				{
					EditorObjects.handles.SetPreferredMode(TransformHandles.EMode.Scale);
				}
			}
			else
			{
				EditorObjects.handles.SetPreferredMode(TransformHandles.EMode.Rotation);
			}
			bool flag = EditorObjects.selection.Count > 0 && EditorObjects.handles.Raycast(EditorInteract.ray);
			if (EditorObjects.selection.Count > 0)
			{
				EditorObjects.handles.Render(EditorInteract.ray);
			}
			if (editorObjectsInstance.isUsingHandle)
			{
				if (!InputEx.GetKey(ControlsSettings.primary))
				{
					editorObjectsInstance.releaseHandle();
					return;
				}
				EditorObjects.handles.wantsToSnap = InputEx.GetKey(ControlsSettings.snap);
				EditorObjects.handles.MouseMove(EditorInteract.ray);
				return;
			}
			if (InputEx.GetKeyDown(ControlsSettings.tool_0))
			{
				if (EditorObjects.dragMode != 0)
				{
					EditorObjects.dragMode = EDragMode.TRANSFORM;
				}
				else
				{
					EditorObjects.wantsBoundsEditor = !EditorObjects.wantsBoundsEditor;
				}
			}
			if (InputEx.GetKeyDown(ControlsSettings.tool_1))
			{
				EditorObjects.dragMode = EDragMode.ROTATE;
			}
			if (InputEx.GetKeyDown(ControlsSettings.tool_3))
			{
				if (EditorObjects.dragMode != EDragMode.SCALE)
				{
					EditorObjects.dragMode = EDragMode.SCALE;
				}
				else
				{
					EditorObjects.wantsBoundsEditor = !EditorObjects.wantsBoundsEditor;
				}
			}
			if ((InputEx.GetKeyDown(KeyCode.Delete) || InputEx.GetKeyDown(KeyCode.Backspace)) && EditorObjects.selection.Count > 0)
			{
				LevelObjects.step++;
				for (int i = 0; i < EditorObjects.selection.Count; i++)
				{
					LevelObjects.registerRemoveObject(EditorObjects.selection[i].transform);
				}
				EditorObjects.selection.Clear();
				EditorObjects.calculateHandleOffsets();
			}
			if (InputEx.GetKeyDown(KeyCode.Z) && InputEx.GetKey(KeyCode.LeftControl))
			{
				EditorObjects.clearSelection();
				LevelObjects.undo();
			}
			if (InputEx.GetKeyDown(KeyCode.X) && InputEx.GetKey(KeyCode.LeftControl))
			{
				EditorObjects.clearSelection();
				LevelObjects.redo();
			}
			if (InputEx.GetKeyDown(KeyCode.B) && EditorObjects.selection.Count > 0 && InputEx.GetKey(KeyCode.LeftControl))
			{
				EditorObjects.copyPosition = EditorObjects.handles.GetPivotPosition();
				EditorObjects.copyRotation = EditorObjects.handles.GetPivotRotation();
				EditorObjects.hasCopiedRotation = EditorObjects.dragCoordinate == EDragCoordinate.LOCAL;
				if (EditorObjects.selection.Count == 1)
				{
					EditorObjects.copyScale = EditorObjects.selection[0].transform.localScale;
					EditorObjects.hasCopyScale = true;
				}
				else
				{
					EditorObjects.copyScale = Vector3.one;
					EditorObjects.hasCopyScale = false;
				}
			}
			if (InputEx.GetKeyDown(KeyCode.N) && EditorObjects.selection.Count > 0 && EditorObjects.copyPosition != Vector3.zero && InputEx.GetKey(KeyCode.LeftControl))
			{
				EditorObjects.pointSelection();
				if (EditorObjects.selection.Count == 1)
				{
					EditorObjects.selection[0].transform.position = EditorObjects.copyPosition;
					if (EditorObjects.hasCopiedRotation)
					{
						EditorObjects.selection[0].transform.rotation = EditorObjects.copyRotation;
					}
					if (EditorObjects.hasCopyScale)
					{
						EditorObjects.selection[0].transform.localScale = EditorObjects.copyScale;
					}
					EditorObjects.calculateHandleOffsets();
				}
				else
				{
					EditorObjects.handles.ExternallyTransformPivot(EditorObjects.copyPosition, EditorObjects.copyRotation, EditorObjects.hasCopiedRotation);
				}
				EditorObjects.applySelection();
			}
			if (InputEx.GetKeyDown(KeyCode.C) && EditorObjects.selection.Count > 0 && InputEx.GetKey(KeyCode.LeftControl))
			{
				EditorObjects.copies.Clear();
				for (int j = 0; j < EditorObjects.selection.Count; j++)
				{
					LevelObjects.getAssetEditor(EditorObjects.selection[j].transform, out ObjectAsset? objectAsset, out ItemAsset? itemAsset);
					if (objectAsset != null || itemAsset != null)
					{
						EditorObjects.copies.Add(new EditorCopy(EditorObjects.selection[j].transform.position, EditorObjects.selection[j].transform.rotation, EditorObjects.selection[j].transform.localScale, objectAsset, itemAsset));
					}
				}
			}
			if (InputEx.GetKeyDown(KeyCode.V) && EditorObjects.copies.Count > 0 && InputEx.GetKey(KeyCode.LeftControl))
			{
				EditorObjects.clearSelection();
				LevelObjects.step++;
				for (int k = 0; k < EditorObjects.copies.Count; k++)
				{
					Transform transform = LevelObjects.registerAddObject(EditorObjects.copies[k].position, EditorObjects.copies[k].rotation, EditorObjects.copies[k].scale, EditorObjects.copies[k].objectAsset, EditorObjects.copies[k].itemAsset);
					if (transform != null)
					{
						EditorObjects.addSelection(transform);
					}
				}
			}
			if (!editorObjectsInstance.isUsingHandle)
			{
				if (InputEx.GetKeyDown(ControlsSettings.primary))
				{
					if (flag)
					{
						EditorObjects.pointSelection();
						EditorObjects.handles.MouseDown(EditorInteract.ray);
						editorObjectsInstance.isUsingHandle = true;
					}
					else if (EditorInteract.objectHit.transform != null)
					{
						if (InputEx.GetKey(ControlsSettings.modify))
						{
							if (EditorObjects.containsSelection(EditorInteract.objectHit.transform))
							{
								EditorObjects.removeSelection(EditorInteract.objectHit.transform);
							}
							else
							{
								EditorObjects.addSelection(EditorInteract.objectHit.transform);
							}
						}
						else if (EditorObjects.containsSelection(EditorInteract.objectHit.transform))
						{
							EditorObjects.clearSelection();
						}
						else
						{
							EditorObjects.clearSelection();
							EditorObjects.addSelection(EditorInteract.objectHit.transform);
						}
					}
					else
					{
						if (!editorObjectsInstance.isDragging)
						{
							editorObjectsInstance.hasDragStart = true;
							editorObjectsInstance.dragStartViewportPoint = InputEx.NormalizedMousePosition;
							editorObjectsInstance.dragStartScreenPoint = Input.mousePosition;
						}
						if (!InputEx.GetKey(ControlsSettings.modify))
						{
							EditorObjects.clearSelection();
						}
					}
				}
				else if (InputEx.GetKey(ControlsSettings.primary) && editorObjectsInstance.hasDragStart)
				{
					editorObjectsInstance.dragEndViewportPoint = InputEx.NormalizedMousePosition;
					editorObjectsInstance.dragEndScreenPoint = Input.mousePosition;
					if (editorObjectsInstance.isDragging || Mathf.Abs(editorObjectsInstance.dragEndScreenPoint.x - editorObjectsInstance.dragStartScreenPoint.x) > 50f || Mathf.Abs(editorObjectsInstance.dragEndScreenPoint.x - editorObjectsInstance.dragStartScreenPoint.x) > 50f)
					{
						Vector2 min = editorObjectsInstance.dragStartViewportPoint;
						Vector2 max = editorObjectsInstance.dragEndViewportPoint;
						if (max.x < min.x)
						{
							(max.x, min.x) = (min.x, max.x);
						}
						if (max.y < min.y)
						{
							(max.y, min.y) = (min.y, max.y);
						}
						EditorObjects.onDragStarted?.Invoke(min, max);
						if (!editorObjectsInstance.isDragging)
						{
							editorObjectsInstance.isDragging = true;
							EditorObjects.dragable.Clear();
							byte regionX = SDG.Unturned.Editor.editor.area.region_x;
							byte regionY = SDG.Unturned.Editor.editor.area.region_y;
							if (Regions.checkSafe(regionX, regionY))
							{
								for (int l = regionX - 1; l <= regionX + 1; l++)
								{
									for (int m = regionY - 1; m <= regionY + 1; m++)
									{
										if (!Regions.checkSafe((byte)l, (byte)m) || !LevelObjects.regions[l, m])
										{
											continue;
										}
										for (int n = 0; n < LevelObjects.objects[l, m].Count; n++)
										{
											LevelObject levelObject = LevelObjects.objects[l, m][n];
											if (!(levelObject.transform == null))
											{
												Vector3 newScreen = MainCamera.instance.WorldToViewportPoint(levelObject.transform.position);
												if (!(newScreen.z < 0f))
												{
													EditorObjects.dragable.Add(new EditorDrag(levelObject.transform, newScreen));
												}
											}
										}
										for (int num = 0; num < LevelObjects.buildables[l, m].Count; num++)
										{
											LevelBuildableObject levelBuildableObject = LevelObjects.buildables[l, m][num];
											if (!(levelBuildableObject.transform == null))
											{
												Vector3 newScreen2 = MainCamera.instance.WorldToViewportPoint(levelBuildableObject.transform.position);
												if (!(newScreen2.z < 0f))
												{
													EditorObjects.dragable.Add(new EditorDrag(levelBuildableObject.transform, newScreen2));
												}
											}
										}
									}
								}
							}
						}
						if (!InputEx.GetKey(ControlsSettings.modify))
						{
							for (int num2 = 0; num2 < EditorObjects.selection.Count; num2++)
							{
								Vector3 vector = MainCamera.instance.WorldToViewportPoint(EditorObjects.selection[num2].transform.position);
								if (vector.z < 0f)
								{
									EditorObjects.removeSelection(EditorObjects.selection[num2].transform);
								}
								else if (vector.x < min.x || vector.y < min.y || vector.x > max.x || vector.y > max.y)
								{
									EditorObjects.removeSelection(EditorObjects.selection[num2].transform);
								}
							}
						}
						for (int num3 = 0; num3 < EditorObjects.dragable.Count; num3++)
						{
							EditorDrag editorDrag = EditorObjects.dragable[num3];
							if (!(editorDrag.transform == null) && !EditorObjects.containsSelection(editorDrag.transform) && !(editorDrag.screen.x < min.x) && !(editorDrag.screen.y < min.y) && !(editorDrag.screen.x > max.x) && !(editorDrag.screen.y > max.y))
							{
								EditorObjects.addSelection(editorDrag.transform);
							}
						}
					}
				}
				if (EditorObjects.selection.Count > 0)
				{
					if (InputEx.GetKeyDown(ControlsSettings.tool_2) && EditorInteract.worldHit.transform != null)
					{
						EditorObjects.pointSelection();
						Vector3 point = EditorInteract.worldHit.point;
						if (InputEx.GetKey(ControlsSettings.snap))
						{
							point += EditorInteract.worldHit.normal * EditorObjects.snapTransform;
						}
						Quaternion pivotRotation = EditorObjects.handles.GetPivotRotation();
						EditorObjects.handles.ExternallyTransformPivot(point, pivotRotation, modifyRotation: false);
						EditorObjects.applySelection();
					}
					if (InputEx.GetKeyDown(ControlsSettings.focus))
					{
						MainCamera.instance.transform.parent.position = EditorObjects.handles.GetPivotPosition() - 15f * MainCamera.instance.transform.forward;
					}
				}
				else if (EditorInteract.worldHit.transform != null)
				{
					if (EditorInteract.worldHit.transform.CompareTag("Large") || EditorInteract.worldHit.transform.CompareTag("Medium") || EditorInteract.worldHit.transform.CompareTag("Small") || EditorInteract.worldHit.transform.CompareTag("Barricade") || EditorInteract.worldHit.transform.CompareTag("Structure"))
					{
						LevelObjects.getAssetEditor(EditorInteract.worldHit.transform, out ObjectAsset? objectAsset2, out ItemAsset? itemAsset2);
						if (objectAsset2 != null)
						{
							EditorUI.hint(EEditorMessage.FOCUS, objectAsset2.objectName + "\n" + (objectAsset2.origin?.name ?? "Unknown"));
						}
						else if (itemAsset2 != null)
						{
							EditorUI.hint(EEditorMessage.FOCUS, itemAsset2.itemName + "\n" + (itemAsset2.origin?.name ?? "Unknown"));
						}
					}
					if (InputEx.GetKeyDown(ControlsSettings.tool_2))
					{
						Vector3 point2 = EditorInteract.worldHit.point;
						if (InputEx.GetKey(ControlsSettings.snap))
						{
							point2 += EditorInteract.worldHit.normal * EditorObjects.snapTransform;
						}
						Quaternion rotation = Quaternion.Euler(-90f, 0f, 0f);
						EditorObjects.handles.SetPreferredPivot(point2, rotation);
						if (EditorObjects.selectedObjectAsset != null || EditorObjects.selectedItemAsset != null)
						{
							LevelObjects.step++;
							Transform transform2 = LevelObjects.registerAddObject(point2, rotation, Vector3.one, EditorObjects.selectedObjectAsset, EditorObjects.selectedItemAsset);
							if (transform2 != null)
							{
								EditorObjects.addSelection(transform2);
							}
						}
					}
				}
			}
		}
		if (InputEx.GetKeyUp(ControlsSettings.primary))
		{
			editorObjectsInstance.hasDragStart = false;
			if (editorObjectsInstance.isDragging)
			{
				editorObjectsInstance.stopDragging();
			}
		}
		
		#region PreicisionExtension
		PrecisionExtensionUI.Instance?.ChangeButtonsVisibility(EditorObjects.selection.Count == 1);
		#endregion
    }
}