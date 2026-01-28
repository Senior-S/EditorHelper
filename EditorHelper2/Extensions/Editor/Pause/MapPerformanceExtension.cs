using DanielWillett.UITools.API.Extensions;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.Loader;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace EditorHelper2.Extensions.Editor.Pause;

/// <remarks>
/// This does not really have to inherit from UIExtension as it does not affect any UI. 
/// But it is a current Requirement for all Extensions due to how <see cref="ExtensionManager.TryGetInstance{T}(out T?)"/> works
/// </remarks>
[EHExtension("Map Performance Extension", "Gamingtoday093", true)]
[UIExtension(typeof(EditorPauseUI))]
public class MapPerformanceExtension : UIExtension, IExtension
{
    private AssetBundle? _satelliteShaderBundle;
    public Material? SatelliteImageShader { get; private set; } 

    public MapPerformanceExtension() => Initialize();

    public void Initialize()
    {
        LoadSatelliteImageShader();
    }

    private void LoadSatelliteImageShader()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string bundlePath = Path.Combine(Path.GetDirectoryName(assembly.Location) ?? string.Empty, "Assets", "SatelliteShader.unity3d");

        _satelliteShaderBundle = AssetBundle.LoadFromFile(bundlePath);
        if (_satelliteShaderBundle == null)
        {
            UnturnedLog.info("[Map Performance Extension] Failed to load satellite shader bundle");
            return;
        }

        Shader? satelliteShader = _satelliteShaderBundle.LoadAsset<Shader>("assets/resources/editorhelper/removealpha.shader");
        if (satelliteShader == null)
        {
            UnturnedLog.info("[Map Performance Extension] Failed to load assets/resources/editorhelper/removealpha.shader shader from bundle");
            return;
        }

        SatelliteImageShader = new Material(satelliteShader);
    }

    public void Dispose()
    {
        SatelliteImageShader = null;
        _satelliteShaderBundle?.Unload(true);
    }
}
