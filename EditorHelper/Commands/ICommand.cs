using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace EditorHelper.Commands;

public class ICommand : Command
{
    protected override void execute(CSteamID executorID, string parameter)
    {
        Player player = PlayerTool.getPlayer(executorID);
        if (player == null)
        {
            CommandWindow.LogError("This command can't be called from console."); // Tbf this should never happen but just in case
            return;
        }
        
        string[] arguments = parameter.Split(' ');

        if (Guid.TryParse(arguments[0], out Guid guid))
        {
            if (Assets.find(guid) is ItemAsset itemAsset)
            {
                GiveItem(itemAsset);
            }

            return;
        }

        if (ushort.TryParse(arguments[0], out ushort id))
        {
            if (Assets.find(EAssetType.ITEM, id) is ItemAsset itemAsset)
            {
                GiveItem(itemAsset);
            }

            return;
        }

        int amount = 0;
        if (Regex.IsMatch(arguments.Last(), @"^\d+$"))
        {
            amount = int.Parse(arguments.Last());
        }
        arguments = arguments.Take(arguments.Length - 1).ToArray();
        if (FindByString(string.Join(" ", arguments)) is ItemAsset asset)
        {
            GiveItem(asset);
            return;
        }
        
        ChatManager.say(player.channel.owner.playerID.steamID, "No item found for the provided input", Color.red, true);

        return;
        
        void GiveItem(ItemAsset itemAsset)
        {
            int amount = 1;
            if (arguments.Length > 1 && int.TryParse(arguments.Last(), out int value))
            {
                amount = value;
            }

            for (int i = 0; i < amount; i++)
            {
                player.inventory.forceAddItem(new Item(itemAsset, EItemOrigin.ADMIN), true);
            }
        }
    }

    private Asset FindByString(string input)
    {
        input = input.Trim().ToLower();
        if (string.IsNullOrEmpty(input))
        {
            return null;
        }
        List<ItemAsset> list = [];
        Assets.find(list);
        foreach (ItemAsset item in list)
        {
            if (string.Equals(input, item.name, StringComparison.InvariantCultureIgnoreCase))
            {
                return item;
            }
        }
        foreach (ItemAsset item2 in list)
        {
            if (string.Equals(input, item2.itemName, StringComparison.InvariantCultureIgnoreCase))
            {
                return item2;
            }
        }
        foreach (ItemAsset item3 in list)
        {
            if (item3.name.ToLower().Contains(input))
            {
                return item3;
            }
        }
        foreach (ItemAsset item4 in list)
        {
            if (item4.itemName.ToLower().Contains(input))
            {
                return item4;
            }
        }
        return null;
    }

    public ICommand()
    {
        _command = "I";
        _help = "/i <id/guid/name> {amount}";
        _info = "Spawns a item";
        CommandWindow.Log($"{_command} command registered correctly!");
    }
}