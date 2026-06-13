using EditorHelper2.common.Keybinds;
using EditorHelper2.Extensions.Editor.Dashboard;
using EditorHelper2.Extensions.Editor.Pause;
using EditorHelper2.Loader;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using System.Collections.Generic;
using System.Reflection;
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
        if (CinematicModeExtension.IsOpen)
        {
            if (InputEx.ConsumeKeyDown(KeyCode.Escape))
            {
                CinematicModeExtension.CloseIfOpen();
            }

            return;
        }

        if (!KeybindsMenuExtension.IsOpen) return;
        if (InputEx.ConsumeKeyDown(KeyCode.Escape))
        {
            KeybindsMenuExtension.CloseIfOpen();
        }
    }

    private static readonly FieldInfo EditorTerrainUIActiveField = typeof(EditorTerrainUI).GetField(nameof(EditorTerrainUI.active), BindingFlags.Public | BindingFlags.Static);

    [HarmonyPatch("Update")]
    [HarmonyTranspiler]
    [UsedImplicitly]
    private static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        if (EditorTerrainUIActiveField is null)
        {
            CommandWindow.LogError("[EditorHelper2] Unable to Transpile, EditorTerrainUIActiveField is null!");
            return instructions;
        }

        var codematcher = new CodeMatcher(instructions).MatchStartForward(
                CodeMatch.LoadsField(EditorTerrainUIActiveField)
            );

        CodeInstruction label = codematcher.Instruction;
        CodeInstruction endBranch = codematcher.InstructionAt(1).Clone();

        return codematcher.Advance(-1).InsertAfter(
            CodeInstruction.Call(() => OnVanillaEditorTabHotkey()).MoveLabelsFrom(label),
            endBranch
        ).InstructionEnumeration();
    }

    private static bool OnVanillaEditorTabHotkey()
    {
        if (ExtensionManager.TryGetInstance(out DashboardHotkeysExtension? dashboardHotkeysExtension))
        {
            dashboardHotkeysExtension.CustomUpdate();
            return false;
        }

        return true;
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

[HarmonyPatch(typeof(EditorTerrainUI), "open")]
public class EditorTerrainUIPatches
{
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void PostfixOpen()
    {
        CinematicModeExtension.CloseForEditorTabSwitch();
    }
}

[HarmonyPatch(typeof(EditorEnvironmentUI), "open")]
public class EditorEnvironmentUIPatches
{
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void PostfixOpen()
    {
        CinematicModeExtension.CloseForEditorTabSwitch();
    }
}

[HarmonyPatch(typeof(EditorSpawnsUI), "open")]
public class EditorSpawnsUIPatches
{
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void PostfixOpen()
    {
        CinematicModeExtension.CloseForEditorTabSwitch();
    }
}

[HarmonyPatch(typeof(EditorLevelUI), "open")]
public class EditorLevelUIPatches
{
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void PostfixOpen()
    {
        CinematicModeExtension.CloseForEditorTabSwitch();
    }
}
