using System;
using DanielWillett.UITools;
using EditorHelper2.Extensions.Environment.Roads;
using EditorHelper2.Loader;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Updates.Editor;

public static class EditorRoadsUpdate
{
    /// <summary>
    /// Main EditorRoads update method
    /// </summary>
    /// if you need to replace any part of the update, this method must be replaced instead and documented with date.
    /// 8/31/25: Added code for <see cref="HandlesExtension"/> *prefix*
    /// 8/31/25: Added code for <see cref="HandlesExtension"/> *postfix*
    public static void Update(EditorRoads editorRoadsInstance)
    {
        #region HandlesExtension
        if (ExtensionManager.TryGetInstance(out HandlesExtension? handlesExtension))
        {
            if (!handlesExtension!.PreCustomUpdate()) return;
        }
        #endregion
        
        if ((InputEx.GetKeyDown(KeyCode.Delete) || InputEx.GetKeyDown(KeyCode.Backspace)) && EditorRoads.selection != null &&
            EditorRoads.road != null)
        {
            if (InputEx.GetKey(ControlsSettings.other))
            {
                LevelRoads.removeRoad(EditorRoads.road);
            }
            else
            {
                EditorRoads.road.removeVertex(EditorRoads.vertexIndex);
            }

            EditorRoads.deselect();
        }

        if (InputEx.GetKeyDown(ControlsSettings.tool_2) && EditorInteract.worldHit.transform != null)
        {
            Vector3 point = EditorInteract.worldHit.point;
            if (EditorRoads.road != null)
            {
                if (EditorRoads.tangentIndex > -1)
                {
                    EditorRoads.road.moveTangent(EditorRoads.vertexIndex, EditorRoads.tangentIndex, point - EditorRoads.joint.vertex);
                }
                else if (EditorRoads.vertexIndex > -1)
                {
                    EditorRoads.road.moveVertex(EditorRoads.vertexIndex, point);
                }
            }
        }

        bool selectingHandle = EditorRoads.selection;
        if (handlesExtension != null)
        {
            selectingHandle = selectingHandle && handlesExtension.GetHandles().Raycast(EditorInteract.ray) && handlesExtension.GetHandlePrioritizeValue();
        }
        if (!InputEx.GetKeyDown(ControlsSettings.primary) || selectingHandle)
        {
            return;
        }

        if (EditorInteract.logicHit.transform != null)
        {
            if (EditorInteract.logicHit.transform.name.IndexOf("Path", StringComparison.Ordinal) != -1 ||
                EditorInteract.logicHit.transform.name.IndexOf("Tangent", StringComparison.Ordinal) != -1)
            {
                EditorRoads.select(EditorInteract.logicHit.transform);
            }
        }
        else
        {
            if (!EditorInteract.worldHit.transform)
            {
                return;
            }

            Vector3 point2 = EditorInteract.worldHit.point;
            if (EditorRoads.road != null)
            {
                if (EditorRoads.tangentIndex > -1)
                {
                    EditorRoads.select(EditorRoads.road.addVertex(EditorRoads.vertexIndex + EditorRoads.tangentIndex, point2));
                    return;
                }

                float num = Vector3.Dot(point2 - EditorRoads.joint.vertex, EditorRoads.joint.getTangent(0));
                float num2 = Vector3.Dot(point2 - EditorRoads.joint.vertex, EditorRoads.joint.getTangent(1));
                if (num > num2)
                {
                    EditorRoads.select(EditorRoads.road.addVertex(EditorRoads.vertexIndex, point2));
                }
                else
                {
                    EditorRoads.select(EditorRoads.road.addVertex(EditorRoads.vertexIndex + 1, point2));
                }
            }
            else
            {
                EditorRoads.select(LevelRoads.addRoad(point2));
            }
        }

        handlesExtension?.PostCustomUpdate();
    }
}