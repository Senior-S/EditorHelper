using EditorHelper2.common.Helpers;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using Action = System.Action;

namespace EditorHelper2.Patches.UI;

[HarmonyPatch(typeof(MenuUI))]
public class MenuUIPatches
{
    /// <summary>
    /// Return true to consume the escape key before <see cref="MenuUI.escapeMenu"/> runs.
    /// </summary>
    public static event System.Func<bool>? OnEscapePressedBefore;

    /// <summary>
    /// Event invoked after <see cref="MenuUI.escapeMenu"/> is called.
    /// Useful to properly react to escape key in UIs
    /// </summary>
    public static event Action? OnEscapePressed;

    [HarmonyPatch(nameof(MenuUI.escapeMenu))]
    [HarmonyPrefix]
    [UsedImplicitly]
    static bool PrefixEscapeMenu()
    {
        if (OnEscapePressedBefore == null)
        {
            return true;
        }

        foreach (System.Delegate del in OnEscapePressedBefore.GetInvocationList())
        {
            if (del is System.Func<bool> handler && handler())
            {
                return false;
            }
        }

        return true;
    }
    
    [HarmonyPatch(nameof(MenuUI.escapeMenu))]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void PostfixEscapeMenu(EditorObjects __instance)
    {
        OnEscapePressed?.Invoke();
    }
    
    [HarmonyPatch(nameof(MenuUI.tickInput))]
    [HarmonyPrefix]
    [UsedImplicitly]
    static bool PrefixTickInput(MenuUI __instance)
    {
        return UpdaterCore.GetVersionStatus() != EVersionStatus.Outdated;
    }
}