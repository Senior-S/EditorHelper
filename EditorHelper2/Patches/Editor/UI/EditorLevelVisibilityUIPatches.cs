using System;
using EditorHelper2.Updates.Editor;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper2.Patches.Editor.UI;

[HarmonyPatch(typeof(EditorLevelVisibilityUI))]
public class EditorLevelVisibilityUIPatches
{
    [HarmonyPatch(nameof(EditorLevelVisibilityUI.update), [])]
    [HarmonyPostfix]
    [UsedImplicitly]
    public static void PostfixUpdate()
    {
        EditorLevelVisibilityUIUpdate.Update();
    }

    [HarmonyPatch(nameof(EditorLevelVisibilityUI.update), [typeof(int), typeof(int)])]
    [HarmonyPostfix]
    [UsedImplicitly]
    public static void PostfixUpdateRegion(int x, int y)
    {
        EditorLevelVisibilityUIUpdate.UpdateRegion(x, y);
    }
}