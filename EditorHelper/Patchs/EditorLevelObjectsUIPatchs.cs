using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Patchs;

[HarmonyPatch]
public class EditorLevelObjectsUIPatchs
{
    [HarmonyPatch(typeof(EditorLevelObjectsUI), MethodType.Constructor)]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void Constructor(EditorLevelObjectsUI __instance)
    {
        Bundle bundle = Bundles.getBundle("/Bundles/Textures/Edit/Icons/EditorLevelObjects/EditorLevelObjects.unity3d");
        
        SleekButtonIcon highlightButton = new(null)
        {
            PositionOffset_X = 210f,
            PositionOffset_Y = -30f,
            PositionScale_Y = 1f,
            SizeOffset_X = 200f,
            SizeOffset_Y = 30f,
            text = "Highlight objects",
            tooltip = "Highlight all objects of the selected type"
        };
        highlightButton.onClickedButton += ProjectMain.HighlightObjects;
        __instance.AddChild(highlightButton);
        ProjectMain.HighlightButton = highlightButton;

    }
}