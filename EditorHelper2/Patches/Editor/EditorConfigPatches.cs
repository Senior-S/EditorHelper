using EditorHelper2.Helpers;
using HarmonyLib;
using JetBrains.Annotations;

namespace EditorHelper2.Patches.Editor;

[HarmonyPatch(typeof(SDG.Unturned.Level), nameof(SDG.Unturned.Level.save))]
internal static class EditorConfigPatches
{
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void SaveMapEditorConfig()
    {
        MapEditorConfigHelper.Save();
    }
}
