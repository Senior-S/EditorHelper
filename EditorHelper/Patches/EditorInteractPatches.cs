using HarmonyLib;
using JetBrains.Annotations;
using SDG.Framework.Devkit.Transactions;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Patches;

[HarmonyPatch]
public class EditorInteractPatches
{
    [HarmonyPatch(typeof(EditorInteract), "Update")]
    [HarmonyPrefix]
    [UsedImplicitly]
    private static bool Update(EditorInteract __instance)
    {
        if (Glazier.Get().ShouldGameProcessInput)
        {
            EditorInteract._isFlying = InputEx.GetKey(ControlsSettings.secondary);
        }
        else
        {
            EditorInteract._isFlying = false;
        }

        EditorInteract._ray = MainCamera.instance.ScreenPointToRay(Input.mousePosition);
        Physics.Raycast(EditorInteract.ray, out EditorInteract._worldHit, 2048f, RayMasks.EDITOR_WORLD);
        int raymasks = RayMasks.EDITOR_INTERACT;
        if (EditorHelper.Instance.ObjectsManager != null)
        {
            raymasks = EditorHelper.Instance.ObjectsManager.ObjectsLayerMask;
        }
        Physics.Raycast(EditorInteract.ray, out EditorInteract._objectHit, 2048f, raymasks);
        Physics.Raycast(EditorInteract.ray, out EditorInteract._logicHit, 2048f, RayMasks.EDITOR_LOGIC);
        if (InputEx.GetKeyDown(KeyCode.S) && InputEx.GetKey(KeyCode.LeftControl))
        {
            Level.save();
        }

        if (InputEx.GetKeyDown(KeyCode.F1))
        {
            LevelVisibility.roadsVisible = !LevelVisibility.roadsVisible;
            EditorLevelVisibilityUI.roadsToggle.Value = LevelVisibility.roadsVisible;
        }

        if (InputEx.GetKeyDown(KeyCode.F2))
        {
            LevelVisibility.navigationVisible = !LevelVisibility.navigationVisible;
            EditorLevelVisibilityUI.navigationToggle.Value = LevelVisibility.navigationVisible;
        }

        if (InputEx.GetKeyDown(KeyCode.F3))
        {
            LevelVisibility.nodesVisible = !LevelVisibility.nodesVisible;
            EditorLevelVisibilityUI.nodesToggle.Value = LevelVisibility.nodesVisible;
        }

        if (InputEx.GetKeyDown(KeyCode.F4))
        {
            LevelVisibility.itemsVisible = !LevelVisibility.itemsVisible;
            EditorLevelVisibilityUI.itemsToggle.Value = LevelVisibility.itemsVisible;
        }

        if (InputEx.GetKeyDown(KeyCode.F5))
        {
            LevelVisibility.playersVisible = !LevelVisibility.playersVisible;
            EditorLevelVisibilityUI.playersToggle.Value = LevelVisibility.playersVisible;
        }

        if (InputEx.GetKeyDown(KeyCode.F6))
        {
            LevelVisibility.zombiesVisible = !LevelVisibility.zombiesVisible;
            EditorLevelVisibilityUI.zombiesToggle.Value = LevelVisibility.zombiesVisible;
        }

        if (InputEx.GetKeyDown(KeyCode.F7))
        {
            LevelVisibility.vehiclesVisible = !LevelVisibility.vehiclesVisible;
            EditorLevelVisibilityUI.vehiclesToggle.Value = LevelVisibility.vehiclesVisible;
        }

        if (InputEx.GetKeyDown(KeyCode.F8))
        {
            LevelVisibility.borderVisible = !LevelVisibility.borderVisible;
            EditorLevelVisibilityUI.borderToggle.Value = LevelVisibility.borderVisible;
        }

        if (InputEx.GetKeyDown(KeyCode.F9))
        {
            LevelVisibility.animalsVisible = !LevelVisibility.animalsVisible;
            EditorLevelVisibilityUI.animalsToggle.Value = LevelVisibility.animalsVisible;
        }

        if (__instance.activeTool == null)
        {
            return false;
        }

        __instance.activeTool.update();
        if (InputEx.GetKeyDown(KeyCode.Z) && InputEx.GetKey(KeyCode.LeftControl))
        {
            if (InputEx.GetKey(KeyCode.LeftShift))
            {
                DevkitTransactionManager.redo();
            }
            else
            {
                DevkitTransactionManager.undo();
            }
        }

        return false;
    }
}