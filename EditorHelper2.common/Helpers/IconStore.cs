using SDG.Unturned;
using SDG.Framework.Foliage;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EditorHelper2.common.Helpers;

public class IconStore
{
    private readonly int _width, _height;
    private readonly Color[] _transparent;
    private readonly Texture2D _transparentTexture;

    private readonly Camera _camera;

    private class AssetIconInfo(Asset asset, int handle, ItemIconReady callback)
    {
        public readonly Asset Asset = asset;

        public readonly int Handle = handle;
        public ItemIconReady Callback = callback;

        public void AddCallback(ItemIconReady callback)
        {
            Callback = (ItemIconReady)Delegate.Combine(Callback, callback);
        }
    }

    private Stack<AssetIconInfo> QueuedAssetIcons = []; // Stack to make newer requests higher priority
    private Dictionary<int, Guid> QueuedItemIcons = [];
    private Dictionary<Guid, Texture2D> CachedIcons = [];

    private static int AssetIconHandle = 1000;

    private AssetIconInfo? PendingIconInfo;
    private Transform? PendingModel;

    public IconStore(int width, int height)
    {
        _width = width;
        _height = height;

        _transparent = new Color[_width * _height];
        for (int p = 0; p < _transparent.Length; p++)
            _transparent[p] = Color.clear;

        _transparentTexture = new Texture2D(_width, _height, TextureFormat.ARGB32, mipChain: false)
        {
            name = "Transparent Texture"
        };
        _transparentTexture.SetPixels(_transparent);
        _transparentTexture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

        GameObject cameraObject = new("IconStore:Camera");
        _camera = cameraObject.AddComponent<Camera>();
        _camera.cullingMask = RayMasks.WATER | RayMasks.RESOURCE | RayMasks.SMALL | RayMasks.MEDIUM | RayMasks.LARGE | RayMasks.ENEMY;
        _camera.clearFlags = CameraClearFlags.Nothing;
        _camera.orthographic = true;
        _camera.enabled = false; // Disable Rendering and use Camera.Render() instead
    }

    public int RequestIcon(ItemAsset itemAsset, ItemIconReady callback)
    {
        if (CachedIcons.TryGetValue(itemAsset.GUID, out Texture2D? icon))
        {
            callback(-1, icon);
            return -1;
        }

        callback = (ItemIconReady)Delegate.Combine(callback, new ItemIconReady(CacheItemIcon));
        int handle = ItemTool.getIcon(itemAsset.id, 100, itemAsset.getState(), itemAsset, _width, _height, callback);

        if (handle != -1)
        {
            AssetIconHandle++;
            QueuedItemIcons.Add(handle, itemAsset.GUID);
        }

        return handle;

        void CacheItemIcon(int handle, Texture2D icon)
        {
            if (!QueuedItemIcons.TryGetValue(handle, out Guid itemGuid)) return;
            if (!CachedIcons.TryAdd(itemGuid, icon)) return;

            QueuedItemIcons.Remove(handle);
        }
    }

    public int RequestIcon(ObjectAsset objectAsset, ItemIconReady callback) => RequestIconInternal(objectAsset, callback);
    public int RequestIcon(ResourceAsset resourceAsset, ItemIconReady callback) => RequestIconInternal(resourceAsset, callback);
    public int RequestIcon(FoliageInstancedMeshInfoAsset foliageAsset, ItemIconReady callback) => RequestIconInternal(foliageAsset, callback);

    private int RequestIconInternal(Asset asset, ItemIconReady callback)
    {
        if (CachedIcons.TryGetValue(asset.GUID, out Texture2D? icon))
        {
            callback(-1, icon);
            return -1;
        }

        foreach (var queuedIcon in QueuedAssetIcons)
        {
            if (queuedIcon.Asset.GUID != asset.GUID) continue;

            queuedIcon.AddCallback(callback);
            return queuedIcon.Handle;
        }

        if (PendingIconInfo != null && PendingIconInfo.Asset.GUID == asset.GUID)
        {
            PendingIconInfo.AddCallback(callback);
            return PendingIconInfo.Handle;
        }

        AssetIconInfo iconInfo = new(asset, AssetIconHandle, callback);
        QueuedAssetIcons.Push(iconInfo);

        AssetIconHandle++;
        return iconInfo.Handle;
    }

    private static bool CreateObjectAsset(ObjectAsset objectAsset, out Transform? model)
    {
        model = null;
        if (objectAsset.type == EObjectType.DECAL) return true;

        GameObject original = objectAsset.GetOrLoadModel(SDG.Unturned.Level.isEditor);
        if (original == null) return false;

        model = GameObject.Instantiate(original).transform;
        model.rotation = Quaternion.Euler(-90f, 0f, 0f);

        if (objectAsset.rubble != EObjectRubble.NONE)
        {
            InteractableObjectRubble interactableRubble = model.gameObject.AddComponent<InteractableObjectRubble>();
            interactableRubble.updateState(objectAsset, objectAsset.getState());
            Transform? editor = model.Find("Editor");
            if (editor != null)
                editor.gameObject.SetActive(objectAsset.rubbleEditor == EObjectRubbleEditor.DEAD && SDG.Unturned.Level.isEditor);
        }

        if (objectAsset.interactability == EObjectInteractability.NPC)
        {
            InteractableObjectNPC interactableNPC = model.gameObject.AddComponent<InteractableObjectNPC>();
            interactableNPC.updateState(objectAsset, objectAsset.getState());
            interactableNPC.enabled = false;
            Animation animationNPC = model.Find("Root").GetComponent<Animation>();
            animationNPC.Play("Idle_Stand");
            animationNPC["Idle_Stand"].normalizedTime = 1f;
        }

        return true;
    }

    private static bool CreateResourceAsset(ResourceAsset resourceAsset, out Transform? model)
    {
        model = null;

        GameObject? original = resourceAsset.modelGameObject;
        if (original == null) return false;

        model = GameObject.Instantiate(original).transform;

        return true;
    }

    private static bool CreateFoliageAsset(FoliageInstancedMeshInfoAsset foliageAsset, out Transform? model)
    {
        model = null;

        Mesh? mesh = SDG.Unturned.Assets.load(foliageAsset.mesh);
        Material? material = SDG.Unturned.Assets.load(foliageAsset.material);
        if (mesh == null || material == null) return false;

        GameObject previewObject = new("IconStore:FoliageInstancedMesh")
        {
            layer = LayerMasks.LARGE
        };
        MeshFilter meshFilter = previewObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;
        MeshRenderer meshRenderer = previewObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;

        model = previewObject.transform;

        return true;
    }

    private Texture2D CaptureObjectIcon(ObjectAsset objectAsset, Transform objectTransform)
    {
        if (objectAsset.type == EObjectType.DECAL)
        {
            GameObject original = objectAsset.GetOrLoadModel(SDG.Unturned.Level.isEditor);
            Transform decalTransform = original.transform.Find("Decal");
            Texture2D decalTexture = (Texture2D)decalTransform.GetComponent<Decal>().material.GetTexture("_MainTex");
            if (decalTexture == null)
            {
                CommandWindow.LogWarning($"{objectAsset.AssetErrorPrefix}: Missing \"Decal\" Texture!");
                return _transparentTexture;
            }

            int width = _width;
            int height = _height;
            if (decalTransform.localScale.x > decalTransform.localScale.y)
                height = Mathf.CeilToInt(height * decalTransform.localScale.y / decalTransform.localScale.x);
            else
                width = Mathf.CeilToInt(width * decalTransform.localScale.x / decalTransform.localScale.y);

            return ResizeTexture(decalTexture, width, height);
        }

        Vector3 direction;
        if (objectAsset.FriendlyName.ToLower().Contains("billboard") || objectAsset.interactability == EObjectInteractability.NPC)
            direction = (objectTransform.right - objectTransform.up).normalized;
        else
            direction = (objectTransform.right + objectTransform.up).normalized;

        return CaptureModelIcon(objectTransform, direction, objectAsset.interactability == EObjectInteractability.NPC ? 1.4f : -1f);
    }

    private static readonly List<Renderer> _renderers = new(4);
    private static Bounds GetBounds(Transform transform)
    {
        _renderers.Clear();
        transform.GetComponentsInChildren(_renderers);

        Bounds bounds = default;
        bool boundsSet = false;
        foreach (Renderer renderer in _renderers)
        {
            if (renderer is not MeshRenderer && renderer is not SkinnedMeshRenderer) continue;

            if (!boundsSet)
            {
                bounds = renderer.bounds;
                boundsSet = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        if (!boundsSet) bounds = new Bounds(transform.position, Vector3.one);
        return bounds;
    }

    // Taken from ItemTool
    private float CalculateOrthographicSize(Bounds bounds)
    {
        Vector3 extents = bounds.extents;
        if (extents.ContainsInfinity() || extents.ContainsNaN() || extents.IsNearlyZero()) return 1f;

        Transform cameraTransform = _camera.transform;

        Bounds bounds2 = new(cameraTransform.InverseTransformVector(extents), Vector3.zero);
        bounds2.Encapsulate(cameraTransform.InverseTransformVector(-extents));
        bounds2.Encapsulate(cameraTransform.InverseTransformVector(new Vector3(0f - extents.x, extents.y, extents.z)));
        bounds2.Encapsulate(cameraTransform.InverseTransformVector(new Vector3(extents.x, 0f - extents.y, extents.z)));
        bounds2.Encapsulate(cameraTransform.InverseTransformVector(new Vector3(extents.x, extents.y, 0f - extents.z)));
        bounds2.Encapsulate(cameraTransform.InverseTransformVector(new Vector3(0f - extents.x, 0f - extents.y, extents.z)));
        bounds2.Encapsulate(cameraTransform.InverseTransformVector(new Vector3(0f - extents.x, extents.y, 0f - extents.z)));
        bounds2.Encapsulate(cameraTransform.InverseTransformVector(new Vector3(extents.x, 0f - extents.y, 0f - extents.z)));
        Vector3 extents2 = bounds2.extents;
        if (extents2.ContainsInfinity() || extents2.ContainsNaN() || extents2.IsNearlyZero()) return 1f;

        float num = Mathf.Abs(extents2.x);
        float num2 = Mathf.Abs(extents2.y);
        float num3 = Mathf.Abs(extents2.z);
        float nearClipPlane = _camera.nearClipPlane;
        cameraTransform.position = bounds.center - cameraTransform.forward * (num3 + 0.02f + nearClipPlane);

        num *= (float)(_width + 16) / (float)_width;
        num2 *= (float)(_height + 16) / (float)_height;
        float num4 = (float)_width / (float)_width;
        float num5 = num / num2;
        float num6 = ((num5 > num4) ? (num5 / num4) : 1f);
        return num2 * num6;
    }

    private Texture2D CaptureModelIcon(
        Transform modelTransform,
        Vector3 direction,
        float overrideOrthographicSize = -1f,
        bool repairAlphaFromColor = false
    )
    {
        Bounds bounds = GetBounds(modelTransform);

        float distance = Mathf.Max(bounds.size.x, bounds.size.z);
        float height = (bounds.size.y * 0.85f);
        if (bounds.size.y * 2 > Math.Abs(bounds.size.x - bounds.size.z))
        {
            height = bounds.size.y * 0.45f;
        }

        bool isFlat = bounds.size.y < 9f && bounds.size.y < Mathf.Min(bounds.size.x, bounds.size.z) * 0.25f;
        if (isFlat)
        {
            _camera.transform.position = bounds.center + direction * distance + Vector3.up * Mathf.Max(bounds.size.x, bounds.size.z);
        }
        else
        {
            _camera.transform.position = bounds.center + direction * distance + Vector3.up * height;
        }

        _camera.transform.rotation = Quaternion.LookRotation((bounds.center - _camera.transform.position).normalized);


        int antiAliasing = SDG.Unturned.GraphicsSettings.IsItemIconAntiAliasingEnabled ? 4 : 1;
        RenderTexture temporary =
            RenderTexture.GetTemporary(_width, _height, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, antiAliasing);
        temporary.name = "Render_" + modelTransform.name;
        RenderTexture.active = temporary;
        _camera.targetTexture = temporary;
        _camera.orthographicSize = overrideOrthographicSize > 0f ? overrideOrthographicSize : CalculateOrthographicSize(bounds);
        _camera.farClipPlane = (bounds.center - _camera.transform.position).magnitude * 2f;

        bool fog = RenderSettings.fog;
        AmbientMode ambientMode = RenderSettings.ambientMode;
        Color ambientSkyColor = RenderSettings.ambientSkyColor;
        Color ambientEquatorColor = RenderSettings.ambientEquatorColor;
        Color ambientGroundColor = RenderSettings.ambientGroundColor;
        Texture customReflection = RenderSettings.customReflectionTexture;

        RenderSettings.fog = false;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Color.white;
        RenderSettings.ambientEquatorColor = Color.white;
        RenderSettings.ambientGroundColor = Color.white;
        RenderSettings.customReflectionTexture = null;
        if (Provider.isConnected)
            LevelLighting.setEnabled(isEnabled: false);

        GL.Clear(clearDepth: true, clearColor: true, ColorEx.BlackZeroAlpha);
        _camera.Render();

        if (Provider.isConnected)
            LevelLighting.setEnabled(isEnabled: true);
        RenderSettings.fog = fog;
        RenderSettings.ambientMode = ambientMode;
        RenderSettings.ambientSkyColor = ambientSkyColor;
        RenderSettings.ambientEquatorColor = ambientEquatorColor;
        RenderSettings.ambientGroundColor = ambientGroundColor;
        RenderSettings.customReflectionTexture = customReflection;

        modelTransform.position = new Vector3(0f, -256f, 256f);

        Texture2D modelIcon = new(_width, _height, TextureFormat.ARGB32, mipChain: false)
        {
            name = "Icon_" + modelTransform.name,
            filterMode = FilterMode.Point
        };
        modelIcon.ReadPixels(new Rect(0f, 0f, _width, _height), 0, 0);

        // Seems to be required to Render some Grass Foliage, Should probably be replaced with a Shader but it's fine for Small Icons
        if (repairAlphaFromColor)
        {
            Color32[] pixels = modelIcon.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 pixel = pixels[i];
                if (pixel.a > 5) continue;

                byte maxChannel = pixel.r;
                if (pixel.g > maxChannel) maxChannel = pixel.g;
                if (pixel.b > maxChannel) maxChannel = pixel.b;

                if (maxChannel > 10)
                {
                    pixel.a = 255;
                    pixels[i] = pixel;
                }
            }

            modelIcon.SetPixels32(pixels);
        }

        modelIcon.Apply(updateMipmaps: false, makeNoLongerReadable: true);

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(temporary);
        GameObject.Destroy(modelTransform.gameObject);

        return modelIcon;
    }

    private Texture2D ResizeTexture(Texture2D texture, int newWidth, int newHeight)
    {
        RenderTexture temporary = RenderTexture.GetTemporary(newWidth, newHeight);
        Graphics.Blit(texture, temporary);

        RenderTexture.active = temporary;

        Texture2D newTexture = new(_width, _height, TextureFormat.ARGB32, mipChain: false)
        {
            name = texture.name,
            filterMode = FilterMode.Point
        };
        newTexture.SetPixels(_transparent);
        newTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
        newTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), newTexture.width / 2 - newWidth / 2, newTexture.height / 2 - newHeight / 2);
        newTexture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(temporary);

        return newTexture;
    }

    public void CustomUpdate()
    {
        if (PendingIconInfo != null)
        {
            // Move into Position for rendering this frame instead of last frame to allow for multiple IconStores
            if (PendingModel != null) PendingModel.position = new Vector3(256f, -256f, 0f);

            Texture2D assetIcon = PendingIconInfo.Asset switch
            {
                ObjectAsset objectAsset => CaptureObjectIcon(objectAsset, PendingModel!),
                ResourceAsset => CaptureModelIcon(PendingModel!, (PendingModel!.right + PendingModel!.forward).normalized, repairAlphaFromColor: true),
                FoliageInstancedMeshInfoAsset => CaptureModelIcon(PendingModel!, (PendingModel!.right + PendingModel!.forward).normalized, repairAlphaFromColor: true),
                _ => _transparentTexture
            };

            PendingIconInfo.Callback(PendingIconInfo.Handle, assetIcon);
            CachedIcons.Add(PendingIconInfo.Asset.GUID, assetIcon);

            if (PendingModel != null) GameObject.Destroy(PendingModel.gameObject);
            PendingModel = null;
            PendingIconInfo = null;
        }

        if (QueuedAssetIcons.Count > 0)
        {
            PendingIconInfo = QueuedAssetIcons.Pop();

            bool createdSuccessfully = PendingIconInfo.Asset switch
            {
                ObjectAsset objectAsset => CreateObjectAsset(objectAsset, out PendingModel),
                ResourceAsset resourceAsset => CreateResourceAsset(resourceAsset, out PendingModel),
                FoliageInstancedMeshInfoAsset foliageAsset => CreateFoliageAsset(foliageAsset, out PendingModel),
                _ => false
            };

            if (!createdSuccessfully)
            {
                PendingIconInfo.Callback(PendingIconInfo.Handle, _transparentTexture);
                CachedIcons.Add(PendingIconInfo.Asset.GUID, _transparentTexture);

                if (PendingModel != null) GameObject.Destroy(PendingModel.gameObject);
                PendingModel = null;
                PendingIconInfo = null;
                return;
            }

            if (PendingModel != null) PendingModel.position = new Vector3(512f, -256f, 0f);
        }
    }
}