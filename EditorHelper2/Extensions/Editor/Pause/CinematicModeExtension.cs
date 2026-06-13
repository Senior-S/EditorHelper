using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using SDG.Unturned;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorHelper2.Extensions.Editor.Pause;

[UIExtension(typeof(EditorPauseUI))]
[EHExtension("Cinematic Mode Tab", "Senior S", alwaysEnabled: true)]
public class CinematicModeExtension : UIExtension, IExtension
{
    private static CinematicModeExtension? _instance;

    private enum CinematicWeatherMode
    {
        None,
        NormalRain,
        Thunder,
        Snow
    }

    private const string HeavyRainWeatherGuid = "6c850687bdb947a689fa8de8a8d99afb";
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

    private static readonly FieldInfo? CinematicModeFlagField = typeof(GraphicsSettings)
        .GetField("clEnableCinematicMode", BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly FieldInfo? RoadRegionSegmentRenderersField = typeof(LevelRoads)
        .GetField("regionSegmentRenderers", BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly MethodInfo? ResourceSpawnpointUpdateActiveMethod = typeof(ResourceSpawnpoint)
        .GetMethod("UpdateActive", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly AssetReference<WeatherAssetBase> HeavyRainWeatherReference = new(HeavyRainWeatherGuid);

    private static readonly EGraphicQuality[] GraphicQualityStates =
    [
        EGraphicQuality.OFF,
        EGraphicQuality.LOW,
        EGraphicQuality.MEDIUM,
        EGraphicQuality.HIGH,
        EGraphicQuality.ULTRA
    ];

    private static readonly EGraphicQuality[] WaterQualityStates =
    [
        EGraphicQuality.LOW,
        EGraphicQuality.MEDIUM,
        EGraphicQuality.HIGH,
        EGraphicQuality.ULTRA
    ];

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

        BuildCinematicContainer();
        AddPauseMenuButton();
        RefreshAllControls();
    }

    private void AddPauseMenuButton()
    {
        _openCinematicButton = Glazier.Get().CreateButton();
        _openCinematicButton.PositionOffset_X = 110f;
        _openCinematicButton.PositionOffset_Y = 245f;
        _openCinematicButton.PositionScale_X = 0.5f;
        _openCinematicButton.PositionScale_Y = 0.5f;
        _openCinematicButton.SizeOffset_X = 200f;
        _openCinematicButton.SizeOffset_Y = 30f;
        _openCinematicButton.Text = "Cinematic";
        _openCinematicButton.TooltipText = "Open cinematic controls for screenshots and flythroughs.";
        _openCinematicButton.TextColor = ESleekTint.FONT;
        _openCinematicButton.OnClicked += OnClickedOpenCinematicButton;
        _pauseContainer!.AddChild(_openCinematicButton);
    }

    protected override void Opened()
    {
        RefreshAllControls();
    }

    private void BuildCinematicContainer()
    {
        _cinematicContainer = new SleekFullscreenBox
        {
            PositionOffset_X = 10f,
            PositionOffset_Y = 10f,
            PositionScale_X = 1f,
            SizeOffset_X = -20f,
            SizeOffset_Y = -20f,
            SizeScale_X = 1f,
            SizeScale_Y = 1f
        };
        EditorUI.window.AddChild(_cinematicContainer);

        AddFovSlider(90f);
        AddRollSlider(125f);
        AddDepthOfFieldToggle(165f);
        AddDepthOfFieldFocusSlider(205f);
        AddDepthOfFieldStrengthSlider(245f);
        AddVignetteSlider(285f);
        AddExposureSlider(325f);
        AddContrastSlider(365f);
        AddSaturationSlider(405f);
        AddTemperatureSlider(445f);
        AddTintSlider(485f);
        AddResetVisualsButton(525f);
        AddCinematicToggle(90f);
        AddWeatherButton(130f);
        AddMoonSlider(170f);
        AddTimeSlider(205f);
        AddSnowLevelSlider(240f);
        AddSeaLevelSlider(280f);
        AddSmoothCameraToggle(325f);
        AddSmoothnessSlider(365f);
        AddWindToggle(405f);
        AddFilmGrainToggle(445f);
        AddSunShaftsButton(485f);
        AddLightingButton(525f);
        AddWaterButton(565f);
        AddWatermarkToggle(565f);
        AddWatermarkSettingsButton(565f);
        AddBackButton();
        AddWatermarkSettingsPanel();
        AddWatermarkRenderer();
    }

    private void AddCinematicToggle(float y)
    {
        AddRowLabel("Cinematic", y, "Toggles Unturned's -Cinematic mode. It renders much more of the map and can reduce performance heavily.");
        _cinematicToggle = CreateToggle(y, "Render the whole map for screenshots and flythroughs.");
        _cinematicToggle.OnValueChanged += OnCinematicToggleChanged;
    }

    private void AddFovSlider(float y)
    {
        AddRowLabel("Camera FOV", y, "Overrides the editor camera field of view for wider or tighter cinematic shots.", useLeftColumn: true);
        _fovSlider = CreateSlider(y, useLeftColumn: true);
        _fovSlider.OnValueChanged += OnFovSliderChanged;
    }

    private void AddRollSlider(float y)
    {
        AddRowLabel("Camera Roll", y, "Tilts the camera around its forward axis. Reset to center for normal editor rotation.", useLeftColumn: true);
        _rollSlider = CreateSlider(y, useLeftColumn: true);
        _rollSlider.OnValueChanged += OnRollSliderChanged;
    }

    private void AddDepthOfFieldToggle(float y)
    {
        AddRowLabel("Depth of Field", y, "Enables a cinematic focus blur on the main camera.", useLeftColumn: true);
        _dofToggle = CreateToggle(y, "Toggle depth of field.", useLeftColumn: true);
        _dofToggle.OnValueChanged += OnDepthOfFieldToggleChanged;
    }

    private void AddDepthOfFieldFocusSlider(float y)
    {
        AddRowLabel("DOF Focus", y, "Distance from the camera that remains sharp while depth of field is enabled.", useLeftColumn: true);
        _dofFocusSlider = CreateSlider(y, useLeftColumn: true);
        _dofFocusSlider.OnValueChanged += OnDepthOfFieldFocusSliderChanged;
    }

    private void AddDepthOfFieldStrengthSlider(float y)
    {
        AddRowLabel("DOF Strength", y, "Higher values increase the blur amount by lowering aperture.", useLeftColumn: true);
        _dofStrengthSlider = CreateSlider(y, useLeftColumn: true);
        _dofStrengthSlider.OnValueChanged += OnDepthOfFieldStrengthSliderChanged;
    }

    private void AddVignetteSlider(float y)
    {
        AddRowLabel("Vignette", y, "Darkens the image edges for cinematic framing.", useLeftColumn: true);
        _vignetteSlider = CreateSlider(y, useLeftColumn: true);
        _vignetteSlider.OnValueChanged += OnVignetteSliderChanged;
    }

    private void AddExposureSlider(float y)
    {
        AddRowLabel("Exposure", y, "Adjusts post-process exposure. Center is neutral.", useLeftColumn: true);
        _exposureSlider = CreateSlider(y, useLeftColumn: true);
        _exposureSlider.OnValueChanged += OnExposureSliderChanged;
    }

    private void AddContrastSlider(float y)
    {
        AddRowLabel("Contrast", y, "Adjusts color grading contrast. Center is neutral.", useLeftColumn: true);
        _contrastSlider = CreateSlider(y, useLeftColumn: true);
        _contrastSlider.OnValueChanged += OnContrastSliderChanged;
    }

    private void AddSaturationSlider(float y)
    {
        AddRowLabel("Saturation", y, "Adjusts color grading saturation. Center is neutral.", useLeftColumn: true);
        _saturationSlider = CreateSlider(y, useLeftColumn: true);
        _saturationSlider.OnValueChanged += OnSaturationSliderChanged;
    }

    private void AddTemperatureSlider(float y)
    {
        AddRowLabel("Temperature", y, "Warms or cools the color grade. Center is neutral.", useLeftColumn: true);
        _temperatureSlider = CreateSlider(y, useLeftColumn: true);
        _temperatureSlider.OnValueChanged += OnTemperatureSliderChanged;
    }

    private void AddTintSlider(float y)
    {
        AddRowLabel("Tint", y, "Shifts the color grade toward green or magenta. Center is neutral.", useLeftColumn: true);
        _tintSlider = CreateSlider(y, useLeftColumn: true);
        _tintSlider.OnValueChanged += OnTintSliderChanged;
    }

    private void AddResetVisualsButton(float y)
    {
        _resetVisualsButton = Glazier.Get().CreateButton();
        _resetVisualsButton.PositionOffset_X = LeftControlX;
        _resetVisualsButton.PositionOffset_Y = y;
        _resetVisualsButton.PositionScale_X = 0f;
        _resetVisualsButton.SizeOffset_X = ControlWidth;
        _resetVisualsButton.SizeOffset_Y = 30f;
        _resetVisualsButton.Text = "Reset Visuals";
        _resetVisualsButton.TooltipText = "Reset camera FOV, roll, depth of field, vignette, and color grading overrides.";
        _resetVisualsButton.TextColor = ESleekTint.FONT;
        _resetVisualsButton.OnClicked += OnClickedResetVisualsButton;
        _cinematicContainer!.AddChild(_resetVisualsButton);
    }

    private void AddWeatherButton(float y)
    {
        AddRowLabel("Weather", y, "Preview fixed weather for screenshots. Thunder uses Unturned's heavy rain weather with lightning.");
        _weatherButton = CreateButtonState(y,
            new GUIContent("None", "Disable active weather."),
            new GUIContent("Normal Rain", "Use Unturned's default rain weather."),
            new GUIContent("Thunder", "Use the heavy rain weather asset with lightning."),
            new GUIContent("Snow", "Use Unturned's default snow weather."));
        _weatherButton.onSwappedState += OnWeatherStateChanged;
    }

    private void AddMoonSlider(float y)
    {
        AddRowLabel("Moon", y, "Matches the moon slider from Environment/Lighting.");
        _moonSlider = CreateSlider(y);
        _moonSlider.OnValueChanged += OnMoonSliderChanged;
    }

    private void AddTimeSlider(float y)
    {
        AddRowLabel("Time", y, "Matches the time slider from Environment/Lighting.");
        _timeSlider = CreateSlider(y);
        _timeSlider.OnValueChanged += OnTimeSliderChanged;
    }

    private void AddSnowLevelSlider(float y)
    {
        AddRowLabel("Snow Level", y, "Matches the snow level control from Environment/Lighting.");
        _snowLevelSlider = CreateValueSlider(y);
        _snowLevelSlider.onValued += OnSnowLevelChanged;
    }

    private void AddSeaLevelSlider(float y)
    {
        AddRowLabel("Sea Level", y, "Matches the sea level control from Environment/Lighting.");
        _seaLevelSlider = CreateValueSlider(y);
        _seaLevelSlider.onValued += OnSeaLevelChanged;
    }

    private void AddSmoothCameraToggle(float y)
    {
        AddRowLabel("Smooth Camera", y, "Interpolates the editor camera after normal movement for smoother flythrough recording.");
        _smoothCameraToggle = CreateToggle(y, "Smooth editor camera movement.");
        _smoothCameraToggle.OnValueChanged += OnSmoothCameraToggleChanged;
    }

    private void AddSmoothnessSlider(float y)
    {
        AddRowLabel("Smoothness", y, "Higher values follow normal camera movement more tightly. Lower values feel more floaty.");
        _smoothnessSlider = CreateSlider(y);
        _smoothnessSlider.OnValueChanged += OnSmoothnessSliderChanged;
    }

    private void AddWindToggle(float y)
    {
        AddRowLabel("Wind Effects", y, "Toggles the existing graphics wind effects option.");
        _windToggle = CreateToggle(y, "Toggle graphics wind effects.");
        _windToggle.OnValueChanged += OnWindToggleChanged;
    }

    private void AddFilmGrainToggle(float y)
    {
        AddRowLabel("Film Grain", y, "Toggles the existing graphics film grain option.");
        _filmGrainToggle = CreateToggle(y, "Toggle post-process film grain.");
        _filmGrainToggle.OnValueChanged += OnFilmGrainToggleChanged;
    }

    private void AddWatermarkToggle(float y)
    {
        AddRowLabel("Watermark", y, "Shows the configured watermark while using cinematic capture options.", useLeftColumn: true);
        _watermarkToggle = CreateToggle(y, "Toggle the watermark.", useLeftColumn: true);
        _watermarkToggle.OnValueChanged += OnWatermarkToggleChanged;
    }

    private void AddWatermarkSettingsButton(float y)
    {
        _watermarkSettingsButton = Glazier.Get().CreateButton();
        _watermarkSettingsButton.PositionOffset_X = LeftControlX + 50f;
        _watermarkSettingsButton.PositionOffset_Y = y;
        _watermarkSettingsButton.PositionScale_X = 0f;
        _watermarkSettingsButton.SizeOffset_X = 170f;
        _watermarkSettingsButton.SizeOffset_Y = 30f;
        _watermarkSettingsButton.Text = "Settings";
        _watermarkSettingsButton.TooltipText = "Open watermark text, opacity, position, rotation, and scale settings.";
        _watermarkSettingsButton.TextColor = ESleekTint.FONT;
        _watermarkSettingsButton.OnClicked += OnClickedWatermarkSettingsButton;
        _cinematicContainer!.AddChild(_watermarkSettingsButton);
    }

    private void AddSunShaftsButton(float y)
    {
        AddRowLabel("Sun Shafts", y, "Matches the graphics Sun Shafts Quality option.");
        _sunShaftsButton = CreateQualityButton(y, GraphicQualityStates);
        _sunShaftsButton.onSwappedState += OnSunShaftsQualityChanged;
    }

    private void AddLightingButton(float y)
    {
        AddRowLabel("Lighting", y, "Matches the graphics Lighting Quality option.");
        _lightingButton = CreateQualityButton(y, GraphicQualityStates);
        _lightingButton.onSwappedState += OnLightingQualityChanged;
    }

    private void AddWaterButton(float y)
    {
        AddRowLabel("Water", y, "Matches the graphics Water Quality option.");
        _waterButton = CreateQualityButton(y, WaterQualityStates);
        _waterButton.onSwappedState += OnWaterQualityChanged;
    }

    private void AddRowLabel(string text, float y, string tooltip, bool useLeftColumn = false)
    {
        ISleekBox label = Glazier.Get().CreateBox();
        label.PositionOffset_X = useLeftColumn ? LeftLabelX : RightLabelX;
        label.PositionOffset_Y = y;
        label.PositionScale_X = useLeftColumn ? 0f : 1f;
        label.SizeOffset_X = useLeftColumn ? LeftLabelWidth : RightLabelWidth;
        label.SizeOffset_Y = 30f;
        label.Text = text;
        label.TextAlignment = TextAnchor.MiddleCenter;
        label.FontSize = ESleekFontSize.Small;
        label.TextColor = ESleekTint.FONT;
        _cinematicContainer!.AddChild(label);
    }

    private ISleekToggle CreateToggle(float y, string tooltip, bool useLeftColumn = false)
    {
        ISleekToggle toggle = Glazier.Get().CreateToggle();
        toggle.PositionOffset_X = useLeftColumn ? LeftControlX : RightControlX;
        toggle.PositionOffset_Y = y - 5f;
        toggle.PositionScale_X = useLeftColumn ? 0f : 1f;
        toggle.SizeOffset_X = 40f;
        toggle.SizeOffset_Y = 40f;
        toggle.TooltipText = tooltip;
        _cinematicContainer!.AddChild(toggle);
        return toggle;
    }

    private ISleekSlider CreateSlider(float y, bool useLeftColumn = false)
    {
        ISleekSlider slider = Glazier.Get().CreateSlider();
        slider.PositionOffset_X = useLeftColumn ? LeftControlX : RightControlX;
        slider.PositionOffset_Y = y + 5f;
        slider.PositionScale_X = useLeftColumn ? 0f : 1f;
        slider.SizeOffset_X = ControlWidth;
        slider.SizeOffset_Y = 20f;
        slider.Orientation = ESleekOrientation.HORIZONTAL;
        _cinematicContainer!.AddChild(slider);
        return slider;
    }

    private SleekValue CreateValueSlider(float y)
    {
        SleekValue slider = new()
        {
            PositionOffset_X = RightControlX,
            PositionOffset_Y = y,
            PositionScale_X = 1f,
            SizeOffset_X = ControlWidth,
            SizeOffset_Y = 30f
        };
        _cinematicContainer!.AddChild(slider);
        return slider;
    }

    private SleekButtonState CreateButtonState(float y, params GUIContent[] states)
    {
        SleekButtonState button = new(states)
        {
            PositionOffset_X = RightControlX,
            PositionOffset_Y = y,
            PositionScale_X = 1f,
            SizeOffset_X = ControlWidth,
            SizeOffset_Y = 30f,
            UseContentTooltip = true
        };
        _cinematicContainer!.AddChild(button);
        return button;
    }

    private SleekButtonState CreateQualityButton(float y, EGraphicQuality[] qualities)
    {
        GUIContent[] states = new GUIContent[qualities.Length];
        for (int i = 0; i < qualities.Length; i++)
        {
            string label = FormatQualityName(qualities[i]);
            states[i] = new GUIContent(label, $"Set quality to {label}.");
        }

        return CreateButtonState(y, states);
    }

    private void AddBackButton()
    {
        _backButton = Glazier.Get().CreateButton();
        _backButton.PositionOffset_X = -240f;
        _backButton.PositionOffset_Y = -50f;
        _backButton.PositionScale_X = 1f;
        _backButton.PositionScale_Y = 1f;
        _backButton.SizeOffset_X = 200f;
        _backButton.SizeOffset_Y = 30f;
        _backButton.Text = "Back";
        _backButton.TooltipText = "Return to the pause menu.";
        _backButton.TextColor = ESleekTint.FONT;
        _backButton.OnClicked += OnClickedBackButton;
        _cinematicContainer!.AddChild(_backButton);
    }

    private void AddWatermarkSettingsPanel()
    {
        _watermarkSettingsPanel = new SleekFullscreenBox
        {
            PositionOffset_X = -230f,
            PositionOffset_Y = 120f,
            PositionScale_X = 0.5f,
            SizeOffset_X = 460f,
            SizeOffset_Y = 300f
        };
        _watermarkSettingsPanel.IsVisible = false;
        _cinematicContainer!.AddChild(_watermarkSettingsPanel);

        AddWatermarkPanelLabel("Watermark Text", 20f);
        _watermarkTextField = Glazier.Get().CreateStringField();
        _watermarkTextField.PositionOffset_X = 180f;
        _watermarkTextField.PositionOffset_Y = 20f;
        _watermarkTextField.SizeOffset_X = 240f;
        _watermarkTextField.SizeOffset_Y = 30f;
        _watermarkTextField.Text = _watermarkText;
        _watermarkTextField.MaxLength = 64;
        _watermarkTextField.TextColor = ESleekTint.FONT;
        _watermarkTextField.OnTextChanged += OnWatermarkTextChanged;
        _watermarkSettingsPanel.AddChild(_watermarkTextField);

        AddWatermarkPanelLabel("Opacity", 60f);
        _watermarkOpacitySlider = CreateWatermarkPanelSlider(60f);
        _watermarkOpacitySlider.OnValueChanged += OnWatermarkOpacityChanged;

        AddWatermarkPanelLabel("Position X", 95f);
        _watermarkXSlider = CreateWatermarkPanelSlider(95f);
        _watermarkXSlider.OnValueChanged += OnWatermarkXChanged;

        AddWatermarkPanelLabel("Position Y", 130f);
        _watermarkYSlider = CreateWatermarkPanelSlider(130f);
        _watermarkYSlider.OnValueChanged += OnWatermarkYChanged;

        AddWatermarkPanelLabel("Rotation", 165f);
        _watermarkRotationSlider = CreateWatermarkPanelSlider(165f);
        _watermarkRotationSlider.OnValueChanged += OnWatermarkRotationChanged;

        AddWatermarkPanelLabel("Scale", 200f);
        _watermarkScaleSlider = CreateWatermarkPanelSlider(200f);
        _watermarkScaleSlider.OnValueChanged += OnWatermarkScaleChanged;

        _resetWatermarkButton = Glazier.Get().CreateButton();
        _resetWatermarkButton.PositionOffset_X = 180f;
        _resetWatermarkButton.PositionOffset_Y = 245f;
        _resetWatermarkButton.SizeOffset_X = 240f;
        _resetWatermarkButton.SizeOffset_Y = 30f;
        _resetWatermarkButton.Text = "Reset Watermark";
        _resetWatermarkButton.TextColor = ESleekTint.FONT;
        _resetWatermarkButton.OnClicked += OnClickedResetWatermarkButton;
        _watermarkSettingsPanel.AddChild(_resetWatermarkButton);
    }

    private void AddWatermarkPanelLabel(string text, float y)
    {
        ISleekBox label = Glazier.Get().CreateBox();
        label.PositionOffset_X = 20f;
        label.PositionOffset_Y = y;
        label.SizeOffset_X = 140f;
        label.SizeOffset_Y = 30f;
        label.Text = text;
        label.TextAlignment = TextAnchor.MiddleCenter;
        label.FontSize = ESleekFontSize.Small;
        label.TextColor = ESleekTint.FONT;
        _watermarkSettingsPanel!.AddChild(label);
    }

    private ISleekSlider CreateWatermarkPanelSlider(float y)
    {
        ISleekSlider slider = Glazier.Get().CreateSlider();
        slider.PositionOffset_X = 180f;
        slider.PositionOffset_Y = y + 5f;
        slider.SizeOffset_X = 240f;
        slider.SizeOffset_Y = 20f;
        slider.Orientation = ESleekOrientation.HORIZONTAL;
        _watermarkSettingsPanel!.AddChild(slider);
        return slider;
    }

    private void AddWatermarkRenderer()
    {
        _watermarkObject = new GameObject("EditorHelper2 Cinematic Watermark");
        Object.DontDestroyOnLoad(_watermarkObject);
        _watermarkRenderer = _watermarkObject.AddComponent<CinematicWatermarkRenderer>();
        ApplyWatermarkSettings();
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
            _weatherButton.state = (int)GetCurrentWeatherMode();
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
            _fovSlider.Value = FovToSliderValue(_smoothCameraBehaviour?.TargetFov ?? OptionsSettings.DesiredVerticalFieldOfView);
        }

        if (_rollSlider != null)
        {
            _rollSlider.Value = RollToSliderValue(_smoothCameraBehaviour?.RollDegrees ?? 0f);
        }

        if (_dofToggle != null)
        {
            _dofToggle.Value = _smoothCameraBehaviour?.IsDepthOfFieldEnabled == true;
        }

        if (_dofFocusSlider != null)
        {
            _dofFocusSlider.Value = DofFocusToSliderValue(_smoothCameraBehaviour?.DepthOfFieldFocusDistance ?? 25f);
        }

        if (_dofStrengthSlider != null)
        {
            _dofStrengthSlider.Value = DofStrengthToSliderValue(_smoothCameraBehaviour?.DepthOfFieldAperture ?? 5.6f);
        }

        if (_vignetteSlider != null)
        {
            _vignetteSlider.Value = VignetteToSliderValue(_smoothCameraBehaviour?.VignetteIntensity ?? 0f);
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
            _smoothnessSlider.Value = SmoothnessToSliderValue(_smoothCameraBehaviour?.Smoothness ?? 8f);
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
            _sunShaftsButton.state = IndexOfQuality(GraphicQualityStates, GraphicsSettings.sunShaftsQuality);
        }

        if (_lightingButton != null)
        {
            _lightingButton.state = IndexOfQuality(GraphicQualityStates, GraphicsSettings.lightingQuality);
        }

        if (_waterButton != null)
        {
            _waterButton.state = IndexOfQuality(WaterQualityStates, GraphicsSettings.waterQuality);
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

        if (!TrySetCinematicMode(value))
        {
            RefreshAllControls();
            UnturnedLog.warn("[EditorHelper2] Unable to toggle cinematic mode because the Unturned graphics flag was not found.");
            return;
        }

        GraphicsSettings.apply("EditorHelper2 cinematic mode toggle");
        RefreshLoadedVisibility(value);
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
            _smoothCameraBehaviour.TargetFov = SliderValueToFov(state);
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
            _smoothCameraBehaviour.RollDegrees = SliderValueToRoll(state);
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
            _smoothCameraBehaviour.DepthOfFieldFocusDistance = SliderValueToDofFocus(state);
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
            _smoothCameraBehaviour.DepthOfFieldAperture = SliderValueToDofAperture(state);
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
            _smoothCameraBehaviour.VignetteIntensity = SliderValueToVignette(state);
        }
    }

    private void OnExposureSliderChanged(ISleekSlider slider, float state)
    {
        SetColorGrade(exposure: SliderValueToRangedValue(state, ExposureMin, ExposureMax));
    }

    private void OnContrastSliderChanged(ISleekSlider slider, float state)
    {
        SetColorGrade(contrast: SliderValueToRangedValue(state, GradeMin, GradeMax));
    }

    private void OnSaturationSliderChanged(ISleekSlider slider, float state)
    {
        SetColorGrade(saturation: SliderValueToRangedValue(state, GradeMin, GradeMax));
    }

    private void OnTemperatureSliderChanged(ISleekSlider slider, float state)
    {
        SetColorGrade(temperature: SliderValueToRangedValue(state, GradeMin, GradeMax));
    }

    private void OnTintSliderChanged(ISleekSlider slider, float state)
    {
        SetColorGrade(tint: SliderValueToRangedValue(state, GradeMin, GradeMax));
    }

    private void SetColorGrade(float? exposure = null, float? contrast = null, float? saturation = null, float? temperature = null, float? tint = null)
    {
        if (_isRefreshing)
        {
            return;
        }

        EnsureSmoothCameraBehaviour();
        if (_smoothCameraBehaviour == null)
        {
            return;
        }

        if (exposure.HasValue)
        {
            _smoothCameraBehaviour.Exposure = exposure.Value;
        }

        if (contrast.HasValue)
        {
            _smoothCameraBehaviour.Contrast = contrast.Value;
        }

        if (saturation.HasValue)
        {
            _smoothCameraBehaviour.Saturation = saturation.Value;
        }

        if (temperature.HasValue)
        {
            _smoothCameraBehaviour.Temperature = temperature.Value;
        }

        if (tint.HasValue)
        {
            _smoothCameraBehaviour.Tint = tint.Value;
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
            _smoothCameraBehaviour.Smoothness = SliderValueToSmoothness(state);
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

        GraphicsSettings.sunShaftsQuality = GraphicQualityStates[Mathf.Clamp(index, 0, GraphicQualityStates.Length - 1)];
        GraphicsSettings.apply("EditorHelper2 cinematic sun shafts quality");
    }

    private void OnLightingQualityChanged(SleekButtonState button, int index)
    {
        if (_isRefreshing)
        {
            return;
        }

        GraphicsSettings.lightingQuality = GraphicQualityStates[Mathf.Clamp(index, 0, GraphicQualityStates.Length - 1)];
        GraphicsSettings.apply("EditorHelper2 cinematic lighting quality");
    }

    private void OnWaterQualityChanged(SleekButtonState button, int index)
    {
        if (_isRefreshing)
        {
            return;
        }

        GraphicsSettings.waterQuality = WaterQualityStates[Mathf.Clamp(index, 0, WaterQualityStates.Length - 1)];
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

    private static CinematicWeatherMode GetCurrentWeatherMode()
    {
        WeatherAssetBase activeWeather = LevelLighting.GetActiveWeatherAsset();
        if (activeWeather == null)
        {
            return CinematicWeatherMode.None;
        }

        if (activeWeather.GUID == HeavyRainWeatherReference.GUID)
        {
            return CinematicWeatherMode.Thunder;
        }

        if (WeatherAssetBase.DEFAULT_RAIN.isReferenceTo(activeWeather))
        {
            return CinematicWeatherMode.NormalRain;
        }

        if (WeatherAssetBase.DEFAULT_SNOW.isReferenceTo(activeWeather))
        {
            return CinematicWeatherMode.Snow;
        }

        return CinematicWeatherMode.None;
    }

    private static void SetWeatherMode(CinematicWeatherMode mode)
    {
        switch (mode)
        {
            case CinematicWeatherMode.None:
                LightingManager.DisableWeather();
                LevelLighting.rainyness = ELightingRain.NONE;
                LevelLighting.snowyness = ELightingSnow.NONE;
                break;

            case CinematicWeatherMode.NormalRain:
                ActivateWeather(WeatherAssetBase.DEFAULT_RAIN, "default rain");
                LevelLighting.rainyness = ELightingRain.DRIZZLE;
                LevelLighting.snowyness = ELightingSnow.NONE;
                break;

            case CinematicWeatherMode.Thunder:
                ActivateWeather(HeavyRainWeatherReference, "heavy rain with lightning");
                LevelLighting.rainyness = ELightingRain.DRIZZLE;
                LevelLighting.snowyness = ELightingSnow.NONE;
                break;

            case CinematicWeatherMode.Snow:
                ActivateWeather(WeatherAssetBase.DEFAULT_SNOW, "default snow");
                LevelLighting.rainyness = ELightingRain.NONE;
                LevelLighting.snowyness = ELightingSnow.BLIZZARD;
                break;
        }

        LevelLighting.MarkParticleCloudsNeedRestart();
    }

    private static void ActivateWeather(AssetReference<WeatherAssetBase> reference, string label)
    {
        WeatherAssetBase asset = reference.Find();
        if (asset == null)
        {
            UnturnedLog.warn("[EditorHelper2] Unable to activate cinematic weather because {0} was not found.", label);
            return;
        }

        LightingManager.ActivatePerpetualWeather(asset);
    }

    private static bool TrySetCinematicMode(bool value)
    {
        if (CinematicModeFlagField?.GetValue(null) is not CommandLineFlag flag)
        {
            return false;
        }

        flag.value = value;
        return true;
    }

    private static void RefreshLoadedVisibility(bool cinematicEnabled)
    {
        RefreshObjectVisibility();
        RefreshResourceVisibility();
        RefreshRoadVisibility(cinematicEnabled);
    }

    private static void RefreshObjectVisibility()
    {
        List<LevelObject>[,]? objects = LevelObjects.objects;
        if (objects == null)
        {
            return;
        }

        for (int x = 0; x < objects.GetLength(0); x++)
        {
            for (int y = 0; y < objects.GetLength(1); y++)
            {
                foreach (LevelObject levelObject in objects[x, y])
                {
                    levelObject?.UpdateActiveAndRenderersEnabled();
                }
            }
        }
    }

    private static void RefreshResourceVisibility()
    {
        if (ResourceSpawnpointUpdateActiveMethod == null)
        {
            return;
        }

        List<ResourceSpawnpoint> resources = [];
        LevelGround.GatherAllTrees(resources);
        foreach (ResourceSpawnpoint resource in resources)
        {
            ResourceSpawnpointUpdateActiveMethod.Invoke(resource, null);
        }
    }

    private static void RefreshRoadVisibility(bool cinematicEnabled)
    {
        if (cinematicEnabled)
        {
            ForceAllRoadSegmentsVisible();
        }
        else
        {
            LevelRoads.ImmediatelySyncRegionalVisibility();
        }
    }

    private static void ForceAllRoadSegmentsVisible()
    {
        if (RoadRegionSegmentRenderersField?.GetValue(null) is not IDictionary regionSegmentRenderers)
        {
            return;
        }

        foreach (object? value in regionSegmentRenderers.Values)
        {
            if (value is not IEnumerable renderers)
            {
                continue;
            }

            foreach (object? rendererObject in renderers)
            {
                if (rendererObject is MeshRenderer renderer)
                {
                    renderer.forceRenderingOff = false;
                }
            }
        }
    }

    private static int IndexOfQuality(EGraphicQuality[] qualities, EGraphicQuality quality)
    {
        int index = Array.IndexOf(qualities, quality);
        return index >= 0 ? index : 0;
    }

    private static string FormatQualityName(EGraphicQuality quality)
    {
        return quality switch
        {
            EGraphicQuality.OFF => "Off",
            EGraphicQuality.LOW => "Low",
            EGraphicQuality.MEDIUM => "Medium",
            EGraphicQuality.HIGH => "High",
            EGraphicQuality.ULTRA => "Ultra",
            _ => quality.ToString()
        };
    }

    private static float SliderValueToFov(float value)
    {
        return Mathf.Lerp(FovMin, FovMax, Mathf.Clamp01(value));
    }

    private static float FovToSliderValue(float fov)
    {
        return Mathf.InverseLerp(FovMin, FovMax, Mathf.Clamp(fov, FovMin, FovMax));
    }

    private static float SliderValueToRoll(float value)
    {
        return Mathf.Lerp(RollMin, RollMax, Mathf.Clamp01(value));
    }

    private static float RollToSliderValue(float roll)
    {
        return Mathf.InverseLerp(RollMin, RollMax, Mathf.Clamp(roll, RollMin, RollMax));
    }

    private static float SliderValueToDofFocus(float value)
    {
        return Mathf.Lerp(DofFocusMin, DofFocusMax, Mathf.Clamp01(value));
    }

    private static float DofFocusToSliderValue(float focusDistance)
    {
        return Mathf.InverseLerp(DofFocusMin, DofFocusMax, Mathf.Clamp(focusDistance, DofFocusMin, DofFocusMax));
    }

    private static float SliderValueToDofAperture(float value)
    {
        return Mathf.Lerp(DofApertureMax, DofApertureMin, Mathf.Clamp01(value));
    }

    private static float DofStrengthToSliderValue(float aperture)
    {
        return Mathf.InverseLerp(DofApertureMax, DofApertureMin, Mathf.Clamp(aperture, DofApertureMin, DofApertureMax));
    }

    private static float SliderValueToVignette(float value)
    {
        return Mathf.Lerp(0f, VignetteMax, Mathf.Clamp01(value));
    }

    private static float VignetteToSliderValue(float intensity)
    {
        return Mathf.InverseLerp(0f, VignetteMax, Mathf.Clamp(intensity, 0f, VignetteMax));
    }

    private static float SliderValueToRangedValue(float value, float min, float max)
    {
        return Mathf.Lerp(min, max, Mathf.Clamp01(value));
    }

    private static float RangedValueToSliderValue(float value, float min, float max)
    {
        return Mathf.InverseLerp(min, max, Mathf.Clamp(value, min, max));
    }

    private static float SliderValueToSmoothness(float value)
    {
        return Mathf.Lerp(SmoothnessMax, SmoothnessMin, Mathf.Clamp01(value));
    }

    private static float SmoothnessToSliderValue(float smoothness)
    {
        return Mathf.InverseLerp(SmoothnessMax, SmoothnessMin, Mathf.Clamp(smoothness, SmoothnessMin, SmoothnessMax));
    }

    public void Dispose()
    {
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
                _postProcessOverrides.Apply(this);
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

    private sealed class CinematicPostProcessOverrides
    {
        private static readonly FieldInfo? BaseProfileField = typeof(UnturnedPostProcess)
            .GetField("baseProfile", BindingFlags.Instance | BindingFlags.NonPublic);

        private bool _hasAppliedOverrides;

        public void Apply(CinematicCameraSmoothingBehaviour settings)
        {
            object? baseProfile = GetBaseProfile();
            if (baseProfile == null)
            {
                return;
            }

            _hasAppliedOverrides = true;
            ApplyDepthOfField(baseProfile, settings);
            ApplyVignette(baseProfile, settings);
            ApplyColorGrading(baseProfile, settings);
        }

        public void Reset()
        {
            if (!_hasAppliedOverrides)
            {
                return;
            }

            object? baseProfile = GetBaseProfile();
            if (baseProfile == null)
            {
                return;
            }

            object? dof = GetSetting(baseProfile, "dof");
            SetActive(dof, false);
            OverrideFloat(dof, "focusDistance", 1f);

            object? vignette = GetSetting(baseProfile, "vignette");
            SetActive(vignette, false);
            OverrideFloat(vignette, "intensity", 0f);

            object? colorGrading = GetSetting(baseProfile, "colorGrading");
            SetActive(colorGrading, false);
            OverrideFloat(colorGrading, "postExposure", 0f);
            OverrideFloat(colorGrading, "contrast", 0f);
            OverrideFloat(colorGrading, "saturation", 0f);
            OverrideFloat(colorGrading, "temperature", 0f);
            OverrideFloat(colorGrading, "tint", 0f);
            _hasAppliedOverrides = false;
        }

        private static object? GetBaseProfile()
        {
            UnturnedPostProcess postProcess = UnturnedPostProcess.instance;
            if (postProcess == null)
            {
                return null;
            }

            return BaseProfileField?.GetValue(postProcess);
        }

        private static void ApplyDepthOfField(object baseProfile, CinematicCameraSmoothingBehaviour settings)
        {
            object? dof = GetSetting(baseProfile, "dof");
            SetActive(dof, settings.IsDepthOfFieldEnabled);
            if (!settings.IsDepthOfFieldEnabled)
            {
                return;
            }

            OverrideFloat(dof, "focusDistance", Mathf.Clamp(settings.DepthOfFieldFocusDistance, DofFocusMin, DofFocusMax));
            OverrideFloat(dof, "aperture", Mathf.Clamp(settings.DepthOfFieldAperture, DofApertureMin, DofApertureMax));
        }

        private static void ApplyVignette(object baseProfile, CinematicCameraSmoothingBehaviour settings)
        {
            float intensity = Mathf.Clamp(settings.VignetteIntensity, 0f, VignetteMax);
            object? vignette = GetSetting(baseProfile, "vignette");
            SetActive(vignette, intensity > 0.001f);
            OverrideFloat(vignette, "intensity", intensity);
        }

        private static void ApplyColorGrading(object baseProfile, CinematicCameraSmoothingBehaviour settings)
        {
            bool isActive = IsNonZero(settings.Exposure)
                || IsNonZero(settings.Contrast)
                || IsNonZero(settings.Saturation)
                || IsNonZero(settings.Temperature)
                || IsNonZero(settings.Tint);

            object? colorGrading = GetSetting(baseProfile, "colorGrading");
            SetActive(colorGrading, isActive);
            OverrideFloat(colorGrading, "postExposure", Mathf.Clamp(settings.Exposure, ExposureMin, ExposureMax));
            OverrideFloat(colorGrading, "contrast", Mathf.Clamp(settings.Contrast, GradeMin, GradeMax));
            OverrideFloat(colorGrading, "saturation", Mathf.Clamp(settings.Saturation, GradeMin, GradeMax));
            OverrideFloat(colorGrading, "temperature", Mathf.Clamp(settings.Temperature, GradeMin, GradeMax));
            OverrideFloat(colorGrading, "tint", Mathf.Clamp(settings.Tint, GradeMin, GradeMax));
        }

        private static object? GetSetting(object profileWrapper, string fieldName)
        {
            return profileWrapper.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(profileWrapper);
        }

        private static void SetActive(object? setting, bool active)
        {
            if (setting == null)
            {
                return;
            }

            FieldInfo? activeField = setting.GetType().GetField("active", BindingFlags.Instance | BindingFlags.Public);
            if (activeField != null)
            {
                activeField.SetValue(setting, active);
                return;
            }

            PropertyInfo? activeProperty = setting.GetType().GetProperty("active", BindingFlags.Instance | BindingFlags.Public);
            activeProperty?.SetValue(setting, active);
        }

        private static void OverrideFloat(object? setting, string parameterName, float value)
        {
            object? parameter = GetSettingParameter(setting, parameterName);
            MethodInfo? overrideMethod = parameter?.GetType().GetMethod("Override", [typeof(float)]);
            overrideMethod?.Invoke(parameter, [value]);
        }

        private static object? GetSettingParameter(object? setting, string parameterName)
        {
            if (setting == null)
            {
                return null;
            }

            Type settingType = setting.GetType();
            return settingType.GetField(parameterName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(setting)
                ?? settingType.GetProperty(parameterName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetValue(setting);
        }

        private static bool IsNonZero(float value)
        {
            return Mathf.Abs(value) > 0.001f;
        }
    }
}
