using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Helpers;
using EditorHelper2.Helpers;
using EditorHelper2.UI.Builders;
using SDG.Unturned;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorHelper2.Extensions.Editor.Pause;

[UIExtension(typeof(EditorPauseUI))]
[EHExtension("Cinematic Mode Tab", "Senior S", alwaysEnabled: true)]
public class CinematicModeExtension : UIExtension, IExtension
{
    private const string ConfigSection = "Cinematic";
    private static CinematicModeExtension? _instance;

    private const float FovMin = 20f;
    private const float FovMax = 120f;
    private const float RollMin = -45f;
    private const float RollMax = 45f;
    private const float DofFocusMin = 1f;
    private const float DofFocusMax = 200f;
    private const float DofApertureMin = 1f;
    private const float DofApertureMax = 32f;
    private const float VignetteMax = 0.75f;
    private const float ExposureMin = -2f;
    private const float ExposureMax = 2f;
    private const float GradeMin = -100f;
    private const float GradeMax = 100f;
    private const float WatermarkRotationMin = -180f;
    private const float WatermarkRotationMax = 180f;
    private const float WatermarkScaleMin = 0.5f;
    private const float WatermarkScaleMax = 3f;
    private const float SmoothnessMin = 1f;
    private const float SmoothnessMax = 24f;
    private const float LeftLabelX = 20f;
    private const float LeftControlX = 150f;
    private const float RightLabelX = -500f;
    private const float RightControlX = -300f;
    private const float LeftLabelWidth = 120f;
    private const float RightLabelWidth = 150f;
    private const float ControlWidth = 220f;

    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _pauseContainer;

    private ISleekButton? _openCinematicButton;
    private SleekFullscreenBox? _cinematicContainer;
    private ISleekButton? _backButton;
    private ISleekToggle? _cinematicToggle;
    private SleekButtonState? _weatherButton;
    private ISleekSlider? _moonSlider;
    private ISleekSlider? _timeSlider;
    private SleekValue? _snowLevelSlider;
    private SleekValue? _seaLevelSlider;
    private ISleekSlider? _fovSlider;
    private ISleekSlider? _rollSlider;
    private ISleekToggle? _dofToggle;
    private ISleekSlider? _dofFocusSlider;
    private ISleekSlider? _dofStrengthSlider;
    private ISleekSlider? _vignetteSlider;
    private ISleekSlider? _exposureSlider;
    private ISleekSlider? _contrastSlider;
    private ISleekSlider? _saturationSlider;
    private ISleekSlider? _temperatureSlider;
    private ISleekSlider? _tintSlider;
    private ISleekButton? _resetVisualsButton;
    private ISleekToggle? _smoothCameraToggle;
    private ISleekSlider? _smoothnessSlider;
    private ISleekToggle? _windToggle;
    private ISleekToggle? _filmGrainToggle;
    private ISleekToggle? _watermarkToggle;
    private ISleekButton? _watermarkSettingsButton;
    private SleekFullscreenBox? _watermarkSettingsPanel;
    private ISleekField? _watermarkTextField;
    private ISleekSlider? _watermarkOpacitySlider;
    private ISleekSlider? _watermarkXSlider;
    private ISleekSlider? _watermarkYSlider;
    private ISleekSlider? _watermarkRotationSlider;
    private ISleekSlider? _watermarkScaleSlider;
    private ISleekButton? _resetWatermarkButton;
    private SleekButtonState? _sunShaftsButton;
    private SleekButtonState? _lightingButton;
    private SleekButtonState? _waterButton;
    private GameObject? _smoothCameraObject;
    private CinematicCameraSmoothingBehaviour? _smoothCameraBehaviour;
    private GameObject? _watermarkObject;
    private CinematicWatermarkRenderer? _watermarkRenderer;
    private bool _active;
    private bool _isRefreshing;
    private bool _isWatermarkEnabled = true;
    private string _watermarkText = "EditorHelper2";
    private float _watermarkOpacity = 0.8f;
    private float _watermarkPositionX = 0.9f;
    private float _watermarkPositionY = 0.92f;
    private float _watermarkRotation;
    private float _watermarkScale = 1f;

    public CinematicModeExtension()
    {
        _instance = this;
        Initialize();
        MapEditorConfigHelper.RegisterExtensionSettings(ConfigSection, CaptureSettings, ApplySettings);
    }

    public static bool IsOpen => _instance?._active == true;

    public static void CloseIfOpen()
    {
        _instance?.CloseToPauseMenu();
    }

    public static void CloseForEditorTabSwitch()
    {
        _instance?.CloseCinematicTab();
    }

    public void Initialize()
    {
        if (_pauseContainer == null)
        {
            return;
        }

        UIBuilder builder = new(0f, 0f);
        BuildCinematicContainer(builder);
        builder.ResetProperties()
            .SetSizeHorizontal(200f)
            .SetSizeVertical(30f)
            .SetOffsetHorizontal(110f)
            .SetOffsetVertical(245f)
            .SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(0.5f)
            .SetText("Cinematic");
        _openCinematicButton = builder.BuildButton("Open cinematic controls for screenshots and flythroughs.");
        _openCinematicButton.OnClicked += OnClickedOpenCinematicButton;
        _pauseContainer.AddChild(_openCinematicButton);
        RefreshAllControls();
    }

    protected override void Opened()
    {
        RefreshAllControls();
    }

    private void BuildCinematicContainer(UIBuilder builder)
    {
        builder.ResetProperties()
            .SetSizeHorizontal(-20f)
            .SetSizeVertical(-20f)
            .SetOffsetHorizontal(10f)
            .SetOffsetVertical(10f)
            .SetAnchorHorizontal(1f)
            .SetScaleHorizontal(1f)
            .SetScaleVertical(1f);
        _cinematicContainer = builder.BuildFullscreenBox();
        EditorUI.window.AddChild(_cinematicContainer);

        AddRowLabel(builder, "Camera FOV", 90f, "Overrides the editor camera field of view for wider or tighter cinematic shots.", useLeftColumn: true);
        _fovSlider = CreateSlider(builder, 90f, useLeftColumn: true);
        _fovSlider.OnValueChanged += OnFovSliderChanged;

        AddRowLabel(builder, "Camera Roll", 125f, "Tilts the camera around its forward axis. Reset to center for normal editor rotation.", useLeftColumn: true);
        _rollSlider = CreateSlider(builder, 125f, useLeftColumn: true);
        _rollSlider.OnValueChanged += OnRollSliderChanged;

        AddRowLabel(builder, "Depth of Field", 165f, "Enables a cinematic focus blur on the main camera.", useLeftColumn: true);
        _dofToggle = CreateToggle(builder, 165f, "Toggle depth of field.", useLeftColumn: true);
        _dofToggle.OnValueChanged += OnDepthOfFieldToggleChanged;

        AddRowLabel(builder, "DOF Focus", 205f, "Distance from the camera that remains sharp while depth of field is enabled.", useLeftColumn: true);
        _dofFocusSlider = CreateSlider(builder, 205f, useLeftColumn: true);
        _dofFocusSlider.OnValueChanged += OnDepthOfFieldFocusSliderChanged;

        AddRowLabel(builder, "DOF Strength", 245f, "Higher values increase the blur amount by lowering aperture.", useLeftColumn: true);
        _dofStrengthSlider = CreateSlider(builder, 245f, useLeftColumn: true);
        _dofStrengthSlider.OnValueChanged += OnDepthOfFieldStrengthSliderChanged;

        AddRowLabel(builder, "Vignette", 285f, "Darkens the image edges for cinematic framing.", useLeftColumn: true);
        _vignetteSlider = CreateSlider(builder, 285f, useLeftColumn: true);
        _vignetteSlider.OnValueChanged += OnVignetteSliderChanged;

        AddRowLabel(builder, "Exposure", 325f, "Adjusts post-process exposure. Center is neutral.", useLeftColumn: true);
        _exposureSlider = CreateSlider(builder, 325f, useLeftColumn: true);
        _exposureSlider.OnValueChanged += OnExposureSliderChanged;

        AddRowLabel(builder, "Contrast", 365f, "Adjusts color grading contrast. Center is neutral.", useLeftColumn: true);
        _contrastSlider = CreateSlider(builder, 365f, useLeftColumn: true);
        _contrastSlider.OnValueChanged += OnContrastSliderChanged;

        AddRowLabel(builder, "Saturation", 405f, "Adjusts color grading saturation. Center is neutral.", useLeftColumn: true);
        _saturationSlider = CreateSlider(builder, 405f, useLeftColumn: true);
        _saturationSlider.OnValueChanged += OnSaturationSliderChanged;

        AddRowLabel(builder, "Temperature", 445f, "Warms or cools the color grade. Center is neutral.", useLeftColumn: true);
        _temperatureSlider = CreateSlider(builder, 445f, useLeftColumn: true);
        _temperatureSlider.OnValueChanged += OnTemperatureSliderChanged;

        AddRowLabel(builder, "Tint", 485f, "Shifts the color grade toward green or magenta. Center is neutral.", useLeftColumn: true);
        _tintSlider = CreateSlider(builder, 485f, useLeftColumn: true);
        _tintSlider.OnValueChanged += OnTintSliderChanged;

        builder.ResetProperties()
            .SetSizeHorizontal(ControlWidth)
            .SetSizeVertical(30f)
            .SetOffsetHorizontal(LeftControlX)
            .SetOffsetVertical(525f)
            .SetText("Reset Visuals");
        _resetVisualsButton = builder.BuildButton("Reset camera FOV, roll, depth of field, vignette, and color grading overrides.");
        _resetVisualsButton.OnClicked += OnClickedResetVisualsButton;
        _cinematicContainer.AddChild(_resetVisualsButton);

        AddRowLabel(builder, "Cinematic", 90f, "Toggles Unturned's -Cinematic mode. It renders much more of the map and can reduce performance heavily.");
        _cinematicToggle = CreateToggle(builder, 90f, "Render the whole map for screenshots and flythroughs.");
        _cinematicToggle.OnValueChanged += OnCinematicToggleChanged;

        AddRowLabel(builder, "Weather", 130f, "Preview fixed weather for screenshots. Thunder uses Unturned's heavy rain weather with lightning.");
        _weatherButton = CreateButtonState(builder, 130f,
            new GUIContent("None", "Disable active weather."),
            new GUIContent("Normal Rain", "Use Unturned's default rain weather."),
            new GUIContent("Thunder", "Use the heavy rain weather asset with lightning."),
            new GUIContent("Snow", "Use Unturned's default snow weather."));
        _weatherButton.onSwappedState += OnWeatherStateChanged;

        AddRowLabel(builder, "Moon", 170f, "Matches the moon slider from Environment/Lighting.");
        _moonSlider = CreateSlider(builder, 170f);
        _moonSlider.OnValueChanged += OnMoonSliderChanged;

        AddRowLabel(builder, "Time", 205f, "Matches the time slider from Environment/Lighting.");
        _timeSlider = CreateSlider(builder, 205f);
        _timeSlider.OnValueChanged += OnTimeSliderChanged;

        AddRowLabel(builder, "Snow Level", 240f, "Matches the snow level control from Environment/Lighting.");
        _snowLevelSlider = CreateValueSlider(builder, 240f);
        _snowLevelSlider.onValued += OnSnowLevelChanged;

        AddRowLabel(builder, "Sea Level", 280f, "Matches the sea level control from Environment/Lighting.");
        _seaLevelSlider = CreateValueSlider(builder, 280f);
        _seaLevelSlider.onValued += OnSeaLevelChanged;

        AddRowLabel(builder, "Smooth Camera", 325f, "Interpolates the editor camera after normal movement for smoother flythrough recording.");
        _smoothCameraToggle = CreateToggle(builder, 325f, "Smooth editor camera movement.");
        _smoothCameraToggle.OnValueChanged += OnSmoothCameraToggleChanged;

        AddRowLabel(builder, "Smoothness", 365f, "Higher values follow normal camera movement more tightly. Lower values feel more floaty.");
        _smoothnessSlider = CreateSlider(builder, 365f);
        _smoothnessSlider.OnValueChanged += OnSmoothnessSliderChanged;

        AddRowLabel(builder, "Wind Effects", 405f, "Toggles the existing graphics wind effects option.");
        _windToggle = CreateToggle(builder, 405f, "Toggle graphics wind effects.");
        _windToggle.OnValueChanged += OnWindToggleChanged;

        AddRowLabel(builder, "Film Grain", 445f, "Toggles the existing graphics film grain option.");
        _filmGrainToggle = CreateToggle(builder, 445f, "Toggle post-process film grain.");
        _filmGrainToggle.OnValueChanged += OnFilmGrainToggleChanged;

        AddRowLabel(builder, "Sun Shafts", 485f, "Matches the graphics Sun Shafts Quality option.");
        _sunShaftsButton = CreateQualityButton(builder, 485f, CinematicModeUtility.GraphicQualityStates);
        _sunShaftsButton.onSwappedState += OnSunShaftsQualityChanged;

        AddRowLabel(builder, "Lighting", 525f, "Matches the graphics Lighting Quality option.");
        _lightingButton = CreateQualityButton(builder, 525f, CinematicModeUtility.GraphicQualityStates);
        _lightingButton.onSwappedState += OnLightingQualityChanged;

        AddRowLabel(builder, "Water", 565f, "Matches the graphics Water Quality option.");
        _waterButton = CreateQualityButton(builder, 565f, CinematicModeUtility.WaterQualityStates);
        _waterButton.onSwappedState += OnWaterQualityChanged;

        AddRowLabel(builder, "Watermark", 565f, "Shows the configured watermark while using cinematic capture options.", useLeftColumn: true);
        _watermarkToggle = CreateToggle(builder, 565f, "Toggle the watermark.", useLeftColumn: true);
        _watermarkToggle.OnValueChanged += OnWatermarkToggleChanged;

        builder.ResetProperties()
            .SetSizeHorizontal(170f)
            .SetSizeVertical(30f)
            .SetOffsetHorizontal(LeftControlX + 50f)
            .SetOffsetVertical(565f)
            .SetText("Settings");
        _watermarkSettingsButton = builder.BuildButton("Open watermark text, opacity, position, rotation, and scale settings.");
        _watermarkSettingsButton.OnClicked += OnClickedWatermarkSettingsButton;
        _cinematicContainer.AddChild(_watermarkSettingsButton);

        builder.ResetProperties()
            .SetSizeHorizontal(200f)
            .SetSizeVertical(30f)
            .SetOffsetHorizontal(-240f)
            .SetOffsetVertical(-50f)
            .SetAnchorHorizontal(1f)
            .SetAnchorVertical(1f)
            .SetText("Back");
        _backButton = builder.BuildButton("Return to the pause menu.");
        _backButton.OnClicked += OnClickedBackButton;
        _cinematicContainer.AddChild(_backButton);

        AddWatermarkSettingsPanel(builder);
        _watermarkObject = new GameObject("EditorHelper2 Cinematic Watermark");
        Object.DontDestroyOnLoad(_watermarkObject);
        _watermarkRenderer = _watermarkObject.AddComponent<CinematicWatermarkRenderer>();
        ApplyWatermarkSettings();
    }

    private void AddRowLabel(UIBuilder builder, string text, float y, string tooltip, bool useLeftColumn = false)
    {
        builder.ResetProperties()
            .SetSizeHorizontal(useLeftColumn ? LeftLabelWidth : RightLabelWidth)
            .SetSizeVertical(30f)
            .SetOffsetHorizontal(useLeftColumn ? LeftLabelX : RightLabelX)
            .SetOffsetVertical(y)
            .SetAnchorHorizontal(useLeftColumn ? 0f : 1f)
            .SetText(text);
        ISleekBox label = builder.BuildBox();
        label.FontSize = ESleekFontSize.Small;
        label.TooltipText = tooltip;
        _cinematicContainer!.AddChild(label);
    }

    private ISleekToggle CreateToggle(UIBuilder builder, float y, string tooltip, bool useLeftColumn = false)
    {
        builder.ResetProperties()
            .SetSizeHorizontal(40f)
            .SetSizeVertical(40f)
            .SetOffsetHorizontal(useLeftColumn ? LeftControlX : RightControlX)
            .SetOffsetVertical(y - 5f)
            .SetAnchorHorizontal(useLeftColumn ? 0f : 1f);
        ISleekToggle toggle = builder.BuildToggle(tooltip);
        _cinematicContainer!.AddChild(toggle);
        return toggle;
    }

    private ISleekSlider CreateSlider(UIBuilder builder, float y, bool useLeftColumn = false)
    {
        builder.ResetProperties()
            .SetSizeHorizontal(ControlWidth)
            .SetSizeVertical(20f)
            .SetOffsetHorizontal(useLeftColumn ? LeftControlX : RightControlX)
            .SetOffsetVertical(y + 5f)
            .SetAnchorHorizontal(useLeftColumn ? 0f : 1f);
        ISleekSlider slider = builder.BuildSlider();
        _cinematicContainer!.AddChild(slider);
        return slider;
    }

    private SleekValue CreateValueSlider(UIBuilder builder, float y)
    {
        builder.ResetProperties()
            .SetSizeHorizontal(ControlWidth)
            .SetSizeVertical(30f)
            .SetOffsetHorizontal(RightControlX)
            .SetOffsetVertical(y)
            .SetAnchorHorizontal(1f);
        SleekValue slider = builder.BuildValue();
        _cinematicContainer!.AddChild(slider);
        return slider;
    }

    private SleekButtonState CreateButtonState(UIBuilder builder, float y, params GUIContent[] states)
    {
        builder.ResetProperties()
            .SetSizeHorizontal(ControlWidth)
            .SetSizeVertical(30f)
            .SetOffsetHorizontal(RightControlX)
            .SetOffsetVertical(y)
            .SetAnchorHorizontal(1f);
        SleekButtonState button = builder.BuildButtonState(states);
        button.UseContentTooltip = true;
        _cinematicContainer!.AddChild(button);
        return button;
    }

    private SleekButtonState CreateQualityButton(UIBuilder builder, float y, EGraphicQuality[] qualities)
    {
        GUIContent[] states = new GUIContent[qualities.Length];
        for (int i = 0; i < qualities.Length; i++)
        {
            string label = CinematicModeUtility.FormatQualityName(qualities[i]);
            states[i] = new GUIContent(label, $"Set quality to {label}.");
        }

        return CreateButtonState(builder, y, states);
    }

    private void AddWatermarkSettingsPanel(UIBuilder builder)
    {
        builder.ResetProperties()
            .SetSizeHorizontal(460f)
            .SetSizeVertical(300f)
            .SetOffsetHorizontal(-230f)
            .SetOffsetVertical(120f)
            .SetAnchorHorizontal(0.5f);
        _watermarkSettingsPanel = builder.BuildFullscreenBox();
        _watermarkSettingsPanel.IsVisible = false;
        _cinematicContainer!.AddChild(_watermarkSettingsPanel);

        AddWatermarkPanelLabel(builder, "Watermark Text", 20f);
        builder.ResetProperties()
            .SetSizeHorizontal(240f)
            .SetSizeVertical(30f)
            .SetOffsetHorizontal(180f)
            .SetOffsetVertical(20f);
        _watermarkTextField = builder.BuildStringField();
        _watermarkTextField.Text = _watermarkText;
        _watermarkTextField.MaxLength = 64;
        _watermarkTextField.TextColor = ESleekTint.FONT;
        _watermarkTextField.OnTextChanged += OnWatermarkTextChanged;
        _watermarkSettingsPanel.AddChild(_watermarkTextField);

        AddWatermarkPanelLabel(builder, "Opacity", 60f);
        _watermarkOpacitySlider = CreateWatermarkPanelSlider(builder, 60f);
        _watermarkOpacitySlider.OnValueChanged += OnWatermarkOpacityChanged;

        AddWatermarkPanelLabel(builder, "Position X", 95f);
        _watermarkXSlider = CreateWatermarkPanelSlider(builder, 95f);
        _watermarkXSlider.OnValueChanged += OnWatermarkXChanged;

        AddWatermarkPanelLabel(builder, "Position Y", 130f);
        _watermarkYSlider = CreateWatermarkPanelSlider(builder, 130f);
        _watermarkYSlider.OnValueChanged += OnWatermarkYChanged;

        AddWatermarkPanelLabel(builder, "Rotation", 165f);
        _watermarkRotationSlider = CreateWatermarkPanelSlider(builder, 165f);
        _watermarkRotationSlider.OnValueChanged += OnWatermarkRotationChanged;

        AddWatermarkPanelLabel(builder, "Scale", 200f);
        _watermarkScaleSlider = CreateWatermarkPanelSlider(builder, 200f);
        _watermarkScaleSlider.OnValueChanged += OnWatermarkScaleChanged;

        builder.ResetProperties()
            .SetSizeHorizontal(240f)
            .SetSizeVertical(30f)
            .SetOffsetHorizontal(180f)
            .SetOffsetVertical(245f)
            .SetText("Reset Watermark");
        _resetWatermarkButton = builder.BuildButton();
        _resetWatermarkButton.OnClicked += OnClickedResetWatermarkButton;
        _watermarkSettingsPanel.AddChild(_resetWatermarkButton);
    }

    private void AddWatermarkPanelLabel(UIBuilder builder, string text, float y)
    {
        builder.ResetProperties()
            .SetSizeHorizontal(140f)
            .SetSizeVertical(30f)
            .SetOffsetHorizontal(20f)
            .SetOffsetVertical(y)
            .SetText(text);
        ISleekBox label = builder.BuildBox();
        label.FontSize = ESleekFontSize.Small;
        _watermarkSettingsPanel!.AddChild(label);
    }

    private ISleekSlider CreateWatermarkPanelSlider(UIBuilder builder, float y)
    {
        builder.ResetProperties()
            .SetSizeHorizontal(240f)
            .SetSizeVertical(20f)
            .SetOffsetHorizontal(180f)
            .SetOffsetVertical(y + 5f);
        ISleekSlider slider = builder.BuildSlider();
        _watermarkSettingsPanel!.AddChild(slider);
        return slider;
    }

    private void OnClickedOpenCinematicButton(ISleekElement button)
    {
        EditorPauseUI.close();
        OpenCinematicTab();
    }

    private void OnClickedBackButton(ISleekElement button)
    {
        CloseToPauseMenu();
    }

    private void OpenCinematicTab()
    {
        if (_active)
        {
            RefreshAllControls();
            return;
        }

        _active = true;
        RefreshAllControls();
        RefreshWatermarkVisibility();
        _cinematicContainer?.AnimateIntoView();
    }

    private void CloseCinematicTab()
    {
        if (!_active)
        {
            return;
        }

        _active = false;
        _cinematicContainer?.AnimateOutOfView(1f, 0f);
        RefreshWatermarkVisibility();
    }

    private void CloseToPauseMenu()
    {
        if (!_active)
        {
            return;
        }

        CloseCinematicTab();
        EditorPauseUI.open();
    }

    private void RefreshAllControls()
    {
        _isRefreshing = true;

        if (_cinematicToggle != null)
        {
            _cinematicToggle.Value = GraphicsSettings.WantsCinematicMode;
        }

        if (_weatherButton != null)
        {
            _weatherButton.state = (int)CinematicModeUtility.GetCurrentWeatherMode();
        }

        if (_moonSlider != null)
        {
            _moonSlider.Value = (float)(int)LevelLighting.moon / (float)(int)LevelLighting.MOON_CYCLES;
        }

        if (_timeSlider != null)
        {
            _timeSlider.Value = LevelLighting.time;
        }

        if (_snowLevelSlider != null)
        {
            _snowLevelSlider.state = LevelLighting.snowLevel;
        }

        if (_seaLevelSlider != null)
        {
            _seaLevelSlider.state = LevelLighting.seaLevel;
        }

        EnsureSmoothCameraBehaviour();
        if (_fovSlider != null)
        {
            _fovSlider.Value = RangedValueToSliderValue(_smoothCameraBehaviour?.TargetFov ?? OptionsSettings.DesiredVerticalFieldOfView, FovMin, FovMax);
        }

        if (_rollSlider != null)
        {
            _rollSlider.Value = RangedValueToSliderValue(_smoothCameraBehaviour?.RollDegrees ?? 0f, RollMin, RollMax);
        }

        if (_dofToggle != null)
        {
            _dofToggle.Value = _smoothCameraBehaviour?.IsDepthOfFieldEnabled == true;
        }

        if (_dofFocusSlider != null)
        {
            _dofFocusSlider.Value = RangedValueToSliderValue(_smoothCameraBehaviour?.DepthOfFieldFocusDistance ?? 25f, DofFocusMin, DofFocusMax);
        }

        if (_dofStrengthSlider != null)
        {
            _dofStrengthSlider.Value = RangedValueToSliderValue(_smoothCameraBehaviour?.DepthOfFieldAperture ?? 5.6f, DofApertureMax, DofApertureMin);
        }

        if (_vignetteSlider != null)
        {
            _vignetteSlider.Value = RangedValueToSliderValue(_smoothCameraBehaviour?.VignetteIntensity ?? 0f, 0f, VignetteMax);
        }

        if (_exposureSlider != null)
        {
            _exposureSlider.Value = RangedValueToSliderValue(_smoothCameraBehaviour?.Exposure ?? 0f, ExposureMin, ExposureMax);
        }

        if (_contrastSlider != null)
        {
            _contrastSlider.Value = RangedValueToSliderValue(_smoothCameraBehaviour?.Contrast ?? 0f, GradeMin, GradeMax);
        }

        if (_saturationSlider != null)
        {
            _saturationSlider.Value = RangedValueToSliderValue(_smoothCameraBehaviour?.Saturation ?? 0f, GradeMin, GradeMax);
        }

        if (_temperatureSlider != null)
        {
            _temperatureSlider.Value = RangedValueToSliderValue(_smoothCameraBehaviour?.Temperature ?? 0f, GradeMin, GradeMax);
        }

        if (_tintSlider != null)
        {
            _tintSlider.Value = RangedValueToSliderValue(_smoothCameraBehaviour?.Tint ?? 0f, GradeMin, GradeMax);
        }

        if (_smoothCameraToggle != null)
        {
            _smoothCameraToggle.Value = _smoothCameraBehaviour != null && _smoothCameraBehaviour.IsSmoothingEnabled;
        }

        if (_smoothnessSlider != null)
        {
            _smoothnessSlider.Value = RangedValueToSliderValue(_smoothCameraBehaviour?.Smoothness ?? 8f, SmoothnessMax, SmoothnessMin);
        }

        if (_windToggle != null)
        {
            _windToggle.Value = GraphicsSettings.IsWindEnabled;
        }

        if (_filmGrainToggle != null)
        {
            _filmGrainToggle.Value = GraphicsSettings.filmGrain;
        }

        if (_watermarkToggle != null)
        {
            _watermarkToggle.Value = _isWatermarkEnabled;
        }

        if (_watermarkTextField != null)
        {
            _watermarkTextField.Text = _watermarkText;
        }

        if (_watermarkOpacitySlider != null)
        {
            _watermarkOpacitySlider.Value = _watermarkOpacity;
        }

        if (_watermarkXSlider != null)
        {
            _watermarkXSlider.Value = _watermarkPositionX;
        }

        if (_watermarkYSlider != null)
        {
            _watermarkYSlider.Value = _watermarkPositionY;
        }

        if (_watermarkRotationSlider != null)
        {
            _watermarkRotationSlider.Value = RangedValueToSliderValue(_watermarkRotation, WatermarkRotationMin, WatermarkRotationMax);
        }

        if (_watermarkScaleSlider != null)
        {
            _watermarkScaleSlider.Value = RangedValueToSliderValue(_watermarkScale, WatermarkScaleMin, WatermarkScaleMax);
        }

        if (_sunShaftsButton != null)
        {
            _sunShaftsButton.state = CinematicModeUtility.IndexOfQuality(CinematicModeUtility.GraphicQualityStates, GraphicsSettings.sunShaftsQuality);
        }

        if (_lightingButton != null)
        {
            _lightingButton.state = CinematicModeUtility.IndexOfQuality(CinematicModeUtility.GraphicQualityStates, GraphicsSettings.lightingQuality);
        }

        if (_waterButton != null)
        {
            _waterButton.state = CinematicModeUtility.IndexOfQuality(CinematicModeUtility.WaterQualityStates, GraphicsSettings.waterQuality);
        }

        _isRefreshing = false;
        ApplyWatermarkSettings();
        RefreshWatermarkVisibility();
    }

    private void OnCinematicToggleChanged(ISleekToggle toggle, bool value)
    {
        if (_isRefreshing)
        {
            return;
        }

        if (!CinematicModeUtility.TrySetCinematicMode(value))
        {
            RefreshAllControls();
            UnturnedLog.warn("[EditorHelper2] Unable to toggle cinematic mode because the Unturned graphics flag was not found.");
            return;
        }

        GraphicsSettings.apply("EditorHelper2 cinematic mode toggle");
        CinematicModeUtility.RefreshLoadedVisibility(value);
        RefreshWatermarkVisibility();
    }

    private void OnWeatherStateChanged(SleekButtonState button, int index)
    {
        if (_isRefreshing)
        {
            return;
        }

        SetWeatherMode((CinematicWeatherMode)index);
    }

    private void OnMoonSliderChanged(ISleekSlider slider, float state)
    {
        if (_isRefreshing)
        {
            return;
        }

        byte moon = (byte)(state * (float)(int)LevelLighting.MOON_CYCLES);
        if (moon >= LevelLighting.MOON_CYCLES)
        {
            moon = (byte)(LevelLighting.MOON_CYCLES - 1);
        }

        LevelLighting.moon = moon;
    }

    private void OnTimeSliderChanged(ISleekSlider slider, float state)
    {
        if (_isRefreshing)
        {
            return;
        }

        LevelLighting.time = state;
        LevelLighting.MarkParticleCloudsNeedRestart();
    }

    private void OnSnowLevelChanged(SleekValue slider, float state)
    {
        if (_isRefreshing)
        {
            return;
        }

        LevelLighting.snowLevel = state;
    }

    private void OnSeaLevelChanged(SleekValue slider, float state)
    {
        if (_isRefreshing)
        {
            return;
        }

        LevelLighting.seaLevel = state;
    }

    private void OnFovSliderChanged(ISleekSlider slider, float state)
    {
        if (_isRefreshing)
        {
            return;
        }

        EnsureSmoothCameraBehaviour();
        if (_smoothCameraBehaviour != null)
        {
            _smoothCameraBehaviour.TargetFov = SliderValueToRangedValue(state, FovMin, FovMax);
        }
    }

    private void OnRollSliderChanged(ISleekSlider slider, float state)
    {
        if (_isRefreshing)
        {
            return;
        }

        EnsureSmoothCameraBehaviour();
        if (_smoothCameraBehaviour != null)
        {
            _smoothCameraBehaviour.RollDegrees = SliderValueToRangedValue(state, RollMin, RollMax);
        }
    }

    private void OnDepthOfFieldToggleChanged(ISleekToggle toggle, bool value)
    {
        if (_isRefreshing)
        {
            return;
        }

        EnsureSmoothCameraBehaviour();
        if (_smoothCameraBehaviour != null)
        {
            _smoothCameraBehaviour.IsDepthOfFieldEnabled = value;
        }
    }

    private void OnDepthOfFieldFocusSliderChanged(ISleekSlider slider, float state)
    {
        if (_isRefreshing)
        {
            return;
        }

        EnsureSmoothCameraBehaviour();
        if (_smoothCameraBehaviour != null)
        {
            _smoothCameraBehaviour.DepthOfFieldFocusDistance = SliderValueToRangedValue(state, DofFocusMin, DofFocusMax);
        }
    }

    private void OnDepthOfFieldStrengthSliderChanged(ISleekSlider slider, float state)
    {
        if (_isRefreshing)
        {
            return;
        }

        EnsureSmoothCameraBehaviour();
        if (_smoothCameraBehaviour != null)
        {
            _smoothCameraBehaviour.DepthOfFieldAperture = SliderValueToRangedValue(state, DofApertureMax, DofApertureMin);
        }
    }

    private void OnVignetteSliderChanged(ISleekSlider slider, float state)
    {
        if (_isRefreshing)
        {
            return;
        }

        EnsureSmoothCameraBehaviour();
        if (_smoothCameraBehaviour != null)
        {
            _smoothCameraBehaviour.VignetteIntensity = SliderValueToRangedValue(state, 0f, VignetteMax);
        }
    }

    private void OnExposureSliderChanged(ISleekSlider slider, float state)
    {
        if (_isRefreshing)
        {
            return;
        }

        EnsureSmoothCameraBehaviour();
        if (_smoothCameraBehaviour != null)
        {
            _smoothCameraBehaviour.Exposure = SliderValueToRangedValue(state, ExposureMin, ExposureMax);
        }
    }

    private void OnContrastSliderChanged(ISleekSlider slider, float state)
    {
        if (_isRefreshing)
        {
            return;
        }

        EnsureSmoothCameraBehaviour();
        if (_smoothCameraBehaviour != null)
        {
            _smoothCameraBehaviour.Contrast = SliderValueToRangedValue(state, GradeMin, GradeMax);
        }
    }

    private void OnSaturationSliderChanged(ISleekSlider slider, float state)
    {
        if (_isRefreshing)
        {
            return;
        }

        EnsureSmoothCameraBehaviour();
        if (_smoothCameraBehaviour != null)
        {
            _smoothCameraBehaviour.Saturation = SliderValueToRangedValue(state, GradeMin, GradeMax);
        }
    }

    private void OnTemperatureSliderChanged(ISleekSlider slider, float state)
    {
        if (_isRefreshing)
        {
            return;
        }

        EnsureSmoothCameraBehaviour();
        if (_smoothCameraBehaviour != null)
        {
            _smoothCameraBehaviour.Temperature = SliderValueToRangedValue(state, GradeMin, GradeMax);
        }
    }

    private void OnTintSliderChanged(ISleekSlider slider, float state)
    {
        if (_isRefreshing)
        {
            return;
        }

        EnsureSmoothCameraBehaviour();
        if (_smoothCameraBehaviour != null)
        {
            _smoothCameraBehaviour.Tint = SliderValueToRangedValue(state, GradeMin, GradeMax);
        }
    }

    private void OnClickedResetVisualsButton(ISleekElement button)
    {
        EnsureSmoothCameraBehaviour();
        _smoothCameraBehaviour?.ResetVisualOverrides();
        RefreshAllControls();
    }

    private void OnSmoothCameraToggleChanged(ISleekToggle toggle, bool value)
    {
        if (_isRefreshing)
        {
            return;
        }

        EnsureSmoothCameraBehaviour();
        if (_smoothCameraBehaviour != null)
        {
            _smoothCameraBehaviour.IsSmoothingEnabled = value;
        }
    }

    private void OnSmoothnessSliderChanged(ISleekSlider slider, float state)
    {
        if (_isRefreshing)
        {
            return;
        }

        EnsureSmoothCameraBehaviour();
        if (_smoothCameraBehaviour != null)
        {
            _smoothCameraBehaviour.Smoothness = SliderValueToRangedValue(state, SmoothnessMax, SmoothnessMin);
        }
    }

    private void OnWindToggleChanged(ISleekToggle toggle, bool value)
    {
        if (_isRefreshing)
        {
            return;
        }

        GraphicsSettings.IsWindEnabled = value;
        GraphicsSettings.apply("EditorHelper2 cinematic wind toggle");
    }

    private void OnFilmGrainToggleChanged(ISleekToggle toggle, bool value)
    {
        if (_isRefreshing)
        {
            return;
        }

        GraphicsSettings.filmGrain = value;
        GraphicsSettings.apply("EditorHelper2 cinematic film grain toggle");
    }

    private void OnWatermarkToggleChanged(ISleekToggle toggle, bool value)
    {
        if (_isRefreshing)
        {
            return;
        }

        _isWatermarkEnabled = value;
        RefreshWatermarkVisibility();
    }

    private void OnClickedWatermarkSettingsButton(ISleekElement button)
    {
        if (_watermarkSettingsPanel != null)
        {
            _watermarkSettingsPanel.IsVisible = !_watermarkSettingsPanel.IsVisible;
        }
    }

    private void OnWatermarkTextChanged(ISleekField field, string text)
    {
        if (_isRefreshing)
        {
            return;
        }

        _watermarkText = string.IsNullOrWhiteSpace(text) ? "EditorHelper2" : text;
        ApplyWatermarkSettings();
    }

    private void OnWatermarkOpacityChanged(ISleekSlider slider, float state)
    {
        if (_isRefreshing)
        {
            return;
        }

        _watermarkOpacity = Mathf.Clamp01(state);
        ApplyWatermarkSettings();
    }

    private void OnWatermarkXChanged(ISleekSlider slider, float state)
    {
        if (_isRefreshing)
        {
            return;
        }

        _watermarkPositionX = Mathf.Clamp01(state);
        ApplyWatermarkSettings();
    }

    private void OnWatermarkYChanged(ISleekSlider slider, float state)
    {
        if (_isRefreshing)
        {
            return;
        }

        _watermarkPositionY = Mathf.Clamp01(state);
        ApplyWatermarkSettings();
    }

    private void OnWatermarkRotationChanged(ISleekSlider slider, float state)
    {
        if (_isRefreshing)
        {
            return;
        }

        _watermarkRotation = SliderValueToRangedValue(state, WatermarkRotationMin, WatermarkRotationMax);
        ApplyWatermarkSettings();
    }

    private void OnWatermarkScaleChanged(ISleekSlider slider, float state)
    {
        if (_isRefreshing)
        {
            return;
        }

        _watermarkScale = SliderValueToRangedValue(state, WatermarkScaleMin, WatermarkScaleMax);
        ApplyWatermarkSettings();
    }

    private void OnClickedResetWatermarkButton(ISleekElement button)
    {
        _isWatermarkEnabled = true;
        _watermarkText = "EditorHelper2";
        _watermarkOpacity = 0.8f;
        _watermarkPositionX = 0.9f;
        _watermarkPositionY = 0.92f;
        _watermarkRotation = 0f;
        _watermarkScale = 1f;
        RefreshAllControls();
    }

    private void OnSunShaftsQualityChanged(SleekButtonState button, int index)
    {
        if (_isRefreshing)
        {
            return;
        }

        GraphicsSettings.sunShaftsQuality = CinematicModeUtility.GraphicQualityStates[Mathf.Clamp(index, 0, CinematicModeUtility.GraphicQualityStates.Length - 1)];
        GraphicsSettings.apply("EditorHelper2 cinematic sun shafts quality");
    }

    private void OnLightingQualityChanged(SleekButtonState button, int index)
    {
        if (_isRefreshing)
        {
            return;
        }

        GraphicsSettings.lightingQuality = CinematicModeUtility.GraphicQualityStates[Mathf.Clamp(index, 0, CinematicModeUtility.GraphicQualityStates.Length - 1)];
        GraphicsSettings.apply("EditorHelper2 cinematic lighting quality");
    }

    private void OnWaterQualityChanged(SleekButtonState button, int index)
    {
        if (_isRefreshing)
        {
            return;
        }

        GraphicsSettings.waterQuality = CinematicModeUtility.WaterQualityStates[Mathf.Clamp(index, 0, CinematicModeUtility.WaterQualityStates.Length - 1)];
        GraphicsSettings.apply("EditorHelper2 cinematic water quality");
    }

    private void EnsureSmoothCameraBehaviour()
    {
        if (_smoothCameraBehaviour != null)
        {
            return;
        }

        _smoothCameraObject = new GameObject("EditorHelper2 Cinematic Camera Smoothing");
        Object.DontDestroyOnLoad(_smoothCameraObject);
        _smoothCameraBehaviour = _smoothCameraObject.AddComponent<CinematicCameraSmoothingBehaviour>();
    }

    private void RefreshWatermarkVisibility()
    {
        if (_watermarkRenderer == null)
        {
            return;
        }

        _watermarkRenderer.IsVisible = _isWatermarkEnabled && (_active || GraphicsSettings.WantsCinematicMode);
    }

    private void ApplyWatermarkSettings()
    {
        if (_watermarkRenderer == null)
        {
            return;
        }

        _watermarkRenderer.Text = string.IsNullOrWhiteSpace(_watermarkText) ? "EditorHelper2" : _watermarkText;
        _watermarkRenderer.Opacity = Mathf.Clamp01(_watermarkOpacity);
        _watermarkRenderer.Position = new Vector2(Mathf.Clamp01(_watermarkPositionX), Mathf.Clamp01(_watermarkPositionY));
        _watermarkRenderer.RotationDegrees = Mathf.Clamp(_watermarkRotation, WatermarkRotationMin, WatermarkRotationMax);
        _watermarkRenderer.Scale = Mathf.Clamp(_watermarkScale, WatermarkScaleMin, WatermarkScaleMax);
    }

    private static void SetWeatherMode(CinematicWeatherMode mode)
    {
        if (mode == CinematicWeatherMode.None)
        {
            LightingManager.DisableWeather();
            LevelLighting.rainyness = ELightingRain.NONE;
            LevelLighting.snowyness = ELightingSnow.NONE;
            LevelLighting.MarkParticleCloudsNeedRestart();
            return;
        }

        WeatherAssetBase? asset = CinematicModeUtility.FindWeatherAsset(mode);
        if (asset == null)
        {
            UnturnedLog.warn("[EditorHelper2] Unable to activate cinematic weather because the {0} asset was not found.", mode);
            return;
        }

        LightingManager.ActivatePerpetualWeather(asset);
        if (mode == CinematicWeatherMode.Snow)
        {
            LevelLighting.rainyness = ELightingRain.NONE;
            LevelLighting.snowyness = ELightingSnow.BLIZZARD;
        }
        else
        {
            LevelLighting.rainyness = ELightingRain.DRIZZLE;
            LevelLighting.snowyness = ELightingSnow.NONE;
        }

        LevelLighting.MarkParticleCloudsNeedRestart();
    }

    private static float SliderValueToRangedValue(float value, float min, float max)
    {
        return Mathf.Lerp(min, max, Mathf.Clamp01(value));
    }

    private static float RangedValueToSliderValue(float value, float min, float max)
    {
        return Mathf.InverseLerp(min, max, Mathf.Clamp(value, min, max));
    }

    public void Dispose()
    {
        MapEditorConfigHelper.UnregisterExtensionSettings(ConfigSection);
        CloseCinematicTab();

        if (_openCinematicButton != null)
        {
            _openCinematicButton.OnClicked -= OnClickedOpenCinematicButton;
            _pauseContainer?.RemoveChild(_openCinematicButton);
            _openCinematicButton = null;
        }

        if (_backButton != null)
        {
            _backButton.OnClicked -= OnClickedBackButton;
            _backButton = null;
        }

        if (_cinematicToggle != null)
        {
            _cinematicToggle.OnValueChanged -= OnCinematicToggleChanged;
        }

        if (_weatherButton != null)
        {
            _weatherButton.onSwappedState -= OnWeatherStateChanged;
        }

        if (_moonSlider != null)
        {
            _moonSlider.OnValueChanged -= OnMoonSliderChanged;
        }

        if (_timeSlider != null)
        {
            _timeSlider.OnValueChanged -= OnTimeSliderChanged;
        }

        if (_snowLevelSlider != null)
        {
            _snowLevelSlider.onValued -= OnSnowLevelChanged;
        }

        if (_seaLevelSlider != null)
        {
            _seaLevelSlider.onValued -= OnSeaLevelChanged;
        }

        if (_fovSlider != null)
        {
            _fovSlider.OnValueChanged -= OnFovSliderChanged;
        }

        if (_rollSlider != null)
        {
            _rollSlider.OnValueChanged -= OnRollSliderChanged;
        }

        if (_dofToggle != null)
        {
            _dofToggle.OnValueChanged -= OnDepthOfFieldToggleChanged;
        }

        if (_dofFocusSlider != null)
        {
            _dofFocusSlider.OnValueChanged -= OnDepthOfFieldFocusSliderChanged;
        }

        if (_dofStrengthSlider != null)
        {
            _dofStrengthSlider.OnValueChanged -= OnDepthOfFieldStrengthSliderChanged;
        }

        if (_vignetteSlider != null)
        {
            _vignetteSlider.OnValueChanged -= OnVignetteSliderChanged;
        }

        if (_exposureSlider != null)
        {
            _exposureSlider.OnValueChanged -= OnExposureSliderChanged;
        }

        if (_contrastSlider != null)
        {
            _contrastSlider.OnValueChanged -= OnContrastSliderChanged;
        }

        if (_saturationSlider != null)
        {
            _saturationSlider.OnValueChanged -= OnSaturationSliderChanged;
        }

        if (_temperatureSlider != null)
        {
            _temperatureSlider.OnValueChanged -= OnTemperatureSliderChanged;
        }

        if (_tintSlider != null)
        {
            _tintSlider.OnValueChanged -= OnTintSliderChanged;
        }

        if (_resetVisualsButton != null)
        {
            _resetVisualsButton.OnClicked -= OnClickedResetVisualsButton;
        }

        if (_smoothCameraToggle != null)
        {
            _smoothCameraToggle.OnValueChanged -= OnSmoothCameraToggleChanged;
        }

        if (_smoothnessSlider != null)
        {
            _smoothnessSlider.OnValueChanged -= OnSmoothnessSliderChanged;
        }

        if (_windToggle != null)
        {
            _windToggle.OnValueChanged -= OnWindToggleChanged;
        }

        if (_filmGrainToggle != null)
        {
            _filmGrainToggle.OnValueChanged -= OnFilmGrainToggleChanged;
        }

        if (_watermarkToggle != null)
        {
            _watermarkToggle.OnValueChanged -= OnWatermarkToggleChanged;
        }

        if (_watermarkSettingsButton != null)
        {
            _watermarkSettingsButton.OnClicked -= OnClickedWatermarkSettingsButton;
        }

        if (_watermarkTextField != null)
        {
            _watermarkTextField.OnTextChanged -= OnWatermarkTextChanged;
        }

        if (_watermarkOpacitySlider != null)
        {
            _watermarkOpacitySlider.OnValueChanged -= OnWatermarkOpacityChanged;
        }

        if (_watermarkXSlider != null)
        {
            _watermarkXSlider.OnValueChanged -= OnWatermarkXChanged;
        }

        if (_watermarkYSlider != null)
        {
            _watermarkYSlider.OnValueChanged -= OnWatermarkYChanged;
        }

        if (_watermarkRotationSlider != null)
        {
            _watermarkRotationSlider.OnValueChanged -= OnWatermarkRotationChanged;
        }

        if (_watermarkScaleSlider != null)
        {
            _watermarkScaleSlider.OnValueChanged -= OnWatermarkScaleChanged;
        }

        if (_resetWatermarkButton != null)
        {
            _resetWatermarkButton.OnClicked -= OnClickedResetWatermarkButton;
        }

        if (_sunShaftsButton != null)
        {
            _sunShaftsButton.onSwappedState -= OnSunShaftsQualityChanged;
        }

        if (_lightingButton != null)
        {
            _lightingButton.onSwappedState -= OnLightingQualityChanged;
        }

        if (_waterButton != null)
        {
            _waterButton.onSwappedState -= OnWaterQualityChanged;
        }

        if (_cinematicContainer != null)
        {
            EditorUI.window.RemoveChild(_cinematicContainer);
            _cinematicContainer = null;
        }

        if (_smoothCameraObject != null)
        {
            _smoothCameraBehaviour?.ResetVisualOverrides();
            Object.Destroy(_smoothCameraObject);
            _smoothCameraObject = null;
            _smoothCameraBehaviour = null;
        }

        if (_watermarkObject != null)
        {
            Object.Destroy(_watermarkObject);
            _watermarkObject = null;
            _watermarkRenderer = null;
        }

        if (_instance == this)
        {
            _instance = null;
        }
    }

    private Settings CaptureSettings()
    {
        EnsureSmoothCameraBehaviour();
        return new Settings
        {
            WeatherMode = (int)CinematicModeUtility.GetCurrentWeatherMode(),
            FieldOfView = _smoothCameraBehaviour?.TargetFov ?? OptionsSettings.DesiredVerticalFieldOfView,
            Roll = _smoothCameraBehaviour?.RollDegrees ?? 0f,
            DepthOfFieldEnabled = _smoothCameraBehaviour?.IsDepthOfFieldEnabled == true,
            DepthOfFieldFocusDistance = _smoothCameraBehaviour?.DepthOfFieldFocusDistance ?? 25f,
            DepthOfFieldAperture = _smoothCameraBehaviour?.DepthOfFieldAperture ?? 5.6f,
            Vignette = _smoothCameraBehaviour?.VignetteIntensity ?? 0f,
            Exposure = _smoothCameraBehaviour?.Exposure ?? 0f,
            Contrast = _smoothCameraBehaviour?.Contrast ?? 0f,
            Saturation = _smoothCameraBehaviour?.Saturation ?? 0f,
            Temperature = _smoothCameraBehaviour?.Temperature ?? 0f,
            Tint = _smoothCameraBehaviour?.Tint ?? 0f,
            CameraSmoothingEnabled = _smoothCameraBehaviour?.IsSmoothingEnabled == true,
            CameraSmoothness = _smoothCameraBehaviour?.Smoothness ?? 8f,
            WatermarkEnabled = _isWatermarkEnabled,
            WatermarkText = _watermarkText,
            WatermarkOpacity = _watermarkOpacity,
            WatermarkPositionX = _watermarkPositionX,
            WatermarkPositionY = _watermarkPositionY,
            WatermarkRotation = _watermarkRotation,
            WatermarkScale = _watermarkScale
        };
    }

    private void ApplySettings(Settings settings)
    {
        SetWeatherMode((CinematicWeatherMode)Mathf.Clamp(settings.WeatherMode, 0, 3));
        EnsureSmoothCameraBehaviour();
        if (_smoothCameraBehaviour != null)
        {
            _smoothCameraBehaviour.TargetFov = Mathf.Clamp(settings.FieldOfView, FovMin, FovMax);
            _smoothCameraBehaviour.RollDegrees = Mathf.Clamp(settings.Roll, RollMin, RollMax);
            _smoothCameraBehaviour.IsDepthOfFieldEnabled = settings.DepthOfFieldEnabled;
            _smoothCameraBehaviour.DepthOfFieldFocusDistance = Mathf.Clamp(settings.DepthOfFieldFocusDistance, DofFocusMin, DofFocusMax);
            _smoothCameraBehaviour.DepthOfFieldAperture = Mathf.Clamp(settings.DepthOfFieldAperture, DofApertureMin, DofApertureMax);
            _smoothCameraBehaviour.VignetteIntensity = Mathf.Clamp(settings.Vignette, 0f, VignetteMax);
            _smoothCameraBehaviour.Exposure = Mathf.Clamp(settings.Exposure, ExposureMin, ExposureMax);
            _smoothCameraBehaviour.Contrast = Mathf.Clamp(settings.Contrast, GradeMin, GradeMax);
            _smoothCameraBehaviour.Saturation = Mathf.Clamp(settings.Saturation, GradeMin, GradeMax);
            _smoothCameraBehaviour.Temperature = Mathf.Clamp(settings.Temperature, GradeMin, GradeMax);
            _smoothCameraBehaviour.Tint = Mathf.Clamp(settings.Tint, GradeMin, GradeMax);
            _smoothCameraBehaviour.IsSmoothingEnabled = settings.CameraSmoothingEnabled;
            _smoothCameraBehaviour.Smoothness = Mathf.Clamp(settings.CameraSmoothness, SmoothnessMin, SmoothnessMax);
        }

        _isWatermarkEnabled = settings.WatermarkEnabled;
        _watermarkText = string.IsNullOrWhiteSpace(settings.WatermarkText) ? "EditorHelper2" : settings.WatermarkText;
        _watermarkOpacity = Mathf.Clamp01(settings.WatermarkOpacity);
        _watermarkPositionX = Mathf.Clamp01(settings.WatermarkPositionX);
        _watermarkPositionY = Mathf.Clamp01(settings.WatermarkPositionY);
        _watermarkRotation = Mathf.Clamp(settings.WatermarkRotation, WatermarkRotationMin, WatermarkRotationMax);
        _watermarkScale = Mathf.Clamp(settings.WatermarkScale, WatermarkScaleMin, WatermarkScaleMax);
        RefreshAllControls();
    }

    private sealed class Settings
    {
        public int WeatherMode { get; set; }
        public float FieldOfView { get; set; } = 60f;
        public float Roll { get; set; }
        public bool DepthOfFieldEnabled { get; set; }
        public float DepthOfFieldFocusDistance { get; set; } = 25f;
        public float DepthOfFieldAperture { get; set; } = 5.6f;
        public float Vignette { get; set; }
        public float Exposure { get; set; }
        public float Contrast { get; set; }
        public float Saturation { get; set; }
        public float Temperature { get; set; }
        public float Tint { get; set; }
        public bool CameraSmoothingEnabled { get; set; }
        public float CameraSmoothness { get; set; } = 8f;
        public bool WatermarkEnabled { get; set; } = true;
        public string WatermarkText { get; set; } = "EditorHelper2";
        public float WatermarkOpacity { get; set; } = 0.8f;
        public float WatermarkPositionX { get; set; } = 0.9f;
        public float WatermarkPositionY { get; set; } = 0.92f;
        public float WatermarkRotation { get; set; }
        public float WatermarkScale { get; set; } = 1f;
    }

    private sealed class CinematicCameraSmoothingBehaviour : MonoBehaviour
    {
        private Vector3 _smoothedPosition;
        private Quaternion _smoothedParentRotation;
        private Quaternion _smoothedCameraLocalRotation;
        private Camera? _highlightCamera;
        private bool _hasState;
        private bool _isSmoothingEnabled;
        private readonly CinematicPostProcessOverrides _postProcessOverrides = new();

        public bool IsSmoothingEnabled
        {
            get => _isSmoothingEnabled;
            set
            {
                if (_isSmoothingEnabled == value)
                {
                    return;
                }

                _isSmoothingEnabled = value;
                _hasState = false;
                if (!_isSmoothingEnabled)
                {
                    CollapseCameraChildOffset();
                }
            }
        }

        public float Smoothness { get; set; } = 8f;

        public float TargetFov { get; set; } = OptionsSettings.DesiredVerticalFieldOfView;

        public float RollDegrees { get; set; }

        public bool IsDepthOfFieldEnabled { get; set; }

        public float DepthOfFieldFocusDistance { get; set; } = 25f;

        public float DepthOfFieldAperture { get; set; } = 5.6f;

        public float VignetteIntensity { get; set; }

        public float Exposure { get; set; }

        public float Contrast { get; set; }

        public float Saturation { get; set; }

        public float Temperature { get; set; }

        public float Tint { get; set; }

        private bool HasPostProcessOverrides => IsDepthOfFieldEnabled
            || Mathf.Abs(VignetteIntensity) > 0.001f
            || Mathf.Abs(Exposure) > 0.001f
            || Mathf.Abs(Contrast) > 0.001f
            || Mathf.Abs(Saturation) > 0.001f
            || Mathf.Abs(Temperature) > 0.001f
            || Mathf.Abs(Tint) > 0.001f;

        public void ResetVisualOverrides()
        {
            TargetFov = OptionsSettings.DesiredVerticalFieldOfView;
            RollDegrees = 0f;
            IsDepthOfFieldEnabled = false;
            DepthOfFieldFocusDistance = 25f;
            DepthOfFieldAperture = 5.6f;
            VignetteIntensity = 0f;
            Exposure = 0f;
            Contrast = 0f;
            Saturation = 0f;
            Temperature = 0f;
            Tint = 0f;
            _postProcessOverrides.Reset();
        }

        private void LateUpdate()
        {
            if (MainCamera.instance == null || !SDG.Unturned.Level.isEditor)
            {
                _hasState = false;
                return;
            }

            Transform cameraTransform = MainCamera.instance.transform;
            Transform parentTransform = cameraTransform.parent;
            if (parentTransform == null)
            {
                _hasState = false;
                return;
            }

            if (IsSmoothingEnabled)
            {
                ApplySmoothing(cameraTransform, parentTransform);
            }
            else
            {
                _hasState = false;
            }

            ApplyCameraOverrides(cameraTransform);
            if (HasPostProcessOverrides)
            {
                _postProcessOverrides.Apply(
                    IsDepthOfFieldEnabled,
                    DepthOfFieldFocusDistance,
                    DepthOfFieldAperture,
                    VignetteIntensity,
                    Exposure,
                    Contrast,
                    Saturation,
                    Temperature,
                    Tint);
            }
            else
            {
                _postProcessOverrides.Reset();
            }
        }

        private void ApplySmoothing(Transform cameraTransform, Transform parentTransform)
        {
            if (cameraTransform.localPosition != Vector3.zero)
            {
                CollapseCameraChildOffset(cameraTransform, parentTransform);
            }

            if (!_hasState)
            {
                _smoothedPosition = parentTransform.position;
                _smoothedParentRotation = parentTransform.rotation;
                _smoothedCameraLocalRotation = cameraTransform.localRotation;
                _hasState = true;
                return;
            }

            float amount = 1f - Mathf.Exp(-Mathf.Max(0.01f, Smoothness) * Time.deltaTime);
            _smoothedPosition = Vector3.Lerp(_smoothedPosition, parentTransform.position, amount);
            _smoothedParentRotation = Quaternion.Slerp(_smoothedParentRotation, parentTransform.rotation, amount);
            _smoothedCameraLocalRotation = Quaternion.Slerp(_smoothedCameraLocalRotation, cameraTransform.localRotation, amount);

            parentTransform.SetPositionAndRotation(_smoothedPosition, _smoothedParentRotation);
            cameraTransform.localPosition = Vector3.zero;
            cameraTransform.localRotation = _smoothedCameraLocalRotation;
        }

        private void ApplyCameraOverrides(Transform cameraTransform)
        {
            float fov = Mathf.Clamp(TargetFov, FovMin, FovMax);
            MainCamera.instance.fieldOfView = fov;

            _highlightCamera ??= cameraTransform.Find("HighlightCamera")?.GetComponent<Camera>();
            if (_highlightCamera != null)
            {
                _highlightCamera.fieldOfView = fov;
            }

            float roll = Mathf.Clamp(RollDegrees, RollMin, RollMax);
            float pitch = cameraTransform.localEulerAngles.x;
            if (pitch > 180f)
            {
                pitch -= 360f;
            }

            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, roll);
        }

        private static void CollapseCameraChildOffset()
        {
            if (MainCamera.instance == null)
            {
                return;
            }

            Transform cameraTransform = MainCamera.instance.transform;
            Transform parentTransform = cameraTransform.parent;
            if (parentTransform == null)
            {
                return;
            }

            CollapseCameraChildOffset(cameraTransform, parentTransform);
        }

        private static void CollapseCameraChildOffset(Transform cameraTransform, Transform parentTransform)
        {
            parentTransform.position = cameraTransform.position;
            parentTransform.rotation = Quaternion.Euler(0f, EditorLook.yaw, 0f);
            cameraTransform.localPosition = Vector3.zero;
            cameraTransform.localRotation = Quaternion.Euler(EditorLook.pitch, 0f, 0f);
        }
    }

    private sealed class CinematicWatermarkRenderer : MonoBehaviour
    {
        private static readonly Vector2[] OutlineOffsets =
        [
            new(-1f, 0f),
            new(1f, 0f),
            new(0f, -1f),
            new(0f, 1f),
            new(-1f, -1f),
            new(1f, -1f),
            new(-1f, 1f),
            new(1f, 1f)
        ];

        private GUIStyle? _style;
        private GUIContent? _content;

        public bool IsVisible { get; set; }

        public string Text { get; set; } = "EditorHelper2";

        public float Opacity { get; set; } = 0.8f;

        public Vector2 Position { get; set; } = new(0.9f, 0.92f);

        public float RotationDegrees { get; set; }

        public float Scale { get; set; } = 1f;

        private void OnGUI()
        {
            if (!IsVisible || string.IsNullOrWhiteSpace(Text))
            {
                return;
            }

            _style ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            _content ??= new GUIContent();
            _content.text = Text;

            _style.fontSize = Mathf.RoundToInt(22f * Mathf.Clamp(Scale, WatermarkScaleMin, WatermarkScaleMax));
            Vector2 textSize = _style.CalcSize(_content);
            Rect rect = new(
                Mathf.Lerp(0f, Screen.width, Mathf.Clamp01(Position.x)) - textSize.x * 0.5f,
                Mathf.Lerp(0f, Screen.height, Mathf.Clamp01(Position.y)) - textSize.y * 0.5f,
                textSize.x,
                textSize.y);

            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUIUtility.RotateAroundPivot(RotationDegrees, rect.center);

            float opacity = Mathf.Clamp01(Opacity);
            _style.normal.textColor = Color.black;
            GUI.color = new Color(1f, 1f, 1f, opacity * 0.6f);
            foreach (Vector2 offset in OutlineOffsets)
            {
                Rect outlineRect = rect;
                outlineRect.position += offset;
                GUI.Label(outlineRect, _content, _style);
            }

            _style.normal.textColor = Color.white;
            GUI.color = new Color(1f, 1f, 1f, opacity);
            GUI.Label(rect, _content, _style);

            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }
    }

}
