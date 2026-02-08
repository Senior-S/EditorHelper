using EditorHelper2.common.Types;
using EditorHelper2.Extensions.Editor.Pause;
using EditorHelper2.Loader;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Framework.Water;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using GraphicsSettings = SDG.Unturned.GraphicsSettings;
using Object = UnityEngine.Object;

namespace EditorHelper2.Patches.Level;

[HarmonyPatch(typeof(SDG.Unturned.Level))]
public class LevelPatches
{
    [HarmonyPatch(typeof(SDG.Unturned.Level), "CaptureSatelliteImage")]
    [HarmonyPrefix]
    [UsedImplicitly]
    private static bool CaptureSatelliteImage()
    {
        CartographyVolume mainVolume = VolumeManager<CartographyVolume, CartographyVolumeManager>.Get().GetMainVolume();
        int width;
        int height;
        if (mainVolume != null)
        {
            mainVolume.GetSatelliteCaptureTransform(out Vector3 position, out Quaternion rotation);
            SDG.Unturned.Level.satelliteCaptureTransform.SetPositionAndRotation(position, rotation);
            Vector3 vector = mainVolume.CalculateLocalBounds().size;
            width = Mathf.CeilToInt(vector.x);
            height = Mathf.CeilToInt(vector.z);
            #region Satellite Dimensions Patch
            if (ExtensionManager.TryGetInstance(out MapResolutionExtension? mapResolutionExtension) && mapResolutionExtension != null &&
                mapResolutionExtension.ShouldModifyResolution)
            {
                int? multiplier = mapResolutionExtension.Multiplier;
                MapResolution defaultResolution = new((uint)width, (uint)height);
                MapResolution customResolution = mapResolutionExtension.CustomResolution;

                width = (int)(
                    customResolution.Width > 0 ? customResolution.Width :
                    customResolution.Height > 0 ? customResolution.GetAspectWidth(defaultResolution) :
                    defaultResolution.Width);
                height = (int)(
                    customResolution.Height > 0 ? customResolution.Height :
                    customResolution.Width > 0 ? customResolution.GetAspectHeight(defaultResolution) :
                    defaultResolution.Height);

                if (multiplier != null)
                {
                    width *= multiplier.Value;
                    height *= multiplier.Value;
                }

                mapResolutionExtension.ResetCustomResolution();
            }
            #endregion

            SDG.Unturned.Level.satelliteCaptureCamera.aspect = vector.x / vector.z;
            SDG.Unturned.Level.satelliteCaptureCamera.orthographicSize = vector.z * 0.5f;
        }
        else
        {
            width = SDG.Unturned.Level.size;
            height = SDG.Unturned.Level.size;
            #region Satellite Dimensions Patch
            if (ExtensionManager.TryGetInstance(out MapResolutionExtension? mapResolutionExtension) && mapResolutionExtension != null &&
                mapResolutionExtension.ShouldModifyResolution)
            {
                int? multiplier = mapResolutionExtension.Multiplier;
                MapResolution defaultResolution = new((uint)width, (uint)height);
                MapResolution customResolution = mapResolutionExtension.CustomResolution;

                width = (int)(
                    customResolution.Width > 0 ? customResolution.Width :
                    customResolution.Height > 0 ? customResolution.GetAspectWidth(defaultResolution) :
                    defaultResolution.Width);
                height = (int)(
                    customResolution.Height > 0 ? customResolution.Height :
                    customResolution.Width > 0 ? customResolution.GetAspectHeight(defaultResolution) :
                    defaultResolution.Height);

                if (multiplier != null)
                {
                    width *= multiplier.Value;
                    height *= multiplier.Value;
                }

                mapResolutionExtension.ResetCustomResolution();
            }
            #endregion

            SDG.Unturned.Level.satelliteCaptureTransform.position = new Vector3(0f, 1028f, 0f);
            SDG.Unturned.Level.satelliteCaptureTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
            SDG.Unturned.Level.satelliteCaptureCamera.orthographicSize = SDG.Unturned.Level.size / 2 - SDG.Unturned.Level.border;
            SDG.Unturned.Level.satelliteCaptureCamera.aspect = 1f;
        }

        RenderTexture temporary = RenderTexture.GetTemporary(width * 2, height * 2, 32);
        temporary.name = "Satellite";
        temporary.filterMode = FilterMode.Bilinear;
        SDG.Unturned.Level.satelliteCaptureCamera.targetTexture = temporary;
        bool fog = RenderSettings.fog;
        AmbientMode ambientMode = RenderSettings.ambientMode;
        Color ambientSkyColor = RenderSettings.ambientSkyColor;
        Color ambientEquatorColor = RenderSettings.ambientEquatorColor;
        Color ambientGroundColor = RenderSettings.ambientGroundColor;
        float lodBias = QualitySettings.lodBias;
        float seaFloat = LevelLighting.getSeaFloat("_Shininess");
        Color seaColor = LevelLighting.getSeaColor("_SpecularColor");
        ERenderMode renderMode = GraphicsSettings.renderMode;
        GraphicsSettings.renderMode = ERenderMode.FORWARD;
        GraphicsSettings.apply("capturing satellite");
        RenderSettings.fog = false;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Palette.AMBIENT;
        RenderSettings.ambientEquatorColor = Palette.AMBIENT;
        RenderSettings.ambientGroundColor = Palette.AMBIENT;
        LevelLighting.setSeaFloat("_Shininess", 500f);
        LevelLighting.setSeaColor("_SpecularColor", Color.black);
        QualitySettings.lodBias = float.MaxValue;
        SDG.Unturned.Level.SetAllObjectsAndTreesActiveForSatelliteCapture();
        //Level.onSatellitePreCapture?.Invoke();
        InvokeStaticEvent(typeof(SDG.Unturned.Level), "onSatellitePreCapture");
        SDG.Unturned.Level.satelliteCaptureCamera.Render();
        //Level.onSatellitePostCapture?.Invoke();
        InvokeStaticEvent(typeof(SDG.Unturned.Level), "onSatellitePostCapture");
        SDG.Unturned.Level.RestorePreCaptureState();
        GraphicsSettings.renderMode = renderMode;
        GraphicsSettings.apply("finished capturing satellite");
        RenderSettings.fog = fog;
        RenderSettings.ambientMode = ambientMode;
        RenderSettings.ambientSkyColor = ambientSkyColor;
        RenderSettings.ambientEquatorColor = ambientEquatorColor;
        RenderSettings.ambientGroundColor = ambientGroundColor;
        LevelLighting.setSeaFloat("_Shininess", seaFloat);
        LevelLighting.setSeaColor("_SpecularColor", seaColor);
        QualitySettings.lodBias = lodBias;
        RenderTexture temporary2 = RenderTexture.GetTemporary(width, height);
        #region Shader Performance Patch
        bool shouldCPUWriteAlpha = true;
        if (ExtensionManager.TryGetInstance(out MapPerformanceExtension? mapPerformanceExtension) &&
            mapPerformanceExtension.SatelliteImageShader != null)
        {
            Graphics.Blit(temporary, temporary2, mapPerformanceExtension.SatelliteImageShader);
            shouldCPUWriteAlpha = false; // SatelliteImageShader has already applied this
        }
        else
        {
            Graphics.Blit(temporary, temporary2);
        }
        #endregion
        RenderTexture.ReleaseTemporary(temporary);
        RenderTexture.active = temporary2;
        Texture2D texture2D = new Texture2D(width, height);
        texture2D.name = "Satellite";
        texture2D.hideFlags = HideFlags.HideAndDontSave;
        texture2D.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
        RenderTexture.ReleaseTemporary(temporary2);
        if (shouldCPUWriteAlpha)
        {
            for (int i = 0; i < texture2D.width; i++)
            {
                for (int j = 0; j < texture2D.height; j++)
                {
                    Color pixel = texture2D.GetPixel(i, j);
                    if (pixel.a < 1f)
                    {
                        pixel.a = 1f;
                        texture2D.SetPixel(i, j, pixel);
                    }
                }
            }
        }

        texture2D.Apply();
        byte[] bytes = texture2D.EncodeToPNG();
        ReadWrite.writeBytes(SDG.Unturned.Level.info.path + "/Map.png", useCloud: false, usePath: false, bytes);
        Object.DestroyImmediate(texture2D);

        return false;
    }

    [HarmonyPatch(typeof(SDG.Unturned.Level), "CaptureChartImage")]
    [HarmonyPrefix]
    [UsedImplicitly]
    private static bool CaptureChartImage()
    {
        Bundle bundle = Bundles.getBundle(SDG.Unturned.Level.info.path + "/Charts.unity3d", prependRoot: false);
        if (bundle == null)
        {
            UnturnedLog.error("Unable to load chart colors");
            return true;
        }

        Stopwatch stopwatch = new();
        stopwatch.Start();

        Texture2D heightStrip = bundle.load<Texture2D>("Height_Strip");
        Texture2D layerStrip = bundle.load<Texture2D>("Layer_Strip");
        bundle.unload();
        if (heightStrip == null || layerStrip == null)
        {
            UnturnedLog.error("Unable to find height and layer strip textures");
            return true;
        }

        CartographyVolume mainVolume = VolumeManager<CartographyVolume, CartographyVolumeManager>.Get().GetMainVolume();
        float terrainMinHeight;
        float terrainMaxHeight;
        int imageWidth;
        int imageHeight;
        float captureWidth;
        float captureHeight;
        if (mainVolume != null)
        {
            mainVolume.GetSatelliteCaptureTransform(out Vector3 position, out Quaternion rotation);
            SDG.Unturned.Level.satelliteCaptureTransform.SetPositionAndRotation(position, rotation);
            Bounds bounds = mainVolume.CalculateWorldBounds();
            terrainMinHeight = bounds.min.y;
            terrainMaxHeight = bounds.max.y;
            Vector3 vector = mainVolume.CalculateLocalBounds().size;
            imageWidth = Mathf.CeilToInt(vector.x);
            imageHeight = Mathf.CeilToInt(vector.z);
            #region Chart Dimensions Patch
            if (ExtensionManager.TryGetInstance(out MapResolutionExtension? mapResolutionExtension) && mapResolutionExtension != null &&
                mapResolutionExtension.ShouldModifyResolution)
            {
                int? multiplier = mapResolutionExtension.Multiplier;
                MapResolution defaultResolution = new((uint)imageWidth, (uint)imageHeight);
                MapResolution customResolution = mapResolutionExtension.CustomResolution;

                imageWidth = (int)(
                    customResolution.Width > 0 ? customResolution.Width :
                    customResolution.Height > 0 ? customResolution.GetAspectWidth(defaultResolution) :
                    defaultResolution.Width);
                imageHeight = (int)(
                    customResolution.Height > 0 ? customResolution.Height :
                    customResolution.Width > 0 ? customResolution.GetAspectHeight(defaultResolution) :
                    defaultResolution.Height);

                if (multiplier != null)
                {
                    imageWidth *= multiplier.Value;
                    imageHeight *= multiplier.Value;
                }

                mapResolutionExtension.ResetCustomResolution();
            }
            #endregion

            captureWidth = vector.x;
            captureHeight = vector.z;
        }
        else
        {
            imageWidth = SDG.Unturned.Level.size;
            imageHeight = SDG.Unturned.Level.size;
            #region Chart Dimensions Patch
            if (ExtensionManager.TryGetInstance(out MapResolutionExtension? mapResolutionExtension) && mapResolutionExtension != null &&
                mapResolutionExtension.ShouldModifyResolution)
            {
                int? multiplier = mapResolutionExtension.Multiplier;
                MapResolution defaultResolution = new((uint)imageWidth, (uint)imageHeight);
                MapResolution customResolution = mapResolutionExtension.CustomResolution;

                imageWidth = (int)(
                    customResolution.Width > 0 ? customResolution.Width :
                    customResolution.Height > 0 ? customResolution.GetAspectWidth(defaultResolution) :
                    defaultResolution.Width);
                imageHeight = (int)(
                    customResolution.Height > 0 ? customResolution.Height :
                    customResolution.Width > 0 ? customResolution.GetAspectHeight(defaultResolution) :
                    defaultResolution.Height);

                if (multiplier != null)
                {
                    imageWidth *= multiplier.Value;
                    imageHeight *= multiplier.Value;
                }

                mapResolutionExtension.ResetCustomResolution();
            }
            #endregion

            captureWidth = SDG.Unturned.Level.size - SDG.Unturned.Level.border * 2f;
            captureHeight = SDG.Unturned.Level.size - SDG.Unturned.Level.border * 2f;
            SDG.Unturned.Level.satelliteCaptureTransform.position = new Vector3(0f, 1028f, 0f);
            SDG.Unturned.Level.satelliteCaptureTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
            terrainMinHeight = WaterVolumeManager.worldSeaLevel;
            terrainMaxHeight = SDG.Unturned.Level.TERRAIN;
        }

        Texture2D texture2D = new Texture2D(imageWidth, imageHeight);
        texture2D.name = "Chart";
        texture2D.hideFlags = HideFlags.HideAndDontSave;
        SDG.Unturned.Level.SetAllObjectsAndTreesActiveForSatelliteCapture();
        GameObject terrainGO = new GameObject();
        terrainGO.layer = 20;
        #region Chart Performance Patch
        if (ExtensionManager.TryGetInstance<MapPerformanceExtension>(out _))
        {
            // Could be GetPixelData<Color32>() to be even faster but I am not sure if all textures will support that
            Color[] heightPixels = heightStrip.GetPixels();
            Color[] layerPixels = layerStrip.GetPixels();

            Color[] pixels = new Color[imageHeight];

            // Yes, this is faster because Unturned does has to loop through every object in a region when finding the ObjectAsset so caching is really helpful for Big Objects
            Dictionary<Transform, ObjectAsset?> transformObjectAsset = new(4096);
            Dictionary<Transform, ResourceAsset?> transformResourceAsset = new(4096); // Seems to also be faster

            var commands = new NativeArray<RaycastCommand>(imageHeight * 4, Allocator.TempJob);
            var results = new NativeArray<RaycastHit>(imageHeight * 4, Allocator.TempJob);
            try
            {
                Matrix4x4 matrix = SDG.Unturned.Level.satelliteCaptureTransform.localToWorldMatrix;
                for (int x = 0; x < imageWidth; x++)
                {
                    var createCommandsJob = new CreateRaycastsJob()
                    {
                        Commands = commands,

                        ImageWidth = imageWidth,
                        ImageHeight = imageHeight,

                        CaptureWidth = captureWidth,
                        CaptureHeight = captureHeight,

                        X = x,
                        
                        LocalToWorldMatrix = matrix
                    };

                    createCommandsJob.Schedule(imageHeight, 16).Complete();

                    RaycastCommand.ScheduleBatch(commands, results, 64).Complete();

                    for (int y = 0; y < imageHeight; y++)
                    {
                        RaycastHit hit1 = results[4 * y + 0];
                        RaycastHit hit2 = results[4 * y + 1];
                        RaycastHit hit3 = results[4 * y + 2];
                        RaycastHit hit4 = results[4 * y + 3];

                        Color color =
                            GetColorFromHit(ref hit1) * 0.25f +
                            GetColorFromHit(ref hit2) * 0.25f +
                            GetColorFromHit(ref hit3) * 0.25f +
                            GetColorFromHit(ref hit4) * 0.25f;

                        color.a = 1f;
                        pixels[y] = color;
                    }
                    texture2D.SetPixels(x, 0, 1, imageHeight, pixels);
                }
            }
            finally
            {
                commands.Dispose();
                results.Dispose();
            }

            Color GetColorFromHit(ref RaycastHit hit)
            {
                EObjectChart objectChart = GetObjectChartFromHit(ref hit);

                Transform? transform = hit.transform;
                Vector3 point = hit.point;
                int layerIndex = LayerMasks.GROUND;
                if (transform != null) layerIndex = transform.gameObject.layer;
                else point.y = LevelGround.getHeight(point);

                switch (objectChart)
                {
                    case EObjectChart.GROUND:
                        layerIndex = LayerMasks.GROUND;
                        break;
                    case EObjectChart.HIGHWAY:
                        layerIndex = 0;
                        break;
                    case EObjectChart.ROAD:
                        layerIndex = 1;
                        break;
                    case EObjectChart.STREET:
                        layerIndex = 2;
                        break;
                    case EObjectChart.PATH:
                        layerIndex = 3;
                        break;
                    case EObjectChart.LARGE:
                        layerIndex = 15;
                        break;
                    case EObjectChart.MEDIUM:
                        layerIndex = 16;
                        break;
                    case EObjectChart.CLIFF:
                        layerIndex = 4;
                        break;
                    case EObjectChart.WATER:
                        return heightPixels[0];
                }

                if (layerIndex == LayerMasks.GROUND)
                {
                    if (WaterUtility.isPointUnderwater(point)) return heightPixels[0];

                    float num4 = Mathf.InverseLerp(terrainMinHeight, terrainMaxHeight, point.y);
                    return heightPixels[(int)(num4 * (float)(heightStrip.width - 1)) + 1];
                }

                return layerPixels[layerIndex];
            }

            EObjectChart GetObjectChartFromHit(ref RaycastHit hit)
            {
                EObjectChart objectChart = EObjectChart.NONE;
                Transform? transform = hit.transform;
                if (transform == null) return objectChart;
                
                if (!transformObjectAsset.TryGetValue(transform.root, out ObjectAsset? objectAsset))
                {
                    objectAsset = LevelObjects.getAsset(transform);
                    if (objectAsset != null) transformObjectAsset.Add(transform.root, objectAsset);
                }
                if (objectAsset != null) objectChart = objectAsset.chart;
                else
                {
                    if (!transformResourceAsset.TryGetValue(transform.root, out ResourceAsset? resourceAsset))
                    {
                        resourceAsset = LevelGround.FindResourceSpawnpointByTransform(transform)?.asset;
                        if (resourceAsset != null) transformResourceAsset.Add(transform.root, resourceAsset);
                    }
                    if (resourceAsset != null) objectChart = resourceAsset.chart;
                    else if (transform.gameObject.layer == LayerMasks.ENVIRONMENT)
                    {
                        Road? road = LevelRoads.FindRoadByRootTransform(transform.root);
                        if (road != null) objectChart = road.GetChartMode();
                    }
                }

                if (objectChart == EObjectChart.IGNORE)
                {
                    SDG.Unturned.Level.FindChartHit(hit.point + new Vector3(0f, -0.01f, 0f), out objectChart, out hit);
                }

                return objectChart;
            }
        }
        else
        {
            for (int i = 0; i < imageWidth; i++)
            {
                for (int j = 0; j < imageHeight; j++)
                {
                    Color color = GetColor((float)i + 0.25f, (float)j + 0.25f) * 0.25f + GetColor((float)i + 0.25f, (float)j + 0.75f) * 0.25f +
                                  GetColor((float)i + 0.75f, (float)j + 0.25f) * 0.25f + GetColor((float)i + 0.75f, (float)j + 0.75f) * 0.25f;
                    color.a = 1f;
                    texture2D.SetPixel(i, j, color);
                }
            }
        }
        #endregion

        texture2D.Apply();
        SDG.Unturned.Level.RestorePreCaptureState();
        byte[] bytes = texture2D.EncodeToPNG();
        ReadWrite.writeBytes(SDG.Unturned.Level.info.path + "/Chart.png", useCloud: false, usePath: false, bytes);
        Object.DestroyImmediate(texture2D);
        
        stopwatch.Stop();
        UnturnedLog.info($"[Chart] Unity.mathematics use: {stopwatch.ElapsedMilliseconds} ms");

        Color GetColor(float x, float y)
        {
            float num = x / (float)imageWidth;
            float num2 = y / (float)imageHeight;
            Vector3 position2 = new Vector3((num - 0.5f) * captureWidth, (num2 - 0.5f) * captureHeight, 0f);
            Vector3 vector2 = SDG.Unturned.Level.satelliteCaptureTransform.TransformPoint(position2);
            SDG.Unturned.Level.FindChartHit(vector2, out EObjectChart chart, out RaycastHit hit);
            Transform transform = hit.transform;
            Vector3 point = hit.point;
            if (transform == null)
            {
                transform = terrainGO.transform;
                point = vector2;
                point.y = LevelGround.getHeight(vector2);
            }

            int num3 = transform.gameObject.layer;
            switch (chart)
            {
                case EObjectChart.GROUND:
                    num3 = 20;
                    break;
                case EObjectChart.HIGHWAY:
                    num3 = 0;
                    break;
                case EObjectChart.ROAD:
                    num3 = 1;
                    break;
                case EObjectChart.STREET:
                    num3 = 2;
                    break;
                case EObjectChart.PATH:
                    num3 = 3;
                    break;
                case EObjectChart.LARGE:
                    num3 = 15;
                    break;
                case EObjectChart.MEDIUM:
                    num3 = 16;
                    break;
                case EObjectChart.CLIFF:
                    num3 = 4;
                    break;
            }

            if (chart == EObjectChart.WATER)
            {
                return heightStrip.GetPixel(0, 0);
            }

            if (num3 == 20)
            {
                if (WaterUtility.isPointUnderwater(point))
                {
                    return heightStrip.GetPixel(0, 0);
                }

                float num4 = Mathf.InverseLerp(terrainMinHeight, terrainMaxHeight, point.y);
                return heightStrip.GetPixel((int)(num4 * (float)(heightStrip.width - 1)) + 1, 0);
            }

            return layerStrip.GetPixel(num3, 0);
        }

        return false;
    }

    private static void InvokeStaticEvent(Type classType, string eventName)
    {
        try
        {
            if (classType == null) return;
            FieldInfo? eventField = classType.GetField(eventName, BindingFlags.Static | BindingFlags.NonPublic);
            if (eventField == null) return;
            Delegate eventDelegate = (Delegate)eventField.GetValue(null);

            eventDelegate?.DynamicInvoke();
        }
        catch (Exception ex)
        {
            UnturnedLog.error($"Error invoking event '{eventName}': {ex.Message}");
        }
    }
}