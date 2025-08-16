using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using Action = System.Action;

namespace EditorHelper2.Patches.UI;

[HarmonyPatch(typeof(MenuUI))]
public class MenuUIPatches
{
    /// <summary>
    /// Event invoked after <see cref="MenuUI.escapeMenu"/> is called.
    /// Useful to properly react to escape key in UIs
    /// </summary>
    public static event Action OnEscapePressed;
    
    [HarmonyPatch(nameof(MenuUI.escapeMenu))]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void PostfixEscapeMenu(EditorObjects __instance)
    {
        OnEscapePressed?.Invoke();
    }
}