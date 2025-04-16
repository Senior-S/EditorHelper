using SDG.Unturned;
using Steamworks;

namespace EditorHelper.Commands;

public class MaxSkillsCommand : Command
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
    }

    public MaxSkillsCommand()
    {
        _command = "MaxSkills";
        _help = "/maxskills";
        _info = "Set your skills to max level";
        CommandWindow.Log($"{_command} command registered correctly!");
    }
}