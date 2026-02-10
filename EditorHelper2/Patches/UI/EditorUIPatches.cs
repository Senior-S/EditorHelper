using EditorHelper2.common.Keybinds;
using EditorHelper2.Extensions.Editor.Pause;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Patches.UI;

[HarmonyPatch(typeof(EditorUI))]
public class EditorUIPatches
{
    [HarmonyPatch("Update")]
    [HarmonyPrefix]
    [UsedImplicitly]
    private static void PrefixUpdate()
    {
        if (!KeybindsMenuExtension.IsOpen) return;
        if (InputEx.ConsumeKeyDown(KeyCode.Escape))
        {
            KeybindsMenuExtension.CloseIfOpen();
        }
    }

    [HarmonyPatch("OnGUI")]
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void PostfixOnGUI()
    {
        KeybindRebindManager.HandleOnGUI();
    }

    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void PostfixUpdate()
    {
        KeybindRebindManager.HandleUpdate();
    }
}
