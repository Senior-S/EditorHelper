using System;
using SDG.Unturned;
using Steamworks;

namespace EditorHelper.Commands;

public class CommandAmmo : Command
{
    protected override void execute(CSteamID executorID, string parameter)
    {
        Player player = PlayerTool.getPlayer(executorID);
        if (!Provider.hasCheats)
        {
            player.ServerShowHint(localization.format("CheatsErrorText"), 2);
            return;
        }
        
        if (player == null) return;
        
        ItemGunAsset? gun = player.equipment.asset as ItemGunAsset;
        if (gun == null) 
        {
            player.ServerShowHint("You must equip a gun to use this command.", 2); 
            return; 
        }

        int amount = 1;
        if (!string.IsNullOrEmpty(parameter))
            amount = Math.Max(1, int.Parse(parameter));

        ushort magazineID = gun.GetDefaultMagazineLegacyId();
        Item magazine = new Item(magazineID, true);
        for (int i = 0; i < amount; i++)
            player.inventory.forceAddItem(magazine, true);

        player.ServerShowHint($"+{amount} {magazine.GetAsset().FriendlyName}.", 2);
    }

    public CommandAmmo()
    {
        _command = "ammo";
        _info = "Ammo [Amount]";
        _help = "This gives magazines for the equipped gun.";
        CommandWindow.Log($"{_command} command registered correctly!");
    }
}