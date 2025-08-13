using System;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Patches.Menu.UI
{
    [HarmonyPatch(typeof(MenuWorkshopSubmitUI))]
    public class MenuWorkshopSubmitUIPatches
    {
        private static Local? _localization;

        [HarmonyPatch(MethodType.Constructor)]
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void ConstructorPostfix(MenuWorkshopSubmitUI instance)
        {
            _localization = Localization.read("/Menu/Workshop/MenuWorkshopSubmit.dat");

            SleekButtonState? typeState = Traverse.Create(instance).Field("typeState").GetValue<SleekButtonState>();
            if (typeState == null)
            {
                UnturnedLog.error("Failed to access typeState field in MenuWorkshopSubmitUI");
                return;
            }

            var newTypeState = new SleekButtonState(
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
            Traverse.Create(instance).Field("typeState").SetValue(newTypeState);
        }

        [HarmonyPatch("GetSubmissionTags")]
        [HarmonyPrefix]
        [UsedImplicitly]
        private static bool GetSubmissionTags(ref List<string> result, MenuWorkshopSubmitUI instance)
        {
            SleekButtonState? typeState = MenuWorkshopSubmitUI.typeState;

            var states = typeState.states;
            UnturnedLog.info($"typeState options: {string.Join(", ", Array.ConvertAll(states, s => s.text))}");
            UnturnedLog.info($"Current typeState.state: {typeState.state}");

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

            result = list;
            return false;
        }
    }
}