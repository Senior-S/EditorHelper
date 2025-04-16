using SDG.Unturned;
using Steamworks;

namespace EditorHelper.Commands;

public class HealCommand : Command
{
    protected override void execute(CSteamID executorID, string parameter)
    {
        Player player = PlayerTool.getPlayer(executorID);
        if (player == null)
        {
            CommandWindow.LogError("This command can't be called from console."); // Tbf this should never happen but just in case
            return;
        }
        
        player.life.serverModifyHealth(100f);
        player.life.serverModifyFood(100f);
        player.life.serverModifyWater(100f);
        player.life.serverModifyVirus(100f);
        player.life.serverModifyStamina(100f);
        player.life.serverSetBleeding(false);
        player.life.serverSetBleeding(false);
    }

    public HealCommand()
    {
        _command = "Heal";
        _help = "/heal";
        _info = "Heals the player";
        CommandWindow.Log($"{_command} command registered correctly!");
    }
}