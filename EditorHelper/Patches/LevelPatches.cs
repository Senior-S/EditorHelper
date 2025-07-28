using System;
using System.Reflection;
using EditorHelper.Editor;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Framework.Water;
using SDG.Unturned;
using UnityEngine;
using UnityEngine.Rendering;
using GraphicsSettings = SDG.Unturned.GraphicsSettings;

namespace EditorHelper.Patches;

// Credits to GamingToday093 for the idea.
[HarmonyPatch]
public class LevelPatches
{
    [HarmonyPatch(typeof(Level), "CaptureSatelliteImage")]
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
            Level.satelliteCaptureTransform.SetPositionAndRotation(position, rotation);
            Vector3 vector = mainVolume.CalculateLocalBounds().size;
            width = Mathf.CeilToInt(vector.x);
            height = Mathf.CeilToInt(vector.z);
            if (EditorHelper.Instance.EditorManager != null && EditorHelper.Instance.EditorManager.ShouldModifyResolution)
            {
                int? multiplier = EditorHelper.Instance.EditorManager.Multiplier;
                uint? customWidth = EditorHelper.Instance.EditorManager.CustomWidth;
                uint? customHeight = EditorHelper.Instance.EditorManager.CustomHeight;
                
                width = multiplier != null ? width * multiplier.Value : (int)customWidth.Value;
                height = multiplier != null ? height * multiplier.Value : (int)customHeight.Value;
                
                EditorHelper.Instance.EditorManager.ResetCustomResolution();
            }
            
            Level.satelliteCaptureCamera.aspect = vector.x / vector.z;
            Level.satelliteCaptureCamera.orthographicSize = vector.z * 0.5f;
        }
        else
        {
            width = Level.size;
            height = Level.size;
            if (EditorHelper.Instance.EditorManager != null && EditorHelper.Instance.EditorManager.ShouldModifyResolution)
            {
                int? multiplier = EditorHelper.Instance.EditorManager.Multiplier;
                uint? customWidth = EditorHelper.Instance.EditorManager.CustomWidth;
                uint? customHeight = EditorHelper.Instance.EditorManager.CustomHeight;
                
                width = multiplier != null ? width * multiplier.Value : (int)customWidth.Value;
                height = multiplier != null ? height * multiplier.Value : (int)customHeight.Value;
                
                EditorHelper.Instance.EditorManager.ResetCustomResolution();
            }
            
            Level.satelliteCaptureTransform.position = new Vector3(0f, 1028f, 0f);
            Level.satelliteCaptureTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Level.satelliteCaptureCamera.orthographicSize = Level.size / 2 - Level.border;
            Level.satelliteCaptureCamera.aspect = 1f;
        }

        RenderTexture temporary = RenderTexture.GetTemporary(width * 2, height * 2, 32);
        temporary.name = "Satellite";
        temporary.filterMode = FilterMode.Bilinear;
        Level.satelliteCaptureCamera.targetTexture = temporary;
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
        Level.SetAllObjectsAndTreesActiveForSatelliteCapture();
        //Level.onSatellitePreCapture?.Invoke();
        InvokeStaticEvent(typeof(Level), "onSatellitePreCapture");
        Level.satelliteCaptureCamera.Render();
        //Level.onSatellitePostCapture?.Invoke();
        InvokeStaticEvent(typeof(Level), "onSatellitePostCapture");
        Level.RestorePreCaptureState();
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
        Graphics.Blit(temporary, temporary2);
        RenderTexture.ReleaseTemporary(temporary);
        RenderTexture.active = temporary2;
        Texture2D texture2D = new Texture2D(width, height);
        texture2D.name = "Satellite";
        texture2D.hideFlags = HideFlags.HideAndDontSave;
        texture2D.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
        RenderTexture.ReleaseTemporary(temporary2);
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

        texture2D.Apply();
        byte[] bytes = texture2D.EncodeToPNG();
        ReadWrite.writeBytes(Level.info.path + "/Map.png", useCloud: false, usePath: false, bytes);
        UnityEngine.Object.DestroyImmediate(texture2D);

        return false;
    }

    [HarmonyPatch(typeof(Level), "CaptureChartImage")]
    [HarmonyPrefix]
    [UsedImplicitly]
    private static bool CaptureChartImage()
    {
        Bundle bundle = Bundles.getBundle(Level.info.path + "/Charts.unity3d", prependRoot: false);
		if (bundle == null)
		{
			UnturnedLog.error("Unable to load chart colors");
			return true;
		}
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
			mainVolume.GetSatelliteCaptureTransform(out var position, out var rotation);
			Level.satelliteCaptureTransform.SetPositionAndRotation(position, rotation);
			Bounds bounds = mainVolume.CalculateWorldBounds();
			terrainMinHeight = bounds.min.y;
			terrainMaxHeight = bounds.max.y;
			Vector3 vector = mainVolume.CalculateLocalBounds().size;
			float xValue = vector.x;
			float zValue = vector.z;
			if (EditorHelper.Instance.EditorManager != null && EditorHelper.Instance.EditorManager.ShouldModifyResolution)
			{
				int? multiplier = EditorHelper.Instance.EditorManager.Multiplier;
				uint? customWidth = EditorHelper.Instance.EditorManager.CustomWidth;
				uint? customHeight = EditorHelper.Instance.EditorManager.CustomHeight;
                
				xValue = multiplier != null ? xValue * multiplier.Value : (int)customWidth.Value;
				zValue = multiplier != null ? zValue * multiplier.Value : (int)customHeight.Value;
                
				EditorHelper.Instance.EditorManager.ResetCustomResolution();
			}
			imageWidth = Mathf.CeilToInt(xValue);
			imageHeight = Mathf.CeilToInt(zValue);
			captureWidth = xValue;
			captureHeight = zValue;
		}
		else
		{
			float xValue = Level.size;
			float zValue = Level.size;
			if (EditorHelper.Instance.EditorManager != null && EditorHelper.Instance.EditorManager.ShouldModifyResolution)
			{
				int? multiplier = EditorHelper.Instance.EditorManager.Multiplier;
				uint? customWidth = EditorHelper.Instance.EditorManager.CustomWidth;
				uint? customHeight = EditorHelper.Instance.EditorManager.CustomHeight;
                
				xValue = multiplier != null ? xValue * multiplier.Value : (int)customWidth.Value;
				zValue = multiplier != null ? zValue * multiplier.Value : (int)customHeight.Value;
                
				EditorHelper.Instance.EditorManager.ResetCustomResolution();
			}
			
			imageWidth = Mathf.CeilToInt(xValue);
			imageHeight = Mathf.CeilToInt(zValue);
			captureWidth = Level.size - Level.border * 2f;
			captureHeight = Level.size - Level.border * 2f;
			Level.satelliteCaptureTransform.position = new Vector3(0f, 1028f, 0f);
			Level.satelliteCaptureTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
			terrainMinHeight = WaterVolumeManager.worldSeaLevel;
			terrainMaxHeight = Level.TERRAIN;
		}
		Texture2D texture2D = new Texture2D(imageWidth, imageHeight);
		texture2D.name = "Chart";
		texture2D.hideFlags = HideFlags.HideAndDontSave;
		Level.SetAllObjectsAndTreesActiveForSatelliteCapture();
		GameObject terrainGO = new GameObject();
		terrainGO.layer = 20;
		for (int i = 0; i < imageWidth; i++)
		{
			for (int j = 0; j < imageHeight; j++)
			{
				Color color = GetColor((float)i + 0.25f, (float)j + 0.25f) * 0.25f + GetColor((float)i + 0.25f, (float)j + 0.75f) * 0.25f + GetColor((float)i + 0.75f, (float)j + 0.25f) * 0.25f + GetColor((float)i + 0.75f, (float)j + 0.75f) * 0.25f;
				color.a = 1f;
				texture2D.SetPixel(i, j, color);
			}
		}
		texture2D.Apply();
		Level.RestorePreCaptureState();
		byte[] bytes = texture2D.EncodeToPNG();
		ReadWrite.writeBytes(Level.info.path + "/Chart.png", useCloud: false, usePath: false, bytes);
		UnityEngine.Object.DestroyImmediate(texture2D);
		Color GetColor(float x, float y)
		{
			float num = x / (float)imageWidth;
			float num2 = y / (float)imageHeight;
			Vector3 position2 = new Vector3((num - 0.5f) * captureWidth, (num2 - 0.5f) * captureHeight, 0f);
			Vector3 vector2 = Level.satelliteCaptureTransform.TransformPoint(position2);
			Level.FindChartHit(vector2, out var chart, out var hit);
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