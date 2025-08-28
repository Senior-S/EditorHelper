using System.Runtime.CompilerServices;
using EditorHelper.Builders;
using EditorHelper.Models;
using HarmonyLib;
using SDG.Unturned;

namespace EditorHelper.Editor.Managers;

public class MaterialAssetManager
{
    private EditorTerrainMaterialsUI _currentUIInstance;
    
    private readonly ISleekBox box;
    private readonly ISleekToggle _useAutoHeightToggle;
    private readonly ISleekFloat32Field _autoMinHeightField;
    private readonly ISleekFloat32Field _autoMaxHeightField;
    public MaterialAssetManager()
    {
        UIBuilder builder = new();
        builder
            .SetPositionScaleX(0f)
            .SetPositionScaleY(0f);
        box = builder.CreateSimpleBox();
        
        _useAutoHeightToggle = Glazier.Get().CreateToggle();
        _useAutoHeightToggle.SizeOffset_X = 30f;
        _useAutoHeightToggle.SizeOffset_Y = 30f;
        _useAutoHeightToggle.AddLabel("Use Auto Height", ESleekSide.RIGHT);
        
        _useAutoHeightToggle.OnValueChanged += (t, value) =>
        {
            //OnToggleElement(item, value);
        };

        box.AddChild(_useAutoHeightToggle);
    }
    
    public void Initialize(ref EditorTerrainMaterialsUI uiInstance)
    {
        _currentUIInstance = uiInstance;
        uiInstance.AddChild(box);
    }
    
    public void CustomUpdate(EditorTerrainMaterialsUI uiInstance)
    {
        if (uiInstance.selectedMaterialAsset != null)
        {
            if (LandscapeMaterialAsset_Patch.Extensions.TryGetValue(uiInstance.selectedMaterialAsset, out var ext))
            {
                UnturnedLog.info("[Extension] Use_Auto_Height" + ext.useAutoHeight);
                UnturnedLog.info("[Extension] Auto_Min_Height" + ext.minHeight);
                UnturnedLog.info("[Extension] Auto_Max_Height" + ext.maxHeight);
            }
        }
    }
}

[HarmonyPatch(typeof(LandscapeMaterialAsset))]
public static class LandscapeMaterialAsset_Patch
{
    public static readonly ConditionalWeakTable<LandscapeMaterialAsset, LandscapeMaterialAssetExtension> Extensions = new();

    [HarmonyPostfix]
    [HarmonyPatch(nameof(LandscapeMaterialAsset.PopulateAsset))]
    public static void Postfix(LandscapeMaterialAsset __instance, in PopulateAssetParameters p)
    {
        // Read custom values from the asset file
        bool useAutoHeight = p.data.ParseBool("Use_Auto_Height");
        float minHeight = p.data.ParseFloat("Auto_Min_Height");
        float maxHeight = p.data.ParseFloat("Auto_Max_Height");

        // Store in ConditionalWeakTable
        var ext = new LandscapeMaterialAssetExtension(useAutoHeight, minHeight, maxHeight);
        Extensions.Add(__instance, ext);
    }
}