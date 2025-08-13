using System;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Patches.Menu.UI;

[HarmonyPatch]
public class MenuWorkshopSubmitUIPatches
{
    private static Local? _localization;

    [HarmonyPatch(typeof(MenuWorkshopSubmitUI), MethodType.Constructor)]
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void Constructor(MenuWorkshopSubmitUI __instance)
    {
        _localization = Localization.read("/Menu/Workshop/MenuWorkshopSubmit.dat");
        SleekButtonState typeState = MenuWorkshopSubmitUI.typeState;

        SleekButtonState newTypeState = new SleekButtonState(
            new GUIContent(_localization.format("Map")),
            new GUIContent(_localization.format("Localization")),
            new GUIContent(_localization.format("Object")),
            new GUIContent(_localization.format("Item")),
            new GUIContent(_localization.format("Vehicle")),
            new GUIContent(_localization.format("Skin")),
            new GUIContent(_localization.format("ServerCuration")),
            new GUIContent("Barn")
        )
        {
            PositionOffset_X = typeState.PositionOffset_X,
            PositionOffset_Y = typeState.PositionOffset_Y,
            PositionScale_X = typeState.PositionScale_X,
            SizeOffset_X = typeState.SizeOffset_X,
            SizeOffset_Y = typeState.SizeOffset_Y,
            onSwappedState = typeState.onSwappedState
        };

        ISleekElement? parent = typeState.Parent;
        if (parent != null)
        {
            parent.RemoveChild(typeState);
            parent.AddChild(newTypeState);
        }

        MenuWorkshopSubmitUI.typeState = newTypeState;
    }
    
    [HarmonyPatch(typeof(MenuWorkshopSubmitUI), "GetSubmissionTags")]
    [HarmonyPrefix]
    [UsedImplicitly]
    private static bool GetSubmissionTags(ref List<string> __result)
    {
        SleekButtonState typeState = MenuWorkshopSubmitUI.typeState;

        List<string> list = new List<string>();
        switch (typeState.state)
        {
            case 0:
                list.Add("Map");
                break;
            case 1:
                list.Add("Localization");
                break;
            case 2:
                list.Add("Object");
                break;
            case 3:
                list.Add("Item");
                break;
            case 4:
                list.Add("Vehicle");
                break;
            case 5:
                list.Add("Skin");
                break;
            case 6:
                list.Add("Server Curation");
                break;
            case 7:
                list.Add("Barn");
                break;
        }

        string? extraTag = MenuWorkshopSubmitUI.ExtraTag;
        if (!string.IsNullOrEmpty(extraTag))
        {
            list.Add(extraTag);
        }

        __result = list;
        return false;
    }
}