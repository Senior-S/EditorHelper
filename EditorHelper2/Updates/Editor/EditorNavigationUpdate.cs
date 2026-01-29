using EditorHelper2.Extensions.Environment.Navigation;
using EditorHelper2.Loader;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Updates.Editor;

public static class EditorNavigationUpdate
{
    public static void Update()
    {
        #region NavHandlesExtension CustomUpdate
        if (ExtensionManager.TryGetInstance(out NavHandlesExtension? navHandlesExtension))
        {
            if (!navHandlesExtension.CustomUpdate()) return;
        }
        #endregion

        if (!EditorNavigation.isPathfinding || EditorInteract.isFlying || !Glazier.Get().ShouldGameProcessInput) return;

        if (EditorInteract.worldHit.transform != null)
        {
            EditorNavigation.marker.position = EditorInteract.worldHit.point;
        }

        if ((InputEx.GetKeyDown(KeyCode.Delete) || InputEx.GetKeyDown(KeyCode.Backspace)) && EditorNavigation.selection != null)
        {
            Transform selection = EditorNavigation.selection;
            EditorNavigation.select(null);
            LevelNavigation.removeFlag(selection);
        }

        if (InputEx.GetKeyDown(ControlsSettings.tool_2) && EditorInteract.worldHit.transform != null && EditorNavigation.selection != null)
        {
            Vector3 point = EditorInteract.worldHit.point;
            EditorNavigation.flag.move(point);

            #region NavHandlesExtension HandleOffsets Patch
            navHandlesExtension?.CalculateHandleOffsets();
            #endregion
        }

        if (!InputEx.GetKeyDown(ControlsSettings.primary)) return;

        if (EditorInteract.logicHit.transform != null)
        {
            if (EditorInteract.logicHit.transform.name == "Flag")
            {
                EditorNavigation.select(EditorInteract.logicHit.transform);
            }
        }
        else if (EditorInteract.worldHit.transform != null)
        {
            EditorNavigation.select(LevelNavigation.addFlag(EditorInteract.worldHit.point));
        }
    }
}
