using SDG.Unturned;
using Steamworks;

namespace EditorHelper.Commands;

public class CommandMaxSkills : Command
{
    protected override void execute(CSteamID executorID, string parameter)
    {
        Player player = PlayerTool.getPlayer(executorID);
        if (player == null)
        {
            CommandWindow.LogError("This command can't be called from console."); // Tbf this should never happen but just in case
            return;
        }
        
        player.skills.ServerUnlockAllSkills();
        player.ServerShowHint("Your skills have set to max.", 2);
    }

    public CommandMaxSkills()
    {
        _command = "MaxSkills";
        _help = "/maxskills";
        _info = "Set your skills to max level";
        CommandWindow.Log($"{_command} command registered correctly!");
    }
}