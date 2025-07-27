using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace EditorHelper.Commands;

public class JumpCommand : Command
{
    protected override void execute(CSteamID executorID, string parameter)
    {
        Player player = PlayerTool.getPlayer(executorID);
        if (player == null)
        {
            CommandWindow.LogError("This command can't be called from console."); // Tbf this should never happen but just in case
            return;
        }
        
        // Mask from https://github.com/TH3AL3X/uEssentials/blob/dev/src/Api/Unturned/UPlayer.cs#L244C45-L244C84
        RaycastInfo? info = DamageTool.raycast(new Ray(player.look.aim.position, player.look.aim.forward), 1000f, RayMasks.BLOCK_COLLISION & ~(1 << 0x15));
        if (info == null) return;
        
        player.teleportToLocationUnsafe(info.point + new Vector3(0f, 1f, 0f), player.look.yaw);
    }

    public JumpCommand()
    {
        _command = "Jump";
        _help = "/jump";
        _info = "Jump to the position you're looking at";
        CommandWindow.Log($"{_command} command registered correctly!");
    }
}