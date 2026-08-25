using System;
using System.Collections.Generic;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Helpers;
using SDG.Unturned;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorHelper2.Extensions.PlayerUIs;

/// <summary>
/// Provides client-side cinematic controls while playing a level.
/// </summary>
[UIExtension(typeof(PlayerUI))]
[EHExtension("In-Game Cinematic Mode", "Senior S", alwaysEnabled: true)]
public sealed class PlayerCinematicModeExtension : UIExtension, IExtension
{
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
    private const float LabelWidth = 150f;
    private const float ControlWidth = 240f;
    private const float LeftLabelX = 30f;
    private const float LeftControlX = 190f;
    private const float RightLabelX = -460f;
    private const float RightControlX = -300f;

    internal static PlayerCinematicModeExtension? Current { get; private set; }

    [ExistingMember("container")]
    private readonly ISleekElement? _playerContainer;

    private SleekFullscreenBox? _cinematicContainer;
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
    private ISleekToggle? _windToggle;
    private ISleekToggle? _filmGrainToggle;
    private SleekButtonState? _sunShaftsButton;
    private SleekButtonState? _lightingButton;
    private SleekButtonState? _waterButton;
    private ISleekButton? _exitButton;
    private GameObject? _cameraOverrideObject;
    private CameraOverrideBehaviour? _cameraOverrideBehaviour;
    private CinematicRestoreState? _restoreState;
    private bool _modeActive;
    private bool _panelVisible;
    private bool _isRefreshing;
    private bool _observedFreecamActive;
    private float _targetFov = 60f;
    private float _targetRoll;
    private bool _isDepthOfFieldEnabled;
    private float _depthOfFieldFocus = 25f;
    private float _depthOfFieldAperture = 5.6f;
    private float _vignette;
    private float _exposure;
    private float _contrast;
    private float _saturation;
    private float _temperature;
    private float _tint;
    private byte _targetMoon;
    private float _targetTime;
    private float _targetSnowLevel;
    private float _targetSeaLevel;
    private CinematicWeatherMode _targetWeather;
    private readonly CinematicPostProcessOverrides _postProcessOverrides = new();
    private readonly List<ISleekElement> _interactiveElements = [];
    private bool _isCameraInputCaptured;

    /// <summary>
    /// Creates the controls for the current player UI.
    /// </summary>
    public PlayerCinematicModeExtension()
    {
        Current = this;
        Initialize();
    }

    /// <summary>
    /// Adds the cinematic panel and camera override component.
    /// </summary>
    public void Initialize()
    {
        if (_playerContainer == null)
        {
            return;
        }

        BuildCinematicContainer();
        _cameraOverrideObject = new GameObject("EditorHelper2 Player Cinematic Camera");
        _cameraOverrideBehaviour = _cameraOverrideObject.AddComponent<CameraOverrideBehaviour>();
        _cameraOverrideBehaviour.Owner = this;
    }

    /// <summary>
    /// Handles the Shift+F8 shortcut and modal Escape input before the game opens another menu.
    /// </summary>
    public void HandleInput()
    {
        if (SDG.Unturned.Level.isEditor || !SDG.Unturned.Level.isLoaded)
        {
            return;
        }

        if (InputEx.GetKey(KeyCode.LeftShift) && InputEx.ConsumeKeyDown(KeyCode.F8))
        {
            if (!_modeActive)
            {
                EnterMode();
            }
            else
            {
                ExitMode();
            }

            return;
        }

        PrepareGameInput();

        if (_panelVisible && InputEx.ConsumeKeyDown(KeyCode.Escape))
        {
            ExitMode();
        }
    }

    /// <summary>
    /// Keeps the controls modal and applies local environment overrides after the game updates lighting.
    /// </summary>
    public void AfterPlayerUiUpdate()
    {
        if (!_modeActive)
        {
            return;
        }

        if (Player.LocalPlayer != null && Player.LocalPlayer.look.IsLocallyUsingFreecam)
        {
            _observedFreecamActive = true;
        }
        else if (_observedFreecamActive)
        {
            ExitMode();
            return;
        }

        PrepareGameInput();
    }

    internal void PrepareGameInput()
    {
        if (!_modeActive || !_panelVisible || PlayerUI.window == null)
        {
            return;
        }

        if (InputEx.GetKeyDown(KeyCode.Mouse1) && !IsCursorOverInteractiveElement())
        {
            _isCameraInputCaptured = true;
        }

        if (_isCameraInputCaptured && InputEx.GetKeyUp(KeyCode.Mouse1))
        {
            _isCameraInputCaptured = false;
        }

        PlayerUI.window.showCursor = !_isCameraInputCaptured;
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
            SizeScale_Y = 1f,
            IsVisible = false
        };
        _playerContainer!.AddChild(_cinematicContainer);

        AddLabel("Whole-map render", 100f, useLeftColumn: true);
        _cinematicToggle = CreateToggle(100f, useLeftColumn: true);
        _cinematicToggle.TooltipText = "Render distant level objects for cinematic shots. This can reduce performance.";
        _cinematicToggle.OnValueChanged += OnCinematicToggleChanged;

        AddLabel("Camera FOV", 145f, useLeftColumn: true);
        _fovSlider = CreateSlider(145f, useLeftColumn: true);
        _fovSlider.OnValueChanged += OnFovChanged;

        AddLabel("Camera roll", 190f, useLeftColumn: true);
        _rollSlider = CreateSlider(190f, useLeftColumn: true);
        _rollSlider.OnValueChanged += OnRollChanged;

        AddLabel("Depth of field", 235f, useLeftColumn: true);
        _dofToggle = CreateToggle(235f, useLeftColumn: true);
        _dofToggle.OnValueChanged += OnDepthOfFieldChanged;

        AddLabel("DOF focus", 275f, useLeftColumn: true);
        _dofFocusSlider = CreateSlider(275f, useLeftColumn: true);
        _dofFocusSlider.OnValueChanged += OnDepthOfFieldFocusChanged;

        AddLabel("DOF strength", 315f, useLeftColumn: true);
        _dofStrengthSlider = CreateSlider(315f, useLeftColumn: true);
        _dofStrengthSlider.OnValueChanged += OnDepthOfFieldStrengthChanged;

        AddLabel("Vignette", 355f, useLeftColumn: true);
        _vignetteSlider = CreateSlider(355f, useLeftColumn: true);
        _vignetteSlider.OnValueChanged += OnVignetteChanged;

        AddLabel("Exposure", 395f, useLeftColumn: true);
        _exposureSlider = CreateSlider(395f, useLeftColumn: true);
        _exposureSlider.OnValueChanged += OnExposureChanged;

        AddLabel("Contrast", 435f, useLeftColumn: true);
        _contrastSlider = CreateSlider(435f, useLeftColumn: true);
        _contrastSlider.OnValueChanged += OnContrastChanged;

        AddLabel("Saturation", 475f, useLeftColumn: true);
        _saturationSlider = CreateSlider(475f, useLeftColumn: true);
        _saturationSlider.OnValueChanged += OnSaturationChanged;

        AddLabel("Temperature", 515f, useLeftColumn: true);
        _temperatureSlider = CreateSlider(515f, useLeftColumn: true);
        _temperatureSlider.OnValueChanged += OnTemperatureChanged;

        AddLabel("Tint", 555f, useLeftColumn: true);
        _tintSlider = CreateSlider(555f, useLeftColumn: true);
        _tintSlider.OnValueChanged += OnTintChanged;

        _resetVisualsButton = Glazier.Get().CreateButton();
        _resetVisualsButton.PositionOffset_X = LeftControlX;
        _resetVisualsButton.PositionOffset_Y = 595f;
        _resetVisualsButton.SizeOffset_X = ControlWidth;
        _resetVisualsButton.SizeOffset_Y = 30f;
        _resetVisualsButton.Text = "Reset Visuals";
        _resetVisualsButton.OnClicked += OnResetVisualsClicked;
        _cinematicContainer.AddChild(_resetVisualsButton);
        _interactiveElements.Add(_resetVisualsButton);

        AddLabel("Weather", 100f);
        _weatherButton = new SleekButtonState(
            new GUIContent("None", "Clear local weather."),
            new GUIContent("Rain", "Preview rain locally."),
            new GUIContent("Thunder", "Preview heavy rain and lightning locally."),
            new GUIContent("Snow", "Preview snow locally."))
        {
            PositionOffset_X = RightControlX,
            PositionOffset_Y = 100f,
            PositionScale_X = 1f,
            SizeOffset_X = ControlWidth,
            SizeOffset_Y = 30f
        };
        _weatherButton.onSwappedState += OnWeatherChanged;
        _cinematicContainer.AddChild(_weatherButton);
        _interactiveElements.Add(_weatherButton);

        AddLabel("Moon", 145f);
        _moonSlider = CreateSlider(145f);
        _moonSlider.OnValueChanged += OnMoonChanged;

        AddLabel("Time", 190f);
        _timeSlider = CreateSlider(190f);
        _timeSlider.OnValueChanged += OnTimeChanged;

        AddLabel("Snow level", 235f);
        _snowLevelSlider = CreateValueSlider(235f);
        _snowLevelSlider.onValued += OnSnowLevelChanged;

        AddLabel("Sea level", 280f);
        _seaLevelSlider = CreateValueSlider(280f);
        _seaLevelSlider.onValued += OnSeaLevelChanged;

        AddLabel("Smooth camera", 325f);
        _smoothCameraToggle = CreateToggle(325f);
        _smoothCameraToggle.TooltipText = "Toggle Unturned's built-in freecam smoothing, also available with Shift+F5.";
        _smoothCameraToggle.OnValueChanged += OnSmoothCameraChanged;

        AddLabel("Wind effects", 365f);
        _windToggle = CreateToggle(365f);
        _windToggle.OnValueChanged += OnWindChanged;

        AddLabel("Film grain", 405f);
        _filmGrainToggle = CreateToggle(405f);
        _filmGrainToggle.OnValueChanged += OnFilmGrainChanged;

        AddLabel("Sun shafts", 445f);
        _sunShaftsButton = CreateQualityButton(445f, CinematicModeUtility.GraphicQualityStates);
        _sunShaftsButton.onSwappedState += OnSunShaftsChanged;

        AddLabel("Lighting", 485f);
        _lightingButton = CreateQualityButton(485f, CinematicModeUtility.GraphicQualityStates);
        _lightingButton.onSwappedState += OnLightingChanged;

        AddLabel("Water", 525f);
        _waterButton = CreateQualityButton(525f, CinematicModeUtility.WaterQualityStates);
        _waterButton.onSwappedState += OnWaterChanged;

        _exitButton = Glazier.Get().CreateButton();
        _exitButton.PositionOffset_X = -240f;
        _exitButton.PositionOffset_Y = -55f;
        _exitButton.PositionScale_X = 1f;
        _exitButton.PositionScale_Y = 1f;
        _exitButton.SizeOffset_X = 200f;
        _exitButton.SizeOffset_Y = 30f;
        _exitButton.Text = "Exit Cinematic";
        _exitButton.TooltipText = "Restore the level and camera settings captured when cinematic mode opened.";
        _exitButton.OnClicked += OnExitClicked;
        _cinematicContainer.AddChild(_exitButton);
        _interactiveElements.Add(_exitButton);
    }

    private void AddLabel(string text, float y, bool useLeftColumn = false)
    {
        ISleekBox label = Glazier.Get().CreateBox();
        label.PositionOffset_X = useLeftColumn ? LeftLabelX : RightLabelX;
        label.PositionOffset_Y = y;
        label.PositionScale_X = useLeftColumn ? 0f : 1f;
        label.SizeOffset_X = LabelWidth;
        label.SizeOffset_Y = 30f;
        label.Text = text;
        label.TextAlignment = TextAnchor.MiddleCenter;
        label.FontSize = ESleekFontSize.Small;
        label.TextColor = ESleekTint.FONT;
        _cinematicContainer!.AddChild(label);
    }

    private ISleekToggle CreateToggle(float y, bool useLeftColumn = false)
    {
        ISleekToggle toggle = Glazier.Get().CreateToggle();
        toggle.PositionOffset_X = useLeftColumn ? LeftControlX : RightControlX;
        toggle.PositionOffset_Y = y - 5f;
        toggle.PositionScale_X = useLeftColumn ? 0f : 1f;
        toggle.SizeOffset_X = 40f;
        toggle.SizeOffset_Y = 40f;
        _cinematicContainer!.AddChild(toggle);
        _interactiveElements.Add(toggle);
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
        _interactiveElements.Add(slider);
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
        _interactiveElements.Add(slider);
        return slider;
    }

    private SleekButtonState CreateQualityButton(float y, EGraphicQuality[] qualities)
    {
        GUIContent[] states = new GUIContent[qualities.Length];
        for (int i = 0; i < qualities.Length; i++)
        {
            states[i] = new GUIContent(CinematicModeUtility.FormatQualityName(qualities[i]));
        }

        SleekButtonState button = new(states)
        {
            PositionOffset_X = RightControlX,
            PositionOffset_Y = y,
            PositionScale_X = 1f,
            SizeOffset_X = ControlWidth,
            SizeOffset_Y = 30f
        };
        _cinematicContainer!.AddChild(button);
        _interactiveElements.Add(button);
        return button;
    }

    private void EnterMode()
    {
        if (Player.LocalPlayer == null)
        {
            return;
        }

        _restoreState = CinematicRestoreState.Capture();
        _observedFreecamActive = Player.LocalPlayer.look.IsLocallyUsingFreecam;
        _targetMoon = LevelLighting.moon;
        _targetTime = LevelLighting.time;
        _targetSnowLevel = LevelLighting.snowLevel;
        _targetSeaLevel = LevelLighting.seaLevel;
        _targetWeather = CinematicModeUtility.GetCurrentWeatherMode();
        _targetFov = MainCamera.instance != null
            ? MainCamera.instance.fieldOfView
            : OptionsSettings.DesiredVerticalFieldOfView;
        _targetRoll = 0f;
        ResetVisualTargets();
        _cameraOverrideBehaviour?.CaptureCameraState();
        _modeActive = true;
        _isCameraInputCaptured = false;

        PlayerLifeUI.close();
        RefreshControls();
        _panelVisible = true;
        if (_cinematicContainer != null)
        {
            _cinematicContainer.IsVisible = true;
            _cinematicContainer.AnimateIntoView();
        }
    }

    internal void ExitMode()
    {
        if (!_modeActive)
        {
            return;
        }

        _modeActive = false;
        _panelVisible = false;
        _isCameraInputCaptured = false;
        _cinematicContainer?.AnimateOutOfView(1f, 0f);
        _cameraOverrideBehaviour?.RestoreCameraState();
        _postProcessOverrides.Reset();
        _restoreState?.Restore();
        _restoreState = null;
    }

    private void RefreshControls()
    {
        _isRefreshing = true;
        if (_cinematicToggle != null)
        {
            _cinematicToggle.Value = GraphicsSettings.WantsCinematicMode;
        }

        if (_weatherButton != null)
        {
            _weatherButton.state = (int)_targetWeather;
        }

        if (_moonSlider != null)
        {
            _moonSlider.Value = (float)_targetMoon / LevelLighting.MOON_CYCLES;
        }

        if (_timeSlider != null)
        {
            _timeSlider.Value = _targetTime;
        }

        if (_snowLevelSlider != null)
        {
            _snowLevelSlider.state = _targetSnowLevel;
        }

        if (_seaLevelSlider != null)
        {
            _seaLevelSlider.state = _targetSeaLevel;
        }

        if (_fovSlider != null)
        {
            _fovSlider.Value = Mathf.InverseLerp(FovMin, FovMax, _targetFov);
        }

        if (_rollSlider != null)
        {
            _rollSlider.Value = Mathf.InverseLerp(RollMin, RollMax, _targetRoll);
        }

        if (_dofToggle != null)
        {
            _dofToggle.Value = _isDepthOfFieldEnabled;
        }

        if (_dofFocusSlider != null)
        {
            _dofFocusSlider.Value = Mathf.InverseLerp(DofFocusMin, DofFocusMax, _depthOfFieldFocus);
        }

        if (_dofStrengthSlider != null)
        {
            _dofStrengthSlider.Value = Mathf.InverseLerp(DofApertureMax, DofApertureMin, _depthOfFieldAperture);
        }

        if (_vignetteSlider != null)
        {
            _vignetteSlider.Value = Mathf.InverseLerp(0f, VignetteMax, _vignette);
        }

        if (_exposureSlider != null)
        {
            _exposureSlider.Value = Mathf.InverseLerp(ExposureMin, ExposureMax, _exposure);
        }

        if (_contrastSlider != null)
        {
            _contrastSlider.Value = Mathf.InverseLerp(GradeMin, GradeMax, _contrast);
        }

        if (_saturationSlider != null)
        {
            _saturationSlider.Value = Mathf.InverseLerp(GradeMin, GradeMax, _saturation);
        }

        if (_temperatureSlider != null)
        {
            _temperatureSlider.Value = Mathf.InverseLerp(GradeMin, GradeMax, _temperature);
        }

        if (_tintSlider != null)
        {
            _tintSlider.Value = Mathf.InverseLerp(GradeMin, GradeMax, _tint);
        }

        if (_smoothCameraToggle != null)
        {
            _smoothCameraToggle.Value = Player.LocalPlayer?.look.isSmoothing == true;
        }

        if (_windToggle != null)
        {
            _windToggle.Value = GraphicsSettings.IsWindEnabled;
        }

        if (_filmGrainToggle != null)
        {
            _filmGrainToggle.Value = GraphicsSettings.filmGrain;
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
    }

    private bool IsCursorOverInteractiveElement()
    {
        foreach (ISleekElement element in _interactiveElements)
        {
            if (!element.IsVisible)
            {
                continue;
            }

            Vector2 cursorPosition = element.GetNormalizedCursorPosition();
            if (cursorPosition.x >= 0f
                && cursorPosition.x <= 1f
                && cursorPosition.y >= 0f
                && cursorPosition.y <= 1f)
            {
                return true;
            }
        }

        return false;
    }

    internal void ApplyEnvironmentOverrides()
    {
        if (!_modeActive)
        {
            return;
        }

        LevelLighting.moon = _targetMoon;
        LevelLighting.time = _targetTime;
        LevelLighting.snowLevel = _targetSnowLevel;
        LevelLighting.seaLevel = _targetSeaLevel;

        WeatherAssetBase? expectedWeather = CinematicModeUtility.FindWeatherAsset(_targetWeather);
        WeatherAssetBase activeWeather = LevelLighting.GetActiveWeatherAsset();
        if (activeWeather != expectedWeather)
        {
            LevelLighting.SetActiveWeatherAsset(expectedWeather, expectedWeather == null ? 0f : 1f, NetId.INVALID);
        }
    }

    private void ApplyCameraOverrides()
    {
        if (!_modeActive || MainCamera.instance == null)
        {
            return;
        }

        MainCamera.instance.fieldOfView = Mathf.Clamp(_targetFov, FovMin, FovMax);
        Transform cameraTransform = MainCamera.instance.transform;
        Vector3 angles = cameraTransform.localEulerAngles;
        cameraTransform.localRotation = Quaternion.Euler(angles.x, angles.y, Mathf.Clamp(_targetRoll, RollMin, RollMax));
        if (_isDepthOfFieldEnabled
            || Mathf.Abs(_vignette) > 0.001f
            || Mathf.Abs(_exposure) > 0.001f
            || Mathf.Abs(_contrast) > 0.001f
            || Mathf.Abs(_saturation) > 0.001f
            || Mathf.Abs(_temperature) > 0.001f
            || Mathf.Abs(_tint) > 0.001f)
        {
            _postProcessOverrides.Apply(
                _isDepthOfFieldEnabled,
                _depthOfFieldFocus,
                _depthOfFieldAperture,
                _vignette,
                _exposure,
                _contrast,
                _saturation,
                _temperature,
                _tint);
        }
        else
        {
            _postProcessOverrides.Reset();
        }
    }

    private void OnCinematicToggleChanged(ISleekToggle toggle, bool value)
    {
        if (_isRefreshing)
        {
            return;
        }

        if (!CinematicModeUtility.TrySetCinematicMode(value))
        {
            UnturnedLog.warn("[EditorHelper2] Unable to update the cinematic graphics flag.");
            RefreshControls();
            return;
        }

        GraphicsSettings.apply("EditorHelper2 player cinematic mode toggle");
        CinematicModeUtility.RefreshLoadedVisibility(value);
    }

    private void OnWeatherChanged(SleekButtonState button, int index)
    {
        if (_isRefreshing)
        {
            return;
        }

        _targetWeather = (CinematicWeatherMode)Mathf.Clamp(index, 0, 3);
        ApplyEnvironmentOverrides();
    }

    private void OnMoonChanged(ISleekSlider slider, float state)
    {
        if (_isRefreshing)
        {
            return;
        }

        _targetMoon = (byte)Mathf.Clamp(
            Mathf.FloorToInt(state * LevelLighting.MOON_CYCLES),
            0,
            LevelLighting.MOON_CYCLES - 1);
        ApplyEnvironmentOverrides();
        LevelLighting.MarkParticleCloudsNeedRestart();
    }

    private void OnTimeChanged(ISleekSlider slider, float state)
    {
        if (!_isRefreshing)
        {
            _targetTime = Mathf.Clamp01(state);
            ApplyEnvironmentOverrides();
            LevelLighting.MarkParticleCloudsNeedRestart();
        }
    }

    private void OnSnowLevelChanged(SleekValue slider, float state)
    {
        if (!_isRefreshing)
        {
            _targetSnowLevel = Mathf.Clamp01(state);
            ApplyEnvironmentOverrides();
        }
    }

    private void OnSeaLevelChanged(SleekValue slider, float state)
    {
        if (!_isRefreshing)
        {
            _targetSeaLevel = Mathf.Clamp01(state);
            ApplyEnvironmentOverrides();
        }
    }

    private void OnFovChanged(ISleekSlider slider, float state)
    {
        if (!_isRefreshing)
        {
            _targetFov = Mathf.Lerp(FovMin, FovMax, Mathf.Clamp01(state));
        }
    }

    private void OnRollChanged(ISleekSlider slider, float state)
    {
        if (!_isRefreshing)
        {
            _targetRoll = Mathf.Lerp(RollMin, RollMax, Mathf.Clamp01(state));
        }
    }

    private void OnDepthOfFieldChanged(ISleekToggle toggle, bool value)
    {
        if (!_isRefreshing)
        {
            _isDepthOfFieldEnabled = value;
        }
    }

    private void OnDepthOfFieldFocusChanged(ISleekSlider slider, float state)
    {
        if (!_isRefreshing)
        {
            _depthOfFieldFocus = Mathf.Lerp(DofFocusMin, DofFocusMax, Mathf.Clamp01(state));
        }
    }

    private void OnDepthOfFieldStrengthChanged(ISleekSlider slider, float state)
    {
        if (!_isRefreshing)
        {
            _depthOfFieldAperture = Mathf.Lerp(DofApertureMax, DofApertureMin, Mathf.Clamp01(state));
        }
    }

    private void OnVignetteChanged(ISleekSlider slider, float state)
    {
        if (!_isRefreshing)
        {
            _vignette = Mathf.Lerp(0f, VignetteMax, Mathf.Clamp01(state));
        }
    }

    private void OnExposureChanged(ISleekSlider slider, float state)
    {
        if (!_isRefreshing)
        {
            _exposure = Mathf.Lerp(ExposureMin, ExposureMax, Mathf.Clamp01(state));
        }
    }

    private void OnContrastChanged(ISleekSlider slider, float state)
    {
        if (!_isRefreshing)
        {
            _contrast = Mathf.Lerp(GradeMin, GradeMax, Mathf.Clamp01(state));
        }
    }

    private void OnSaturationChanged(ISleekSlider slider, float state)
    {
        if (!_isRefreshing)
        {
            _saturation = Mathf.Lerp(GradeMin, GradeMax, Mathf.Clamp01(state));
        }
    }

    private void OnTemperatureChanged(ISleekSlider slider, float state)
    {
        if (!_isRefreshing)
        {
            _temperature = Mathf.Lerp(GradeMin, GradeMax, Mathf.Clamp01(state));
        }
    }

    private void OnTintChanged(ISleekSlider slider, float state)
    {
        if (!_isRefreshing)
        {
            _tint = Mathf.Lerp(GradeMin, GradeMax, Mathf.Clamp01(state));
        }
    }

    private void OnResetVisualsClicked(ISleekElement button)
    {
        _targetFov = OptionsSettings.DesiredVerticalFieldOfView;
        _targetRoll = 0f;
        ResetVisualTargets();
        _postProcessOverrides.Reset();
        RefreshControls();
    }

    private void ResetVisualTargets()
    {
        _isDepthOfFieldEnabled = false;
        _depthOfFieldFocus = 25f;
        _depthOfFieldAperture = 5.6f;
        _vignette = 0f;
        _exposure = 0f;
        _contrast = 0f;
        _saturation = 0f;
        _temperature = 0f;
        _tint = 0f;
    }

    private void OnSmoothCameraChanged(ISleekToggle toggle, bool value)
    {
        if (!_isRefreshing && Player.LocalPlayer != null)
        {
            Player.LocalPlayer.look.isSmoothing = value;
        }
    }

    private void OnWindChanged(ISleekToggle toggle, bool value)
    {
        if (_isRefreshing)
        {
            return;
        }

        GraphicsSettings.IsWindEnabled = value;
        GraphicsSettings.apply("EditorHelper2 player cinematic wind toggle");
    }

    private void OnFilmGrainChanged(ISleekToggle toggle, bool value)
    {
        if (_isRefreshing)
        {
            return;
        }

        GraphicsSettings.filmGrain = value;
        GraphicsSettings.apply("EditorHelper2 player cinematic film grain toggle");
    }

    private void OnSunShaftsChanged(SleekButtonState button, int index)
    {
        if (_isRefreshing)
        {
            return;
        }

        GraphicsSettings.sunShaftsQuality = CinematicModeUtility.GraphicQualityStates[Mathf.Clamp(index, 0, CinematicModeUtility.GraphicQualityStates.Length - 1)];
        GraphicsSettings.apply("EditorHelper2 player cinematic sun shafts quality");
    }

    private void OnLightingChanged(SleekButtonState button, int index)
    {
        if (_isRefreshing)
        {
            return;
        }

        GraphicsSettings.lightingQuality = CinematicModeUtility.GraphicQualityStates[Mathf.Clamp(index, 0, CinematicModeUtility.GraphicQualityStates.Length - 1)];
        GraphicsSettings.apply("EditorHelper2 player cinematic lighting quality");
    }

    private void OnWaterChanged(SleekButtonState button, int index)
    {
        if (_isRefreshing)
        {
            return;
        }

        GraphicsSettings.waterQuality = CinematicModeUtility.WaterQualityStates[Mathf.Clamp(index, 0, CinematicModeUtility.WaterQualityStates.Length - 1)];
        GraphicsSettings.apply("EditorHelper2 player cinematic water quality");
    }

    private void OnExitClicked(ISleekElement button)
    {
        ExitMode();
    }

    /// <summary>
    /// Removes UI callbacks, restores temporary state, and destroys the camera component.
    /// </summary>
    public void Dispose()
    {
        ExitMode();
        if (ReferenceEquals(Current, this))
        {
            Current = null;
        }

        if (_cinematicToggle != null)
        {
            _cinematicToggle.OnValueChanged -= OnCinematicToggleChanged;
        }

        if (_weatherButton != null)
        {
            _weatherButton.onSwappedState -= OnWeatherChanged;
        }

        if (_moonSlider != null)
        {
            _moonSlider.OnValueChanged -= OnMoonChanged;
        }

        if (_timeSlider != null)
        {
            _timeSlider.OnValueChanged -= OnTimeChanged;
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
            _fovSlider.OnValueChanged -= OnFovChanged;
        }

        if (_rollSlider != null)
        {
            _rollSlider.OnValueChanged -= OnRollChanged;
        }

        if (_dofToggle != null)
        {
            _dofToggle.OnValueChanged -= OnDepthOfFieldChanged;
        }

        if (_dofFocusSlider != null)
        {
            _dofFocusSlider.OnValueChanged -= OnDepthOfFieldFocusChanged;
        }

        if (_dofStrengthSlider != null)
        {
            _dofStrengthSlider.OnValueChanged -= OnDepthOfFieldStrengthChanged;
        }

        if (_vignetteSlider != null)
        {
            _vignetteSlider.OnValueChanged -= OnVignetteChanged;
        }

        if (_exposureSlider != null)
        {
            _exposureSlider.OnValueChanged -= OnExposureChanged;
        }

        if (_contrastSlider != null)
        {
            _contrastSlider.OnValueChanged -= OnContrastChanged;
        }

        if (_saturationSlider != null)
        {
            _saturationSlider.OnValueChanged -= OnSaturationChanged;
        }

        if (_temperatureSlider != null)
        {
            _temperatureSlider.OnValueChanged -= OnTemperatureChanged;
        }

        if (_tintSlider != null)
        {
            _tintSlider.OnValueChanged -= OnTintChanged;
        }

        if (_resetVisualsButton != null)
        {
            _resetVisualsButton.OnClicked -= OnResetVisualsClicked;
        }

        if (_smoothCameraToggle != null)
        {
            _smoothCameraToggle.OnValueChanged -= OnSmoothCameraChanged;
        }

        if (_windToggle != null)
        {
            _windToggle.OnValueChanged -= OnWindChanged;
        }

        if (_filmGrainToggle != null)
        {
            _filmGrainToggle.OnValueChanged -= OnFilmGrainChanged;
        }

        if (_sunShaftsButton != null)
        {
            _sunShaftsButton.onSwappedState -= OnSunShaftsChanged;
        }

        if (_lightingButton != null)
        {
            _lightingButton.onSwappedState -= OnLightingChanged;
        }

        if (_waterButton != null)
        {
            _waterButton.onSwappedState -= OnWaterChanged;
        }

        if (_exitButton != null)
        {
            _exitButton.OnClicked -= OnExitClicked;
        }

        if (_cinematicContainer != null)
        {
            _playerContainer?.RemoveChild(_cinematicContainer);
        }

        if (_cameraOverrideObject != null)
        {
            Object.Destroy(_cameraOverrideObject);
        }

        _interactiveElements.Clear();
    }

    private sealed class CameraOverrideBehaviour : MonoBehaviour
    {
        private float _originalFov;
        private float _originalRoll;
        private bool _hasCameraState;

        public PlayerCinematicModeExtension? Owner { get; set; }

        public void CaptureCameraState()
        {
            if (MainCamera.instance == null)
            {
                _hasCameraState = false;
                return;
            }

            _originalFov = MainCamera.instance.fieldOfView;
            _originalRoll = MainCamera.instance.transform.localEulerAngles.z;
            _hasCameraState = true;
        }

        public void RestoreCameraState()
        {
            if (!_hasCameraState || MainCamera.instance == null)
            {
                return;
            }

            MainCamera.instance.fieldOfView = _originalFov;
            Transform cameraTransform = MainCamera.instance.transform;
            Vector3 angles = cameraTransform.localEulerAngles;
            cameraTransform.localRotation = Quaternion.Euler(angles.x, angles.y, _originalRoll);
            _hasCameraState = false;
        }

        private void LateUpdate()
        {
            Owner?.ApplyCameraOverrides();
        }
    }

    private sealed class CinematicRestoreState
    {
        private byte Moon { get; init; }
        private float Time { get; init; }
        private float SnowLevel { get; init; }
        private float SeaLevel { get; init; }
        private ELightingRain Rainyness { get; init; }
        private ELightingSnow Snowyness { get; init; }
        private WeatherAssetBase? ActiveWeather { get; init; }
        private float WeatherBlendAlpha { get; init; }
        private NetId WeatherNetId { get; init; }
        private bool WindEnabled { get; init; }
        private bool FilmGrainEnabled { get; init; }
        private EGraphicQuality SunShaftsQuality { get; init; }
        private EGraphicQuality LightingQuality { get; init; }
        private EGraphicQuality WaterQuality { get; init; }
        private bool SmoothCameraEnabled { get; init; }
        private bool CinematicModeEnabled { get; init; }
        private bool LifeUiActive { get; init; }

        public static CinematicRestoreState Capture()
        {
            LevelLighting.GetActiveWeatherNetState(
                out WeatherAssetBase activeWeather,
                out float weatherBlendAlpha,
                out NetId weatherNetId);

            return new CinematicRestoreState
            {
                Moon = LevelLighting.moon,
                Time = LevelLighting.time,
                SnowLevel = LevelLighting.snowLevel,
                SeaLevel = LevelLighting.seaLevel,
                Rainyness = LevelLighting.rainyness,
                Snowyness = LevelLighting.snowyness,
                ActiveWeather = activeWeather,
                WeatherBlendAlpha = weatherBlendAlpha,
                WeatherNetId = weatherNetId,
                WindEnabled = GraphicsSettings.IsWindEnabled,
                FilmGrainEnabled = GraphicsSettings.filmGrain,
                SunShaftsQuality = GraphicsSettings.sunShaftsQuality,
                LightingQuality = GraphicsSettings.lightingQuality,
                WaterQuality = GraphicsSettings.waterQuality,
                SmoothCameraEnabled = Player.LocalPlayer?.look.isSmoothing == true,
                CinematicModeEnabled = GraphicsSettings.WantsCinematicMode,
                LifeUiActive = PlayerLifeUI.active
            };
        }

        public void Restore()
        {
            LevelLighting.moon = Moon;
            LevelLighting.time = Time;
            LevelLighting.snowLevel = SnowLevel;
            LevelLighting.seaLevel = SeaLevel;
            LevelLighting.rainyness = Rainyness;
            LevelLighting.snowyness = Snowyness;
            LevelLighting.SetActiveWeatherAsset(ActiveWeather, WeatherBlendAlpha, WeatherNetId);
            LevelLighting.MarkParticleCloudsNeedRestart();

            GraphicsSettings.IsWindEnabled = WindEnabled;
            GraphicsSettings.filmGrain = FilmGrainEnabled;
            GraphicsSettings.sunShaftsQuality = SunShaftsQuality;
            GraphicsSettings.lightingQuality = LightingQuality;
            GraphicsSettings.waterQuality = WaterQuality;
            bool restoredCinematicMode = CinematicModeUtility.TrySetCinematicMode(CinematicModeEnabled);
            GraphicsSettings.apply("EditorHelper2 restore player cinematic graphics");
            if (restoredCinematicMode)
            {
                CinematicModeUtility.RefreshLoadedVisibility(CinematicModeEnabled);
            }

            if (Player.LocalPlayer != null)
            {
                Player.LocalPlayer.look.isSmoothing = SmoothCameraEnabled;
                if (LifeUiActive && !Player.LocalPlayer.life.isDead)
                {
                    PlayerLifeUI.open();
                }
            }
        }
    }
}
