using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace EditorHelper.Commands;

public class CommandFly : Command
{
    protected override void execute(CSteamID executorID, string parameter)
    {
        Player player = PlayerTool.getPlayer(executorID);
        if (player == null) return;

        if (!Provider.hasCheats)
        {
            player.ServerShowHint(localization.format("CheatsErrorText"), 2);
            return;
        }

        // Toggle custom flight controller
        FlyController controller = player.gameObject.GetComponent<FlyController>();
        if (controller == null)
        {
            player.gameObject.AddComponent<FlyController>();
            player.ServerShowHint("Fly mode enabled.", 2);
        }
        else
        {
            Object.Destroy(controller);
            player.movement.sendPluginGravityMultiplier(1f);
            player.ServerShowHint("Fly mode disabled.", 2);
        }
    }

    public CommandFly()
    {
        _command = "fly";
        _info = "Toggle flight mode with vertical controls.";
        _help = "Fly using space to go up, ctrl/c to go down.";
    }
}