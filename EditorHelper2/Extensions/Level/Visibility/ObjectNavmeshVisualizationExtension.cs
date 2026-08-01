using System.Collections.Generic;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.Helpers;
using EditorHelper2.UI.Builders;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Level.Visibility;

/// <summary>
/// Draws the navigation geometry contributed by placed level objects while navigation visibility is enabled.
/// </summary>
[EHExtension("Object Navmesh Visualization", "Senior S")]
[UIExtension(typeof(EditorLevelVisibilityUI))]
public sealed class ObjectNavmeshVisualizationExtension : UIExtension, IExtension
{
    private const string ConfigSection = "ObjectNavmeshVisualization";
    private const float CacheRefreshIntervalSeconds = 0.5f;

    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;

    private readonly List<MeshCollider> _meshColliders = [];
    private readonly List<MeshCollider> _meshColliderBuffer = [];
    private readonly List<Renderer> _objectRenderers = [];
    private readonly List<Renderer> _rendererBuffer = [];
    private readonly Dictionary<Renderer, bool> _originalForceRenderingOff = [];
    private readonly ISleekToggle _hideObjectModelsToggle;

    private Material? _visualizationMaterial;
    private Material? _navmeshOnlyMaterial;
    private float _nextCacheRefreshTime;
    private bool _isVisible;
    private bool _areObjectModelsHidden;

    /// <summary>
    /// Creates the object navigation visualization extension.
    /// </summary>
    public ObjectNavmeshVisualizationExtension()
    {
        UIBuilder builder = new UIBuilder(40f, 40f)
            .SetAnchorHorizontal(1f)
            .SetOffsetHorizontal(-210f)
            .SetOffsetVertical(590f)
            .SetText("Object navmesh only");

        _hideObjectModelsToggle = builder.BuildToggle(
            "Hide object models while displaying their navigation geometry");
        _hideObjectModelsToggle.Value = false;

        Initialize();
        MapEditorConfigHelper.RegisterExtensionSettings(ConfigSection, CaptureSettings, ApplySettings);
    }

    /// <summary>
    /// Loads Unturned's navigation visualization material.
    /// </summary>
    public void Initialize()
    {
        _container?.AddChild(_hideObjectModelsToggle);
        _hideObjectModelsToggle.OnValueChanged += OnHideObjectModelsToggleChanged;

        GameObject? flagPrefab = Resources.Load<GameObject>("Edit/Flag");
        Transform? navmeshTransform = flagPrefab?.transform.Find("Navmesh");
        _visualizationMaterial = navmeshTransform?.GetComponent<MeshRenderer>()?.sharedMaterial;
        _navmeshOnlyMaterial = Resources.Load<Material>("Materials/Blank");

        if (_visualizationMaterial == null)
        {
            UnturnedLog.warn("[Object Navmesh Visualization] Unable to load the navigation visualization material");
            return;
        }
    }

    /// <summary>
    /// Synchronizes the cached object navigation meshes with the current level.
    /// </summary>
    internal void CustomUpdate()
    {
        bool shouldBeVisible = SDG.Unturned.Level.isEditor
                               && LevelVisibility.navigationVisible
                               && _visualizationMaterial != null;
        bool shouldHideObjectModels = shouldBeVisible && _hideObjectModelsToggle.Value;

        if (!shouldBeVisible)
        {
            SetObjectModelsHidden(false);
            _isVisible = false;
            return;
        }

        float currentTime = Time.realtimeSinceStartup;
        if (!_isVisible || currentTime >= _nextCacheRefreshTime)
        {
            RefreshMeshColliderCache();
            _nextCacheRefreshTime = currentTime + CacheRefreshIntervalSeconds;
        }

        SetObjectModelsHidden(shouldHideObjectModels);
        _isVisible = true;
        DrawMeshColliders();
    }

    private void OnHideObjectModelsToggleChanged(ISleekToggle toggle, bool state)
    {
        bool shouldHideObjectModels = state
                                      && SDG.Unturned.Level.isEditor
                                      && LevelVisibility.navigationVisible
                                      && _visualizationMaterial != null;
        SetObjectModelsHidden(shouldHideObjectModels);
    }

    private Settings CaptureSettings() => new() { HideObjectModels = _hideObjectModelsToggle.Value };

    private void ApplySettings(Settings settings)
    {
        _hideObjectModelsToggle.Value = settings.HideObjectModels;
    }

    private void RefreshMeshColliderCache()
    {
        SetObjectModelsHidden(false);
        _meshColliders.Clear();
        _objectRenderers.Clear();

        List<LevelObject>[,]? levelObjects = LevelObjects.objects;
        if (levelObjects == null)
        {
            return;
        }

        for (int x = 0; x < levelObjects.GetLength(0); x++)
        {
            for (int y = 0; y < levelObjects.GetLength(1); y++)
            {
                List<LevelObject>? regionObjects = levelObjects[x, y];
                if (regionObjects == null)
                {
                    continue;
                }

                foreach (LevelObject levelObject in regionObjects)
                {
                    Transform? objectTransform = levelObject?.transform;
                    if (objectTransform == null)
                    {
                        continue;
                    }

                    Transform? navTransform = objectTransform.Find("Nav");
                    if (navTransform == null)
                    {
                        continue;
                    }

                    _meshColliderBuffer.Clear();
                    navTransform.GetComponentsInChildren(true, _meshColliderBuffer);

                    foreach (MeshCollider meshCollider in _meshColliderBuffer)
                    {
                        if (meshCollider != null && meshCollider.sharedMesh != null)
                        {
                            _meshColliders.Add(meshCollider);
                        }
                    }

                    _rendererBuffer.Clear();
                    objectTransform.GetComponentsInChildren(true, _rendererBuffer);

                    foreach (Renderer renderer in _rendererBuffer)
                    {
                        if (renderer == null
                            || renderer.transform == navTransform
                            || renderer.transform.IsChildOf(navTransform))
                        {
                            continue;
                        }

                        _objectRenderers.Add(renderer);
                    }
                }
            }
        }
    }

    private void SetObjectModelsHidden(bool areHidden)
    {
        if (_areObjectModelsHidden == areHidden)
        {
            return;
        }

        if (areHidden)
        {
            foreach (Renderer renderer in _objectRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (!_originalForceRenderingOff.ContainsKey(renderer))
                {
                    _originalForceRenderingOff.Add(renderer, renderer.forceRenderingOff);
                }

                renderer.forceRenderingOff = true;
            }
        }
        else
        {
            foreach (KeyValuePair<Renderer, bool> rendererState in _originalForceRenderingOff)
            {
                if (rendererState.Key != null)
                {
                    rendererState.Key.forceRenderingOff = rendererState.Value;
                }
            }

            _originalForceRenderingOff.Clear();
        }

        _areObjectModelsHidden = areHidden;
    }

    private void DrawMeshColliders()
    {
        Camera? camera = MainCamera.instance;
        Material? material = _hideObjectModelsToggle.Value
            ? _navmeshOnlyMaterial ?? _visualizationMaterial
            : _visualizationMaterial;

        if (camera == null || material == null)
        {
            return;
        }

        foreach (MeshCollider meshCollider in _meshColliders)
        {
            if (meshCollider == null || !meshCollider.enabled || !meshCollider.gameObject.activeInHierarchy)
            {
                continue;
            }

            Mesh? mesh = meshCollider.sharedMesh;
            if (mesh == null)
            {
                continue;
            }

            Graphics.DrawMesh(
                mesh,
                meshCollider.transform.localToWorldMatrix,
                material,
                LayerMasks.NAVMESH,
                camera,
                0,
                null,
                UnityEngine.Rendering.ShadowCastingMode.Off,
                false);
        }
    }

    /// <summary>
    /// Releases cached Unity objects.
    /// </summary>
    public void Dispose()
    {
        MapEditorConfigHelper.UnregisterExtensionSettings(ConfigSection);
        SetObjectModelsHidden(false);

        _hideObjectModelsToggle.OnValueChanged -= OnHideObjectModelsToggleChanged;
        _container?.RemoveChild(_hideObjectModelsToggle);

        _meshColliders.Clear();
        _meshColliderBuffer.Clear();
        _objectRenderers.Clear();
        _rendererBuffer.Clear();
        _visualizationMaterial = null;
        _navmeshOnlyMaterial = null;
        _isVisible = false;
    }

    private sealed class Settings
    {
        public bool HideObjectModels { get; set; }
    }
}
