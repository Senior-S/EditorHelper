using EditorHelper2.Updates.Editor;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using Action = System.Action;

namespace EditorHelper2.Patches.Editor;

[HarmonyPatch(typeof(EditorNavigation))]
public class EditorNavigationPatches
{
    public static event Action? OnSelectionChanged;

    [HarmonyPatch(nameof(EditorNavigation.Update))]
    [HarmonyPrefix]
    [UsedImplicitly]
    static bool PrefixUpdate()
    {
        EditorNavigationUpdate.Update();

        return false;
    }

    [HarmonyPatch(nameof(EditorNavigation.select))]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void PostfixSelect()
    {
        OnSelectionChanged?.Invoke();
    }
}
