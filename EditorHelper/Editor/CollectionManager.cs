using System.Collections.Generic;
using EditorHelper.Builders;
using SDG.Framework.Foliage;
using SDG.Unturned;

namespace EditorHelper.Editor;

public class CollectionManager
{
    private readonly ISleekBox _box;
    private readonly SleekList<FoliageInfoAsset> _assetScrollView;

    public CollectionManager()
    {
        UIBuilder builder = new(650f, 400f);

        builder.SetPositionScaleX(0.5f)
            .SetPositionScaleY(0.5f)
            .SetPositionOffsetX(-325f)
            .SetPositionOffsetY(-200f);

        _box = builder.BuildBox();
        
        builder = new UIBuilder().SetPositionScaleX(0f)
            .SetPositionScaleY(1f)
            .SetPositionOffsetX(10f)
            .SetPositionOffsetY(-390f) // Then we define the area of the scroll box. It's negative due we're already at the bottom so we need to go up.
            .SetSizeOffsetX(630f) // Size/Area of the scrollbox
            .SetSizeOffsetY(-10f) // And once the scrollbox area is done we added the bottom padding
            .SetOneTimeSpacing(0f);

        _assetScrollView = builder.BuildScrollBox<FoliageInfoAsset>(25, 5);
        _assetScrollView.onCreateElement = OnCreateElement;
        _box.AddChild(_assetScrollView);
    }

    private ISleekElement OnCreateElement(FoliageInfoAsset item)
    {
        ISleekButton button = Glazier.Get().CreateButton();
        button.Text = item.name;

        return button;
    }

    public void Initialize(ref EditorTerrainDetailsUI uiInstance)
    {
        uiInstance.AddChild(_box);
        _box.IsVisible = true;
        
        List<FoliageInfoAsset> foliageAssets = new();
        Assets.find(foliageAssets);
        
        _assetScrollView.SetData(foliageAssets);
        _assetScrollView.Update();
    }

    public void CustomUpdate(EditorTerrainDetailsUI uiInstance)
    {
        _box.IsVisible = uiInstance.selectedAssetBox.IsVisible;
    }
}