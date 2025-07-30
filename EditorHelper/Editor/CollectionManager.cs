using System.Collections.Generic;
using EditorHelper.Builders;
using SDG.Framework.Foliage;
using SDG.Unturned;

namespace EditorHelper.Editor;

public class CollectionManager
{
    private readonly SleekList<ISleekButton> _assetScrollView;

    public CollectionManager()
    {
        UIBuilder builder = new(40f, 40f);

        builder.SetPositionOffsetX(0f)
            .SetPositionScaleY(1f)
            .SetPositionOffsetX(10f)
            .SetPositionOffsetY(-100f)
            .SetSizeOffsetX(50f)
            .SetSizeOffsetY(-10f);

        _assetScrollView = builder.BuildScrollBox<ISleekButton>(20, 5);
    }

    public void Initialize(ref EditorTerrainDetailsUI uiInstance)
    {
        uiInstance.AddChild(_assetScrollView);
        
        List<FoliageInfoAsset> foliageAssets = new();
        Assets.find(foliageAssets);

        foreach (FoliageInfoAsset asset in foliageAssets)
        {
            ISleekButton button = Glazier.Get().CreateButton();
            button.Text = asset.name;
            _assetScrollView.AddChild(button);
        }
    }

    public void CustomUpdate(EditorTerrainDetailsUI uiInstance)
    {
        _assetScrollView.IsVisible = uiInstance.selectedAssetBox.IsVisible;
    }
}