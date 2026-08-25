using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.common.Helpers;

/// <summary>
/// Weather presets supported by the cinematic controls.
/// </summary>
public enum CinematicWeatherMode
{
    None,
    NormalRain,
    Thunder,
    Snow
}

/// <summary>
/// Provides shared cinematic graphics, weather, and level-visibility operations.
/// </summary>
public static class CinematicModeUtility
{
    private const string HeavyRainWeatherGuid = "6c850687bdb947a689fa8de8a8d99afb";

    private static readonly FieldInfo? CinematicModeFlagField = typeof(GraphicsSettings)
        .GetField("clEnableCinematicMode", BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly FieldInfo? RoadRegionSegmentRenderersField = typeof(LevelRoads)
        .GetField("regionSegmentRenderers", BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly MethodInfo? ResourceSpawnpointUpdateActiveMethod = typeof(ResourceSpawnpoint)
        .GetMethod("UpdateActive", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly AssetReference<WeatherAssetBase> HeavyRainWeatherReference = new(HeavyRainWeatherGuid);

    /// <summary>
    /// Graphics quality states supported by the cinematic quality controls.
    /// </summary>
    public static readonly EGraphicQuality[] GraphicQualityStates =
    [
        EGraphicQuality.OFF,
        EGraphicQuality.LOW,
        EGraphicQuality.MEDIUM,
        EGraphicQuality.HIGH,
        EGraphicQuality.ULTRA
    ];

    /// <summary>
    /// Water quality states supported by the cinematic quality controls.
    /// </summary>
    public static readonly EGraphicQuality[] WaterQualityStates =
    [
        EGraphicQuality.LOW,
        EGraphicQuality.MEDIUM,
        EGraphicQuality.HIGH,
        EGraphicQuality.ULTRA
    ];

    /// <summary>
    /// Gets the cinematic preset matching the active weather asset.
    /// </summary>
    /// <returns>The matching weather preset.</returns>
    public static CinematicWeatherMode GetCurrentWeatherMode()
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

        return WeatherAssetBase.DEFAULT_SNOW.isReferenceTo(activeWeather)
            ? CinematicWeatherMode.Snow
            : CinematicWeatherMode.None;
    }

    /// <summary>
    /// Finds the weather asset associated with a cinematic preset.
    /// </summary>
    /// <param name="mode">The weather preset.</param>
    /// <returns>The matching weather asset, or <see langword="null"/> for no weather or a missing asset.</returns>
    public static WeatherAssetBase? FindWeatherAsset(CinematicWeatherMode mode)
    {
        return mode switch
        {
            CinematicWeatherMode.NormalRain => WeatherAssetBase.DEFAULT_RAIN.Find(),
            CinematicWeatherMode.Thunder => HeavyRainWeatherReference.Find(),
            CinematicWeatherMode.Snow => WeatherAssetBase.DEFAULT_SNOW.Find(),
            _ => null
        };
    }

    /// <summary>
    /// Updates Unturned's cinematic graphics flag.
    /// </summary>
    /// <param name="value">Whether cinematic rendering should be enabled.</param>
    /// <returns><see langword="true"/> when the flag was found and updated.</returns>
    public static bool TrySetCinematicMode(bool value)
    {
        if (CinematicModeFlagField?.GetValue(null) is not CommandLineFlag flag)
        {
            return false;
        }

        flag.value = value;
        return true;
    }

    /// <summary>
    /// Finds a quality value in the supplied control states.
    /// </summary>
    /// <param name="qualities">The available quality states.</param>
    /// <param name="quality">The current quality value.</param>
    /// <returns>The matching index, or zero when the value is unavailable.</returns>
    public static int IndexOfQuality(EGraphicQuality[] qualities, EGraphicQuality quality)
    {
        int index = Array.IndexOf(qualities, quality);
        return index >= 0 ? index : 0;
    }

    /// <summary>
    /// Formats a graphics quality value for display.
    /// </summary>
    /// <param name="quality">The quality value.</param>
    /// <returns>The display name.</returns>
    public static string FormatQualityName(EGraphicQuality quality)
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

    /// <summary>
    /// Refreshes loaded objects, resources, and roads after changing cinematic rendering.
    /// </summary>
    /// <param name="cinematicEnabled">Whether cinematic rendering is enabled.</param>
    public static void RefreshLoadedVisibility(bool cinematicEnabled)
    {
        List<LevelObject>[,]? objects = LevelObjects.objects;
        if (objects != null)
        {
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

        if (ResourceSpawnpointUpdateActiveMethod != null)
        {
            List<ResourceSpawnpoint> resources = [];
            LevelGround.GatherAllTrees(resources);
            foreach (ResourceSpawnpoint resource in resources)
            {
                ResourceSpawnpointUpdateActiveMethod.Invoke(resource, null);
            }
        }

        if (!cinematicEnabled)
        {
            LevelRoads.ImmediatelySyncRegionalVisibility();
            return;
        }

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
}

/// <summary>
/// Applies temporary cinematic post-process settings and restores their original values.
/// </summary>
public sealed class CinematicPostProcessOverrides
{
    private const float DofFocusMin = 1f;
    private const float DofFocusMax = 200f;
    private const float DofApertureMin = 1f;
    private const float DofApertureMax = 32f;
    private const float VignetteMax = 0.75f;
    private const float ExposureMin = -2f;
    private const float ExposureMax = 2f;
    private const float GradeMin = -100f;
    private const float GradeMax = 100f;

    private static readonly FieldInfo? BaseProfileField = typeof(UnturnedPostProcess)
        .GetField("baseProfile", BindingFlags.Instance | BindingFlags.NonPublic);

    private object? _depthOfField;
    private object? _vignette;
    private object? _colorGrading;
    private bool _depthOfFieldActive;
    private bool _vignetteActive;
    private bool _colorGradingActive;
    private FloatParameterState _focusDistance;
    private FloatParameterState _aperture;
    private FloatParameterState _vignetteIntensity;
    private FloatParameterState _hueShift;
    private FloatParameterState _exposure;
    private FloatParameterState _contrast;
    private FloatParameterState _saturation;
    private FloatParameterState _temperature;
    private FloatParameterState _tint;
    private bool _hasOriginalState;

    /// <summary>
    /// Applies the supplied cinematic post-process settings.
    /// </summary>
    /// <param name="depthOfFieldEnabled">Whether depth of field is enabled.</param>
    /// <param name="focusDistance">The depth-of-field focus distance.</param>
    /// <param name="aperture">The depth-of-field aperture.</param>
    /// <param name="vignetteIntensity">The vignette intensity.</param>
    /// <param name="exposure">The color-grade exposure.</param>
    /// <param name="contrast">The color-grade contrast.</param>
    /// <param name="saturation">The color-grade saturation.</param>
    /// <param name="temperature">The color-grade temperature.</param>
    /// <param name="tint">The color-grade tint.</param>
    public void Apply(
        bool depthOfFieldEnabled,
        float focusDistance,
        float aperture,
        float vignetteIntensity,
        float exposure,
        float contrast,
        float saturation,
        float temperature,
        float tint)
    {
        object? baseProfile = GetBaseProfile();
        if (baseProfile == null)
        {
            return;
        }

        if (!_hasOriginalState)
        {
            _depthOfField = GetSetting(baseProfile, "dof");
            _vignette = GetSetting(baseProfile, "vignette");
            _colorGrading = GetSetting(baseProfile, "colorGrading");
            _depthOfFieldActive = GetActive(_depthOfField);
            _vignetteActive = GetActive(_vignette);
            _colorGradingActive = GetActive(_colorGrading);
            _focusDistance = CaptureFloat(_depthOfField, "focusDistance");
            _aperture = CaptureFloat(_depthOfField, "aperture");
            _vignetteIntensity = CaptureFloat(_vignette, "intensity");
            _hueShift = CaptureFloat(_colorGrading, "hueShift");
            _exposure = CaptureFloat(_colorGrading, "postExposure");
            _contrast = CaptureFloat(_colorGrading, "contrast");
            _saturation = CaptureFloat(_colorGrading, "saturation");
            _temperature = CaptureFloat(_colorGrading, "temperature");
            _tint = CaptureFloat(_colorGrading, "tint");
            _hasOriginalState = true;
        }

        SetActive(_depthOfField, depthOfFieldEnabled);
        if (depthOfFieldEnabled)
        {
            OverrideFloat(_depthOfField, "focusDistance", Mathf.Clamp(focusDistance, DofFocusMin, DofFocusMax));
            OverrideFloat(_depthOfField, "aperture", Mathf.Clamp(aperture, DofApertureMin, DofApertureMax));
        }

        float clampedVignette = Mathf.Clamp(vignetteIntensity, 0f, VignetteMax);
        SetActive(_vignette, clampedVignette > 0.001f);
        OverrideFloat(_vignette, "intensity", clampedVignette);

        bool hasColorGrade = IsNonZero(exposure)
                             || IsNonZero(contrast)
                             || IsNonZero(saturation)
                             || IsNonZero(temperature)
                             || IsNonZero(tint);
        SetActive(_colorGrading, hasColorGrade);
        OverrideFloat(_colorGrading, "hueShift", 0f);
        OverrideFloat(_colorGrading, "postExposure", Mathf.Clamp(exposure, ExposureMin, ExposureMax));
        OverrideFloat(_colorGrading, "contrast", Mathf.Clamp(contrast, GradeMin, GradeMax));
        OverrideFloat(_colorGrading, "saturation", Mathf.Clamp(saturation, GradeMin, GradeMax));
        OverrideFloat(_colorGrading, "temperature", Mathf.Clamp(temperature, GradeMin, GradeMax));
        OverrideFloat(_colorGrading, "tint", Mathf.Clamp(tint, GradeMin, GradeMax));
    }

    /// <summary>
    /// Restores the post-process settings captured before the first application.
    /// </summary>
    public void Reset()
    {
        if (!_hasOriginalState)
        {
            return;
        }

        SetActive(_depthOfField, _depthOfFieldActive);
        SetActive(_vignette, _vignetteActive);
        SetActive(_colorGrading, _colorGradingActive);
        RestoreFloat(_focusDistance);
        RestoreFloat(_aperture);
        RestoreFloat(_vignetteIntensity);
        RestoreFloat(_hueShift);
        RestoreFloat(_exposure);
        RestoreFloat(_contrast);
        RestoreFloat(_saturation);
        RestoreFloat(_temperature);
        RestoreFloat(_tint);
        _hasOriginalState = false;
    }

    private static object? GetBaseProfile()
    {
        return UnturnedPostProcess.instance == null
            ? null
            : BaseProfileField?.GetValue(UnturnedPostProcess.instance);
    }

    private static object? GetSetting(object profileWrapper, string fieldName)
    {
        return profileWrapper.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(profileWrapper);
    }

    private static bool GetActive(object? setting)
    {
        return GetMemberValue(setting, "active") is bool active && active;
    }

    private static void SetActive(object? setting, bool active)
    {
        SetMemberValue(setting, "active", active);
    }

    private static FloatParameterState CaptureFloat(object? setting, string parameterName)
    {
        object? parameter = GetMemberValue(setting, parameterName);
        float value = GetMemberValue(parameter, "value") is float currentValue ? currentValue : 0f;
        bool overrideState = GetMemberValue(parameter, "overrideState") is bool currentOverrideState && currentOverrideState;
        return new FloatParameterState(parameter, value, overrideState);
    }

    private static void OverrideFloat(object? setting, string parameterName, float value)
    {
        object? parameter = GetMemberValue(setting, parameterName);
        parameter?.GetType().GetMethod("Override", [typeof(float)])?.Invoke(parameter, [value]);
    }

    private static void RestoreFloat(FloatParameterState state)
    {
        if (state.Parameter == null)
        {
            return;
        }

        state.Parameter.GetType().GetMethod("Override", [typeof(float)])?.Invoke(state.Parameter, [state.Value]);
        SetMemberValue(state.Parameter, "overrideState", state.OverrideState);
    }

    private static object? GetMemberValue(object? instance, string memberName)
    {
        if (instance == null)
        {
            return null;
        }

        Type type = instance.GetType();
        return type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                   ?.GetValue(instance)
               ?? type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                   ?.GetValue(instance);
    }

    private static void SetMemberValue(object? instance, string memberName, object value)
    {
        if (instance == null)
        {
            return;
        }

        Type type = instance.GetType();
        FieldInfo? field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(instance, value);
            return;
        }

        type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(instance, value);
    }

    private static bool IsNonZero(float value)
    {
        return Mathf.Abs(value) > 0.001f;
    }

    private readonly struct FloatParameterState(object? parameter, float value, bool overrideState)
    {
        internal object? Parameter { get; } = parameter;
        internal float Value { get; } = value;
        internal bool OverrideState { get; } = overrideState;
    }
}
