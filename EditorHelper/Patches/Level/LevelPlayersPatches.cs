using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper.Patches.Level;

[HarmonyPatch]
public class LevelPlayersPatches
{
    [HarmonyPatch(typeof(LevelPlayers), "save")]
    [UsedImplicitly]
    private static bool save()
    {
        if (LevelPlayers.spawns.Count > byte.MaxValue)
        {
            EditorHelper.Instance.EditorManager.DisplayAlert($"You have exceeded the max amount of player spawns ({byte.MaxValue}). Player spawns won't be saved.");
            return false;
        }
        
        return true;
    }
}