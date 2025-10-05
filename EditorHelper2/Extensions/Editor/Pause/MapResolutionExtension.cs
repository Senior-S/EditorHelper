using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.UI.Builders;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Editor.Pause;

[UIExtension(typeof(EditorPauseUI))]
[EHExtension("Extra Map Resolution extension", "Senior S & Gamingtoday093")]
public class MapResolutionExtension : UIExtension, IExtension
{
    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;
    
    private readonly SleekButtonIcon _2XResolution;
    private readonly SleekButtonIcon _4XResolution;
    private readonly ISleekInt32Field _widthResolution;
    private readonly ISleekInt32Field _heightResolution;

    public int? Multiplier;
    public (uint?, uint?) CustomResolution;
    public bool ShouldModifyResolution = false;
    
    public MapResolutionExtension()
    {
        UIBuilder builder = new(40f, 30f);

        builder.SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(0.5f)
            .SetOffsetHorizontal(-145f)
            .SetOffsetVertical(-55f)
            .SetText("2x");
        
        _2XResolution = builder.BuildButtonIcon("2x satellite resolution");
        
        builder.SetSpacing(0)
            .SetOffsetHorizontal(-187.5f)
            .SetText("4x");
        
        _4XResolution = builder.BuildButtonIcon("4x satellite resolution");

        builder.SetSpacing(0)
            .SetOffsetHorizontal(-255f)
            .SetOffsetVertical(-70f)
            .SetSizeHorizontal(65f)
            .SetText("Width");
        
        _widthResolution = builder.BuildInt32Field("Width resolution");
        
        builder.SetSpacing(0)
            .SetOffsetVertical(-40f)
            .SetText("Height");
        
        _heightResolution = builder.BuildInt32Field("Height resolution");

        Initialize();
    }

    public void Initialize()
    {
        if (_container == null) return;
        
        _2XResolution.onClickedButton += On2XButtonClicked;
        _4XResolution.onClickedButton += On4XButtonClicked;
        _widthResolution.OnValueChanged += OnWidthResolutionChanged;
        _heightResolution.OnValueChanged += OnHeightResolutionChanged;
        
        _container.AddChild(_2XResolution);
        _container.AddChild(_4XResolution);
        _container.AddChild(_widthResolution);
        _container.AddChild(_heightResolution);
    }

    #region Event handlers
    private void On2XButtonClicked(ISleekElement button)
    {
        if (Multiplier == 2)
        {
            Multiplier = null;
            _2XResolution.backgroundColor = SleekColor.BackgroundIfLight(Color.black);
            return;
        }
        Multiplier = 2;
        _2XResolution.backgroundColor = SleekColor.BackgroundIfLight(Color.white);
        ShouldModifyResolution = true;
    }
    
    private void On4XButtonClicked(ISleekElement button)
    {
        if (Multiplier == 4)
        {
            Multiplier = null;
            _4XResolution.backgroundColor = SleekColor.BackgroundIfLight(Color.black);
            return;
        }
        Multiplier = 4;
        _4XResolution.backgroundColor = SleekColor.BackgroundIfLight(Color.white);
        ShouldModifyResolution = true;
    }
    
    private void OnWidthResolutionChanged(ISleekInt32Field field, int value)
    {
        if (_widthResolution.Value < 1)
        {
            CustomResolution.Item1 = null;
            _widthResolution.Value = 0;
            _widthResolution.BackgroundColor = SleekColor.BackgroundIfLight(Color.black);
            return;
        }
            
        CustomResolution.Item1 = (uint)value;
        ShouldModifyResolution = true;
        _widthResolution.BackgroundColor = SleekColor.BackgroundIfLight(Color.white);
    }
    
    private void OnHeightResolutionChanged(ISleekInt32Field field, int value)
    {
        if (_heightResolution.Value < 1)
        {
            CustomResolution.Item2 = null;
            _heightResolution.Value = 0;
            _heightResolution.BackgroundColor = SleekColor.BackgroundIfLight(Color.black);
            return;
        }
            
        CustomResolution.Item2 = (uint)value;
        ShouldModifyResolution = true;
        _heightResolution.BackgroundColor = SleekColor.BackgroundIfLight(Color.white);
    }
    #endregion Event handlers

    #region Extension Functions
    public void ResetCustomResolution()
    {
        _widthResolution.Value = 0;
        _heightResolution.Value = 0;
        Multiplier = null;
        CustomResolution = (null, null);
        
        _widthResolution.BackgroundColor = SleekColor.BackgroundIfLight(Color.black);
        _heightResolution.BackgroundColor = SleekColor.BackgroundIfLight(Color.black);
        _2XResolution.backgroundColor = SleekColor.BackgroundIfLight(Color.black);
        _4XResolution.backgroundColor = SleekColor.BackgroundIfLight(Color.black);
        
        ShouldModifyResolution = false;
    }
    #endregion Extension Functions

    public void Dispose()
    {
        if (_container == null) return;
        ResetCustomResolution();
        
        _2XResolution.onClickedButton -= On2XButtonClicked;
        _4XResolution.onClickedButton -= On4XButtonClicked;
        _widthResolution.OnValueChanged -= OnWidthResolutionChanged;
        _heightResolution.OnValueChanged -= OnHeightResolutionChanged;
        
        _container.RemoveChild(_2XResolution);
        _container.RemoveChild(_4XResolution);
        _container.RemoveChild(_widthResolution);
        _container.RemoveChild(_heightResolution);
    }
}