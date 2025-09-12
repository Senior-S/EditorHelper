using System;
using EditorHelper2.Updates.Editor;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper2.Patches.Editor.UI;

[HarmonyPatch(typeof(EditorLevelVisibilityUI))]
public class EditorLevelVisibilityUIPatches
{
    [HarmonyPatch(nameof(EditorLevelVisibilityUI.update), new Type[] { })]
    [HarmonyPostfix]
    [UsedImplicitly]
    public static void PostfixUpdate()
    {
        EditorLevelVisibilityUIUpdate.Update();
    }
}