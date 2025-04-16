using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace EditorHelper.Commands;

public class ExpCommand : Command
{
    protected override void execute(CSteamID executorID, string parameter)
    {
        Player player = PlayerTool.getPlayer(executorID);
        if (player == null)
        {
            CommandWindow.LogError("This command can't be called from console."); // Tbf this should never happen but just in case
            return;
        }

        if (!int.TryParse(parameter, out int exp))
        {
            ChatManager.say(player.channel.owner.playerID.steamID, "Error! Usage: " +_info, Color.red, true);
            return;
        }
        
        player.skills.ServerModifyExperience(exp);
    }

    public ExpCommand()
    {
        _command = "Exp";
        _help = "/exp <amount>";
        _info = "Give experience to the player";
        CommandWindow.Log($"{_command} command registered correctly!");
    }
}