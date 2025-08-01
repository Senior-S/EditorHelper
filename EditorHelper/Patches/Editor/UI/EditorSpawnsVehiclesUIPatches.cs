using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper.Patches.Editor.UI;

[HarmonyPatch]
public class EditorSpawnsVehiclesUIPatches
{
    [HarmonyPatch(typeof(EditorSpawnsVehiclesUI), "onClickedTableButton")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void onClickedTableButton()
    {
        if (EditorHelper.Instance.VehicleSpawnsManager == null) return;
        
        EditorHelper.Instance.VehicleSpawnsManager.UpdateColor();
    }
    
    [HarmonyPatch(typeof(EditorSpawnsVehiclesUI), "onDraggedRotationSlider")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void onDraggedRotationSlider(ISleekSlider slider, float state)
    {
        if (EditorHelper.Instance.VehicleSpawnsManager == null) return;
        
        EditorHelper.Instance.VehicleSpawnsManager.UpdateRotation();
    }
    
    [HarmonyPatch(typeof(EditorSpawnsVehiclesUI), "onVehicleColorPicked")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void onVehicleColorPicked()
    {
        if (EditorHelper.Instance.VehicleSpawnsManager == null) return;
        
        EditorHelper.Instance.VehicleSpawnsManager.UpdateColor();
    }
}