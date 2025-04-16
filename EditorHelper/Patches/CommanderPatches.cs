using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper.Patches;

[HarmonyPatch]
public class CommanderPatches
{
    [HarmonyPatch(typeof(Commander), "init")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void init()
    {
        EditorHelper.RegisterCommands();
    }
}