using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Types;
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

    private readonly ISleekButton _2XResolution;
    private readonly ISleekButton _4XResolution;
    private readonly ISleekLabel _multiplierResolution;
    private readonly ISleekField _widthResolution;
    private readonly ISleekField _heightResolution;

    private MapResolution DefaultResolution;

    public int? Multiplier
    {
        get;
        private set
        {
            field = value;

            _multiplierResolution.IsVisible = Multiplier != null;
            if (Multiplier != null) _multiplierResolution.Text = $"{Multiplier.Value}x";
        }
    }
    public MapResolution CustomResolution;
    public bool ShouldModifyResolution = false;

    public MapResolutionExtension()
    {
        UIBuilder builder = new(40f, 30f);
        
        builder.SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(0.5f)
            .SetOffsetHorizontal(-145f)
            .SetOffsetVertical(-55f)
            .SetText("2x");
        
        _2XResolution = builder.BuildButton("2x satellite resolution");

        builder.SetSpacing(0)
            .SetOffsetHorizontal(-187.5f)
            .SetText("4x");
        
        _4XResolution = builder.BuildButton("4x satellite resolution");

        builder.SetOffsetHorizontal(-212.5f)
            .SetOffsetVertical(-82.5f)
            .SetText("2x");

        _multiplierResolution = builder.BuildLabel();
        _multiplierResolution.IsVisible = false;
        _multiplierResolution.TextContrastContext = ETextContrastContext.ColorfulBackdrop;

        builder.SetSpacing(0)
            .SetOffsetHorizontal(-255f)
            .SetOffsetVertical(-70f)
            .SetSizeHorizontal(65f)
            .SetText("Width");
        
        _widthResolution = builder.BuildStringField();
        _widthResolution.TooltipText = "Width resolution";
        _widthResolution.AddLabel("Width", ESleekSide.LEFT);

        builder.SetSpacing(0)
            .SetOffsetVertical(-40f)
            .SetText("Height");
        
        _heightResolution = builder.BuildStringField();
        _heightResolution.TooltipText = "Height resolution";
        _heightResolution.AddLabel("Height", ESleekSide.LEFT);

        Initialize();
    }

    public void Initialize()
    {
        if (_container == null) return;
        
        _2XResolution.OnClicked += On2XButtonClicked;
        _4XResolution.OnClicked += On4XButtonClicked;
        _widthResolution.OnTextChanged += OnWidthResolutionChanged;
        _heightResolution.OnTextChanged += OnHeightResolutionChanged;

        _container.AddChild(_2XResolution);
        _container.AddChild(_4XResolution);
        _container.AddChild(_widthResolution);
        _container.AddChild(_heightResolution);
        _container.AddChild(_multiplierResolution);
    }

    protected override void Opened()
    {
        CartographyVolume? mainVolume = VolumeManager<CartographyVolume, CartographyVolumeManager>.Get().GetMainVolume();
        if (mainVolume != null)
        {
            Vector3 volumeSize = mainVolume.CalculateLocalBounds().size;
            DefaultResolution.Width = (uint)Mathf.CeilToInt(volumeSize.x);
            DefaultResolution.Height = (uint)Mathf.CeilToInt(volumeSize.z);
        }
        else
        {
            DefaultResolution.Width = SDG.Unturned.Level.size;
            DefaultResolution.Height = SDG.Unturned.Level.size;
        }

        if (CustomResolution.Height > 0)
            _widthResolution.PlaceholderText = CustomResolution.GetAspectWidth(DefaultResolution).ToString();
        else
            _widthResolution.PlaceholderText = DefaultResolution.Width.ToString();

        if (CustomResolution.Width > 0)
            _heightResolution.PlaceholderText = CustomResolution.GetAspectHeight(DefaultResolution).ToString();
        else
            _heightResolution.PlaceholderText = DefaultResolution.Height.ToString();
    }

    #region Event handlers
    private void On2XButtonClicked(ISleekElement button)
    {
        if (Multiplier == 2)
        {
            Multiplier = null;
            ShouldModifyResolution = CustomResolution.Width != 0 || CustomResolution.Height != 0;
            return;
        }
        Multiplier = 2;
        ShouldModifyResolution = true;
    }
    
    private void On4XButtonClicked(ISleekElement button)
    {
        if (Multiplier == 4)
        {
            Multiplier = null;
            ShouldModifyResolution = CustomResolution.Width != 0 || CustomResolution.Height != 0;
            return;
        }
        Multiplier = 4;
        ShouldModifyResolution = true;
    }

    private void OnWidthResolutionChanged(ISleekField field, string text)
    {
        bool parsedWidth = uint.TryParse(text, out uint widthValue);

        if (string.IsNullOrEmpty(text) || parsedWidth && widthValue < 1)
        {
            CustomResolution.Width = 0;
            _widthResolution.Text = string.Empty;
            _heightResolution.PlaceholderText = DefaultResolution.Height.ToString();
            ShouldModifyResolution = CustomResolution.Height != 0 || Multiplier != null;
            return;
        }

        if (!parsedWidth)
        {
            _widthResolution.Text = CustomResolution.Width > 0 ? CustomResolution.Width.ToString() : string.Empty;
            return;
        }

        CustomResolution.Width = widthValue;
        _heightResolution.PlaceholderText = CustomResolution.GetAspectHeight(DefaultResolution).ToString();
        ShouldModifyResolution = true;
    }

    private void OnHeightResolutionChanged(ISleekField field, string text)
    {
        bool parsedHeight = uint.TryParse(text, out uint heightValue);

        if (string.IsNullOrEmpty(text) || parsedHeight && heightValue < 1)
        {
            CustomResolution.Height = 0;
            _heightResolution.Text = string.Empty;
            _widthResolution.PlaceholderText = DefaultResolution.Width.ToString();
            ShouldModifyResolution = CustomResolution.Width != 0 || Multiplier != null;
            return;
        }

        if (!parsedHeight)
        {
            _heightResolution.Text = CustomResolution.Height > 0 ? CustomResolution.Height.ToString() : string.Empty;
            return;
        }

        CustomResolution.Height = heightValue;
        _widthResolution.PlaceholderText = CustomResolution.GetAspectWidth(DefaultResolution).ToString();
        ShouldModifyResolution = true;
    }
    #endregion Event handlers

    #region Extension Functions
    public void ResetCustomResolution()
    {
        _widthResolution.Text = string.Empty;
        _widthResolution.PlaceholderText = DefaultResolution.Width.ToString();
        _heightResolution.Text = string.Empty;
        _heightResolution.PlaceholderText = DefaultResolution.Height.ToString();
        Multiplier = null;
        CustomResolution = new MapResolution();
        
        _widthResolution.BackgroundColor = SleekColor.BackgroundIfLight(Color.black);
        _heightResolution.BackgroundColor = SleekColor.BackgroundIfLight(Color.black);
        _2XResolution.BackgroundColor = SleekColor.BackgroundIfLight(Color.black);
        _4XResolution.BackgroundColor = SleekColor.BackgroundIfLight(Color.black);
        
        ShouldModifyResolution = false;
    }
    #endregion Extension Functions

    public void Dispose()
    {
        if (_container == null) return;
        ResetCustomResolution();
        
        _2XResolution.OnClicked -= On2XButtonClicked;
        _4XResolution.OnClicked -= On4XButtonClicked;
        _widthResolution.OnTextChanged -= OnWidthResolutionChanged;
        _heightResolution.OnTextChanged -= OnHeightResolutionChanged;

        _container.RemoveChild(_2XResolution);
        _container.RemoveChild(_4XResolution);
        _container.RemoveChild(_widthResolution);
        _container.RemoveChild(_heightResolution);
        _container.RemoveChild(_multiplierResolution);
    }
}