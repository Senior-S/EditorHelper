using SDG.Unturned;
using Steamworks;

namespace EditorHelper.Commands;

public class CommandResetSkills : Command
{
    protected override void execute(CSteamID executorID, string parameter)
    {
        Player player = PlayerTool.getPlayer(executorID);
        if (player == null)
        {
            CommandWindow.LogError("This command can't be called from console."); // Tbf this should never happen but just in case
            return;
        }

        foreach (Skill[]? skills in player.skills.skills)
        {
            foreach (Skill skill in skills)
            {
                skill.level = 0;
            }
        }
        
        player.ServerShowHint("Your skills have been reset.", 2);
    }

    public CommandResetSkills()
    {
        _command = "ResetSkills";
        _help = "/resetskills";
        _info = "Resets your skills";
        CommandWindow.Log($"{_command} command registered correctly!");
    }
}