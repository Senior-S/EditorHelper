using EditorHelper2.Updates.Editor;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper2.Patches.Editor.UI;

[HarmonyPatch(typeof(EditorTerrainDetailsUI))]
public class EditorTerrainDetailsUIPatches
{
    [HarmonyPatch(nameof(EditorTerrainDetailsUI.OnUpdate))]
    [HarmonyPostfix]
    [UsedImplicitly]
    public static void PostfixUpdate()
    {
        UnturnedLog.info("UPDATE PATCH POSTFIX CALLED");
        EditorTerrainDetailsUIUpdate.Update();
    }
}