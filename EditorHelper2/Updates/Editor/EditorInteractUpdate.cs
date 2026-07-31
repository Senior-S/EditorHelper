using EditorHelper2.common.Keybinds;
using EditorHelper2.Extensions.Level.Objects;
using EditorHelper2.Extensions.Level.Visibility;
using EditorHelper2.Loader;
using SDG.Framework.Devkit.Transactions;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Updates.Editor;

public static class EditorInteractUpdate
{
    public static void Update(EditorInteract __instance)
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
        if (ExtensionManager.TryGetInstance(out ExtrasExtension? extrasExtension))
        {
            raymasks = extrasExtension.ObjectsLayerMask;
        }

        Physics.Raycast(EditorInteract.ray, out EditorInteract._objectHit, 2048f, raymasks);
        Physics.Raycast(EditorInteract.ray, out EditorInteract._logicHit, 2048f, RayMasks.EDITOR_LOGIC);
        if (KeybindManager.IsDown(KeybindIds.EditorSave))
        {
            SDG.Unturned.Level.save();
        }

        if (KeybindManager.IsDown(KeybindIds.VisibilityRoads))
        {
            LevelVisibility.roadsVisible = !LevelVisibility.roadsVisible;
            EditorLevelVisibilityUI.roadsToggle.Value = LevelVisibility.roadsVisible;
        }

        if (KeybindManager.IsDown(KeybindIds.VisibilityNavigation))
        {
            LevelVisibility.navigationVisible = !LevelVisibility.navigationVisible;
            EditorLevelVisibilityUI.navigationToggle.Value = LevelVisibility.navigationVisible;
        }

        if (KeybindManager.IsDown(KeybindIds.VisibilityNodes))
        {
            LevelVisibility.nodesVisible = !LevelVisibility.nodesVisible;
            EditorLevelVisibilityUI.nodesToggle.Value = LevelVisibility.nodesVisible;
        }

        if (KeybindManager.IsDown(KeybindIds.VisibilityItems))
        {
            LevelVisibility.itemsVisible = !LevelVisibility.itemsVisible;
            EditorLevelVisibilityUI.itemsToggle.Value = LevelVisibility.itemsVisible;
        }

        if (KeybindManager.IsDown(KeybindIds.VisibilityPlayers))
        {
            LevelVisibility.playersVisible = !LevelVisibility.playersVisible;
            EditorLevelVisibilityUI.playersToggle.Value = LevelVisibility.playersVisible;
        }

        if (KeybindManager.IsDown(KeybindIds.VisibilityZombies))
        {
            LevelVisibility.zombiesVisible = !LevelVisibility.zombiesVisible;
            EditorLevelVisibilityUI.zombiesToggle.Value = LevelVisibility.zombiesVisible;
        }

        if (KeybindManager.IsDown(KeybindIds.VisibilityVehicles))
        {
            LevelVisibility.vehiclesVisible = !LevelVisibility.vehiclesVisible;
            EditorLevelVisibilityUI.vehiclesToggle.Value = LevelVisibility.vehiclesVisible;
        }

        if (KeybindManager.IsDown(KeybindIds.VisibilityBorder))
        {
            LevelVisibility.borderVisible = !LevelVisibility.borderVisible;
            EditorLevelVisibilityUI.borderToggle.Value = LevelVisibility.borderVisible;
        }

        if (KeybindManager.IsDown(KeybindIds.VisibilityAnimals))
        {
            LevelVisibility.animalsVisible = !LevelVisibility.animalsVisible;
            EditorLevelVisibilityUI.animalsToggle.Value = LevelVisibility.animalsVisible;
        }

        #region ObjectNavmeshVisualizationExtension
        if (ExtensionManager.TryGetInstance(out ObjectNavmeshVisualizationExtension? objectNavmeshVisualizationExtension))
        {
            objectNavmeshVisualizationExtension.CustomUpdate();
        }
        #endregion

        if (__instance.activeTool == null)
        {
            return;
        }

        __instance.activeTool.update();
        if (KeybindManager.IsDown(KeybindIds.EditorRedo))
        {
            DevkitTransactionManager.redo();
        }
        else if (KeybindManager.IsDown(KeybindIds.EditorUndo))
        {
            DevkitTransactionManager.undo();
        }
    }
}
