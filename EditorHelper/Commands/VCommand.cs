using System;
using System.Collections.Generic;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace EditorHelper.Commands;

public class VCommand : Command
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
            Asset asset = Assets.find(guid);
            if (asset is not null)
            {
                InteractableVehicle interactableVehicle = VehicleTool.SpawnVehicleForPlayer(player, asset);
                if (interactableVehicle)
                {
                    ChatManager.say(player.channel.owner.playerID.steamID, $"Successfully spawned a {asset.FriendlyName}", Color.green, true);
                }
            }

            return;
        }

        if (ushort.TryParse(arguments[0], out ushort id))
        {
            Asset asset = Assets.find(EAssetType.VEHICLE, id);
            if (asset is not null)
            {
                InteractableVehicle interactableVehicle = VehicleTool.SpawnVehicleForPlayer(player, asset);
                if (interactableVehicle)
                {
                    ChatManager.say(player.channel.owner.playerID.steamID, $"Successfully spawned a {asset.FriendlyName}", Color.green, true);
                }
            }

            return;
        }

        Asset stringAsset = FindByString(string.Join(" ", arguments));
        if (stringAsset is not null)
        {
            InteractableVehicle interactableVehicle = VehicleTool.SpawnVehicleForPlayer(player, stringAsset);
            if (interactableVehicle)
            {
                ChatManager.say(player.channel.owner.playerID.steamID, $"Successfully spawned a {stringAsset.FriendlyName}", Color.green, true);
            }

            return;
        }

        ChatManager.say(player.channel.owner.playerID.steamID, "No vehicle found for the provided input", Color.red, true);
    }

    private Asset FindByString(string input)
    {
        input = input.Trim().ToLower();
        if (string.IsNullOrEmpty(input))
        {
            return null;
        }
        List<VehicleAsset> list = [];
        Assets.find(list);
        foreach (VehicleAsset item in list)
        {
            if (string.Equals(input, item.name, StringComparison.InvariantCultureIgnoreCase))
            {
                return item;
            }
        }
        foreach (VehicleAsset item2 in list)
        {
            if (string.Equals(input, item2.vehicleName, StringComparison.InvariantCultureIgnoreCase))
            {
                return item2;
            }
        }
        foreach (VehicleAsset item3 in list)
        {
            if (item3.name.ToLower().Contains(input))
            {
                return item3;
            }
        }
        foreach (VehicleAsset item4 in list)
        {
            if (item4.FriendlyName.ToLower().Contains(input))
            {
                return item4;
            }
        }
        return null;
    }

    public VCommand()
    {
        _command = "V";
        _help = "/v <id/guid/name>";
        _info = "Spawns a vehicle";
        CommandWindow.Log($"{_command} command registered correctly!");
    }
}