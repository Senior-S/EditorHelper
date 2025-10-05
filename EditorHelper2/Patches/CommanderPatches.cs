using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper2.Patches;

[HarmonyPatch(typeof(Commander))]
public class CommanderPatches
{
    [HarmonyPatch(nameof(Commander.init))]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void init()
    {
        EditorHelper.RegisterCustomCommands();
    }
}