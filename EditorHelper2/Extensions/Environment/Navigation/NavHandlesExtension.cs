using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.Helpers;
using EditorHelper2.Patches.Editor;
using EditorHelper2.UI.Builders;
using SDG.Unturned;
using System.Collections.Generic;
using UnityEngine;

namespace EditorHelper2.Extensions.Environment.Navigation;

[EHExtension("Navigation Handles", "Gamingtoday093")]
[UIExtension(typeof(EditorEnvironmentNavigationUI))]
public class NavHandlesExtension : UIExtension, IExtension
{
    private const string ConfigSection = "NavigationHandles";
    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;

    [ExistingMember("widthSlider")]
    private readonly ISleekSlider? _widthSlider;
    [ExistingMember("heightSlider")]
    private readonly ISleekSlider? _heightSlider;

    private readonly ISleekFloat32Field _snapTransformField;
    private readonly SleekButtonIcon _translateButton;
    private readonly SleekButtonIcon _scaleButton;

    private readonly TransformHandles _handles;
    private EDragMode DragMode
    {
        get;
        set
        {
            field = value;

            _wantsBoundsEditor = false;
            CalculateHandleOffsets();
        }
    }
    private bool _wantsBoundsEditor;
    private bool _isUsingHandle;
    private float _snapTransform;

    private Vector3 _fromPosition;
    private float _fromWidth, _fromHeight;
    private Matrix4x4 _preTransformMatrix;

    private readonly BoxCollider _navigationBounds;

    public NavHandlesExtension()
    {
        _handles = new TransformHandles();

        DragMode = EDragMode.TRANSFORM;
        _wantsBoundsEditor = false;
        _snapTransform = 1f;

        GameObject navBounds = new("EditorHelper:NavigationBounds")
        {
            layer = LayerMasks.IGNORE_RAYCAST
        };
        _navigationBounds = navBounds.AddComponent<BoxCollider>();

        Local local = Localization.read("/Editor/EditorLevelObjects.dat");
        Bundle bundle = Bundles.getBundle("/Bundles/Textures/Edit/Icons/EditorLevelObjects/EditorLevelObjects.unity3d");

        UIBuilder builder = new(200f, 30f);

        builder.SetAnchorVertical(1f)
            .SetOffsetVertical(-30f)
            .SetSpacing(40f)
            .SetText(local.format("ScaleButtonText", ControlsSettings.tool_3));

        _scaleButton = builder.BuildButtonIcon(local.format("ScaleButtonTooltip"), bundle.load<Texture2D>("Scale"));
        _scaleButton.onClickedButton += UseScaleButton;

        builder.SetText(local.format("TransformButtonText", ControlsSettings.tool_0));

        _translateButton = builder.BuildButtonIcon(local.format("TransformButtonTooltip"), bundle.load<Texture2D>("Transform"));
        _translateButton.onClickedButton += UseTranslateButton;

        builder.SetText(local.format("SnapTransformLabelText"));

        _snapTransformField = builder.BuildFloatInput(ESleekSide.RIGHT);
        _snapTransformField.Value = _snapTransform;
        _snapTransformField.OnValueChanged += OnSnapTransformValueChanged;

        bundle.unload();

        Initialize();
        MapEditorConfigHelper.RegisterExtensionSettings(ConfigSection, CaptureSettings, ApplySettings);
    }

    public void Initialize()
    {
        if (_container == null) return;

        _container.AddChild(_snapTransformField);
        _container.AddChild(_translateButton);
        _container.AddChild(_scaleButton);

        _handles.OnPreTransform += OnPreTransform;
        _handles.OnTranslatedAndRotated += OnTranslated;
        _handles.OnTransformed += OnTransformed;

        EditorNavigationPatches.OnSelectionChanged += OnSelectionChanged;
    }

    #region Event Handlers
    private void OnSnapTransformValueChanged(ISleekFloat32Field field, float value)
    {
        _snapTransform = value;
    }

    private void UseTranslateButton(ISleekElement button)
    {
        DragMode = EDragMode.TRANSFORM;
    }

    private void UseScaleButton(ISleekElement button)
    {
        DragMode = EDragMode.SCALE;
    }

    private void OnSelectionChanged()
    {
        CalculateHandleOffsets();
    }

    private void OnPreTransform(Matrix4x4 worldToPivot)
    {
        if (EditorNavigation.selection != null)
        {
            _fromPosition = EditorNavigation.flag.point;
            _fromWidth = EditorNavigation.flag.width;
            _fromHeight = EditorNavigation.flag.height;
            _preTransformMatrix = worldToPivot * EditorNavigation.selection.localToWorldMatrix;
        }
    }

    private void OnTranslated(Vector3 worldPositionDelta, Quaternion worldRotationDelta, Vector3 pivotPosition, bool modifyRotation)
    {
        if (EditorNavigation.selection != null)
        {
            EditorNavigation.flag.move(_fromPosition + worldPositionDelta);
        }
        CalculateHandleOffsets();
    }

    private void OnTransformed(Matrix4x4 pivotToWorld)
    {
        if (EditorNavigation.selection != null)
        {
            pivotToWorld = pivotToWorld * _preTransformMatrix;

            EditorNavigation.flag.move(pivotToWorld.GetPosition());

            // Flag.width & Flag.height uses the scale 0 to 1 to indicate size which you can't multiply by 2 for example to get twice the size because 0 * 2 = 0
            // Instead multiply the world size of the Region by the scale and then bring it back into a 0 to 1 scale
            Vector3 scale = pivotToWorld.lossyScale;
            float worldWidth = Flag.MIN_SIZE + _fromWidth * (Flag.MAX_SIZE - Flag.MIN_SIZE);
            float worldHeight = Flag.MIN_SIZE + _fromHeight * (Flag.MAX_SIZE - Flag.MIN_SIZE);
            EditorNavigation.flag.width = Mathf.Clamp01(((worldWidth * scale.x) - Flag.MIN_SIZE) / (Flag.MAX_SIZE - Flag.MIN_SIZE));
            EditorNavigation.flag.height = Mathf.Clamp01(((worldHeight * scale.z) - Flag.MIN_SIZE) / (Flag.MAX_SIZE - Flag.MIN_SIZE));
            EditorNavigation.flag.buildMesh();

            _widthSlider?.Value = EditorNavigation.flag.width;
            _heightSlider?.Value = EditorNavigation.flag.height;
        }
        CalculateHandleOffsets();
    }
    #endregion

    public bool CustomUpdate()
    {
        if (!EditorNavigation.isPathfinding || EditorInteract.isFlying || !Glazier.Get().ShouldGameProcessInput)
        {
            if (_isUsingHandle) ReleaseHandle();
            return true;
        }

        _handles.snapPositionInterval = _snapTransform;
        if (DragMode == EDragMode.TRANSFORM)
        {
            if (_wantsBoundsEditor)
            {
                _handles.SetPreferredMode(TransformHandles.EMode.PositionBounds);
                _handles.UpdateBoundsFromSelection(GetSelectionBounds());
            }
            else _handles.SetPreferredMode(TransformHandles.EMode.Position);
        }
        else if (DragMode == EDragMode.SCALE)
        {
            if (_wantsBoundsEditor)
            {
                _handles.SetPreferredMode(TransformHandles.EMode.ScaleBounds);
                _handles.UpdateBoundsFromSelection(GetSelectionBounds());
            }
            else _handles.SetPreferredMode(TransformHandles.EMode.Scale);
        }

        bool selectingHandle = EditorNavigation.selection && _handles.Raycast(EditorInteract.ray);
        if (EditorNavigation.selection != null)
        {
            _handles.Render(EditorInteract.ray);
        }

        if (_isUsingHandle)
        {
            if (!InputEx.GetKey(ControlsSettings.primary))
            {
                ReleaseHandle();
                return false;
            }

            _handles.wantsToSnap = InputEx.GetKey(ControlsSettings.snap);
            _handles.MouseMove(EditorInteract.ray);
            return false;
        }

        if (InputEx.GetKeyDown(ControlsSettings.tool_0))
        {
            if (DragMode != EDragMode.TRANSFORM) DragMode = EDragMode.TRANSFORM;
            else _wantsBoundsEditor = !_wantsBoundsEditor;
        }
        if (InputEx.GetKeyDown(ControlsSettings.tool_3))
        {
            if (DragMode != EDragMode.SCALE) DragMode = EDragMode.SCALE;
            else _wantsBoundsEditor = !_wantsBoundsEditor;
        }

        if (!_isUsingHandle && InputEx.GetKeyDown(ControlsSettings.primary))
        {
            if (selectingHandle)
            {
                _handles.MouseDown(EditorInteract.ray);
                _isUsingHandle = true;
                return false;
            }
        }

        EditorNavigation.marker.gameObject.SetActive(!selectingHandle);
        return true;
    }

    private void ReleaseHandle()
    {
        _isUsingHandle = false;
        _handles.MouseUp();
    }

    public void CalculateHandleOffsets()
    {
        if (EditorNavigation.selection == null) return;

        _handles.SetPreferredPivot(EditorNavigation.selection.position, Quaternion.identity);
    }

    private IEnumerable<GameObject> GetSelectionBounds()
    {
        if (EditorNavigation.selection == null) yield break;

        _navigationBounds.transform.position = EditorNavigation.flag.point;
        float worldWidth = Flag.MIN_SIZE + EditorNavigation.flag.width * (Flag.MAX_SIZE - Flag.MIN_SIZE);
        float worldHeight = Flag.MIN_SIZE + EditorNavigation.flag.height * (Flag.MAX_SIZE - Flag.MIN_SIZE);
        _navigationBounds.size = new Vector3(worldWidth, 0f, worldHeight);
        yield return _navigationBounds.gameObject;
    }

    public void Dispose()
    {
        MapEditorConfigHelper.UnregisterExtensionSettings(ConfigSection);
        if (_container == null) return;

        _container.RemoveChild(_snapTransformField);
        _container.RemoveChild(_translateButton);
        _container.RemoveChild(_scaleButton);

        _handles.OnPreTransform -= OnPreTransform;
        _handles.OnTranslatedAndRotated -= OnTranslated;
        _handles.OnTransformed -= OnTransformed;

        EditorNavigationPatches.OnSelectionChanged -= OnSelectionChanged;
    }

    private Settings CaptureSettings() => new() { SnapTransform = _snapTransform };

    private void ApplySettings(Settings settings)
    {
        _snapTransform = settings.SnapTransform;
        _snapTransformField.Value = settings.SnapTransform;
    }

    private sealed class Settings
    {
        public float SnapTransform { get; set; } = 1f;
    }
}
