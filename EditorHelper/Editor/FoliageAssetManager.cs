using EditorHelper.Builders;
using EditorHelper.Writers;
using SDG.Unturned;

namespace EditorHelper.Editor
{
    public class FoliageAssetManager
    {
        private readonly SleekButtonIcon _densitySaveButton;
        private readonly ISleekFloat32Field _densityField;

        // Save it in variable, because I don't know how else I can make it work
        private EditorTerrainDetailsUI _currentUIInstance;
        
        public FoliageAssetManager()
        {
            UIBuilder builder = new();

            builder.SetPositionScaleX(1f)
                .SetPositionScaleY(1f)
                .SetPositionOffsetX(-200f)
                .SetPositionOffsetY(-30f)
                .SetSizeOffsetX(200)
                .SetSizeOffsetY(30);
            
            _densitySaveButton = builder.BuildButton("Write to file.");
            _densitySaveButton.onClickedButton += OnClickedButton;
            _densitySaveButton.text = "Save";

            builder.SetPositionScaleX(1f)
                .SetPositionScaleY(1f)
                .SetPositionOffsetX(-200f)
                .SetPositionOffsetY(-70f)
                .SetSizeOffsetX(200)
                .SetSizeOffsetY(30);
            
            _densityField = builder.BuildFloatInput();
            _densityField.AddLabel("Density", ESleekSide.LEFT);
            _densityField.OnValueChanged += OnValueChanged;
        }
        
        public void Initialize(ref EditorTerrainDetailsUI uiInstance)
        {
            _currentUIInstance = uiInstance;
            uiInstance.assetScrollView.SizeOffset_Y = -200;
            
            uiInstance.AddChild(_densitySaveButton);
            uiInstance.AddChild(_densityField);
        }

        public void CustomUpdate(EditorTerrainDetailsUI uiInstance)
        {
            bool visibility = uiInstance.searchTypeButton.state == 0 &&
                              uiInstance.tool.mode != FoliageEditor.EFoliageMode.BAKE;
            _densitySaveButton.IsVisible = visibility;
            _densityField.IsVisible = visibility;
            
            if (_currentUIInstance.tool.selectedInstanceAsset != null)
                _densityField.Value = _currentUIInstance.tool.selectedInstanceAsset.density;
            else _densityField.Value = 0;
        }

        private void OnClickedButton(ISleekElement button)
        {
            _currentUIInstance.tool.selectedInstanceAsset.density = _densityField.Value;
            AssetWriter.SaveFoliageInfoAssetDensity(_currentUIInstance.tool.selectedInstanceAsset);
        }

        private void OnValueChanged(ISleekFloat32Field field, float value)
        {
            _currentUIInstance.tool.selectedInstanceAsset.density = value;
        }
    }
}