using System.Collections.Generic;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Keybinds;
using EditorHelper2.common.Types;
using EditorHelper2.Patches.Editor;
using EditorHelper2.UI.Builders;
using SDG.Framework.Rendering;
using SDG.Framework.Utilities;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Environment.Roads;

[UIExtension(typeof(EditorEnvironmentRoadsUI))]
[EHExtension("Road Handles & Selection", "Senior S & JienSultan")]
public class HandlesExtension : UIExtension, IExtension
{
    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;
    
    private readonly TransformHandles _handles;
    public TransformHandles GetHandles()
    {
        return _handles;
    }
    private EDragMode _dragMode;
    private EDragCoordinate _dragCoordinate;
    private bool _isUsingHandle;
    private Vector2 _dragStartViewportPoint;
    private Vector2 _dragStartScreenPoint;
    private Vector2 _dragEndViewportPoint;
    private Vector2 _dragEndScreenPoint;
    private bool _hasDragStart;
    private bool _isDragging;
    // TODO: Save this stuff to a file, same as EditorObjects do
    private float _snapTransform;
    private readonly float _snapRotation;
    private int _step;
    private IReun[] _reun = [];
    private int _frame;
    
    private RoadSelection? _roadSelection;
    private EditorDrag? _editorDrag;
    private Vector3 _copyPosition = Vector3.zero;
    
    // Road Selection
    // State for drag selection rectangle
    private Vector2 _startScreenPos;
    private bool _isSelecting;
    private bool _isAddingToSelection;
    private Rect _selectionRect;

    // Currently selected joints and their associated paths
    private List<RoadJointCustom> _selectedJoints;
    private List<RoadPath> _selectedPaths;

    private Camera? _sceneCamera;
    // Information about the primary joint (the pivot for moving multiple joints)
    public static RoadJointCustom? _primaryJoint;
    private Vector3 _primaryLastPosition;

    // Stores relative offsets for multi-joint movement
    private readonly Dictionary<RoadJointCustom, Vector3> _otherOffsets = new();
    
    private readonly SleekButtonState _coordinateButton;
    private readonly ISleekToggle _depthToggleButton;
    private readonly ISleekFloat32Field _snapTransformField;
    private readonly ISleekToggle _handlePrioritizeToggleButton;

    public bool GetHandlePrioritizeValue()
    {
        return _handlePrioritizeToggleButton.Value;
    }
    
    public HandlesExtension()
    {
        _handles = new TransformHandles();
        _handles.OnPreTransform += OnHandlePreTransform;
        _handles.OnTranslatedAndRotated += OnHandleTranslatedAndRotated;
        _handles.OnTransformed += OnHandleTransformed;
        _dragMode = EDragMode.TRANSFORM;
        _dragCoordinate = EDragCoordinate.GLOBAL;
        _hasDragStart = false;
        _isDragging = false;
        _snapTransform = 1f;
        _snapRotation = 15f;
        _roadSelection = null;
        _editorDrag = null;
        _primaryJoint = null;
        _selectedJoints = [];
        _selectedPaths = [];
        _step = 0;
        _frame = 0;
        
        UIBuilder builder = new(40f, 40f);
        
        builder.SetAnchorVertical(1f)
            .SetOffsetVertical(-320f)
            .SetText("Radius includes depth");
        _depthToggleButton = builder.BuildToggle("Should the display radius of the road include the depth?");

        builder.SetSpacing(30f)
            .SetSizeHorizontal(200f)
            .SetSizeVertical(30f);

        Local local = Localization.read("/Editor/EditorLevelObjects.dat"); // Just to keep consistency in case client have a translations mod.
        Bundle bundle = Bundles.getBundle("/Bundles/Textures/Edit/Icons/EditorLevelObjects/EditorLevelObjects.unity3d");
        builder.SetText(local.format("CoordinateButtonTooltip"));
        _coordinateButton = builder.BuildButtonState(
            new GUIContent(local.format("CoordinateButtonTextGlobal"), bundle.load<Texture>("Global")),
            new GUIContent(local.format("CoordinateButtonTextLocal"), bundle.load<Texture>("Local")));

        builder.SetText(local.format("SnapTransformLabelText"));
        _snapTransformField = builder.BuildFloatInput(ESleekSide.RIGHT);

        builder.SetText("Prioritize handle")
            .SetSizeHorizontal(40f)
            .SetSizeVertical(40f);
        _handlePrioritizeToggleButton = builder.BuildToggle("When clicking should the click prioritize the arrows?");
        _handlePrioritizeToggleButton.Value = true;

        bundle.unload();
        
        if (SDG.Unturned.Level.isEditor)
        {
            _reun = new IReun[256];
            _step = 0;
            _frame = 0;
        }
        
        Initialize();
    }

    public void Initialize()
    {
        if (_container == null) return;

        _sceneCamera = Camera.main;
        _container.AddChild(_coordinateButton);
        _container.AddChild(_depthToggleButton);
        _container.AddChild(_snapTransformField);
        _container.AddChild(_handlePrioritizeToggleButton);
        _snapTransformField.Value = _snapTransform;
        
        _handles.OnPreTransform += OnHandlePreTransform;
        _handles.OnTranslatedAndRotated += OnHandleTranslatedAndRotated;
        _handles.OnTransformed += OnHandleTransformed;
        _coordinateButton.onSwappedState = OnSwappedState;
        _snapTransformField.OnValueChanged += OnSnapTransformFieldValueChanged;
        Camera.onPostRender += OnPostRender;
        
        EditorRoadsPatches.OnRoadSelected += OnRoadSelected;
        EditorRoadsPatches.OnRoadDeselected += OnRoadDeselected;
    }

    #region Event handlers
    private void OnRoadSelected(Transform obj)
    {
        Select();
    }
    
    private void OnRoadDeselected()
    {
        ClearSelection();
    }
    
    private void OnPostRender(Camera cam)
    {
        if (!_isSelecting || _sceneCamera == null)
            return;

        GLUtility.LINE_FLAT_COLOR.SetPass(0);
        GLUtility.matrix = MathUtility.IDENTITY_MATRIX;

        GL.Begin(GL.LINES);
        GL.Color(Color.yellow);

        Vector3 startViewport = _sceneCamera.ScreenToViewportPoint(_startScreenPos);
        Vector3 endViewport = _sceneCamera.ScreenToViewportPoint(Input.mousePosition);

        startViewport.z = 16f;
        endViewport.z = 16f;

        Vector3 min = new Vector3(Mathf.Min(startViewport.x, endViewport.x), Mathf.Min(startViewport.y, endViewport.y), 16f);
        Vector3 max = new Vector3(Mathf.Max(startViewport.x, endViewport.x), Mathf.Max(startViewport.y, endViewport.y), 16f);

        Vector3 v0 = _sceneCamera.ViewportToWorldPoint(new Vector3(min.x, min.y, min.z));
        Vector3 v1 = _sceneCamera.ViewportToWorldPoint(new Vector3(max.x, min.y, min.z));
        Vector3 v2 = _sceneCamera.ViewportToWorldPoint(new Vector3(max.x, max.y, max.z));
        Vector3 v3 = _sceneCamera.ViewportToWorldPoint(new Vector3(min.x, max.y, max.z));

        GL.Vertex(v0);
        GL.Vertex(v1);
        GL.Vertex(v1);
        GL.Vertex(v2);
        GL.Vertex(v2);
        GL.Vertex(v3);
        GL.Vertex(v3);
        GL.Vertex(v0);

        GL.End();
    }
    
    private void OnHandlePreTransform(Matrix4x4 worldToPivot)
    {
        if (_roadSelection == null) return;
        
        _roadSelection.FromPosition = _roadSelection.Transform.position;
        _roadSelection.RelativeToPivot = worldToPivot * _roadSelection.Transform.localToWorldMatrix;
    }
    
    private void OnHandleTranslatedAndRotated(Vector3 worldPositionDelta, Quaternion worldRotationDelta, Vector3 pivotPosition, bool modifyRotation)
    {
        if (_roadSelection == null) return;
        
        Vector3 vector = _roadSelection.FromPosition - pivotPosition;
        Vector3 point = _roadSelection.FromPosition + worldPositionDelta;
        if (!vector.IsNearlyZero())
        {
            point = pivotPosition + worldRotationDelta * vector + worldPositionDelta;
        }

        if (EditorRoads.road != null)
        {
            if (EditorRoads.tangentIndex > -1)
            {
                EditorRoads.road.moveTangent(EditorRoads.vertexIndex, EditorRoads.tangentIndex, point - EditorRoads.joint.vertex);
            }
            else if (EditorRoads.vertexIndex > -1)
            {
                EditorRoads.road.moveVertex(EditorRoads.vertexIndex, point);
            }
        }

        if (modifyRotation)
        {
            if (_primaryJoint.HasValue)
            {
                foreach ((RoadJointCustom targetJoint, Vector3 originalOffset) in _otherOffsets)
                {
                    Vector3 originalPosition = _roadSelection.FromPosition + originalOffset;
                    Vector3 pivotToOriginal = originalPosition - pivotPosition;
                    Vector3 rotatedOffset = worldRotationDelta * pivotToOriginal;
                    Vector3 newPosition = pivotPosition + rotatedOffset + worldPositionDelta;

                    targetJoint.Road.moveVertex(targetJoint.Index, newPosition);
                    
                    if (targetJoint.TangentPositions[0] != Vector3.zero)
                    {
                        Vector3 rotatedPosition = worldRotationDelta * targetJoint.TangentPositions[0];
                        targetJoint.Road.moveTangent(targetJoint.Index, 0, rotatedPosition);
                    }

                    if (targetJoint.TangentPositions[1] != Vector3.zero)
                    {
                        Vector3 rotatedPosition = worldRotationDelta * targetJoint.TangentPositions[1];
                        targetJoint.Road.moveTangent(targetJoint.Index, 1, rotatedPosition);
                    }
                }
            }

            RoadJoint? primaryJoint = EditorRoads.joint;
            if (EditorRoads.road != null && primaryJoint != null)
            {
                if (primaryJoint.getTangent(0) != Vector3.zero)
                {
                    Vector3 rotatedPosition = worldRotationDelta * _roadSelection.Tangents[0];
                    EditorRoads.road.moveTangent(EditorRoads.vertexIndex, 0, rotatedPosition);
                }

                if (primaryJoint.getTangent(1) != Vector3.zero)
                {
                    Vector3 rotatedPosition = worldRotationDelta * _roadSelection.Tangents[1];
                    EditorRoads.road.moveTangent(EditorRoads.vertexIndex, 1, rotatedPosition);
                }
            }
        }
        else if (_primaryJoint.HasValue)
        {
            RoadJointCustom joint = _primaryJoint.Value;
            Vector3 current = joint.Road.joints[joint.Index].vertex;

            if (current != _primaryLastPosition)
            {
                foreach ((RoadJointCustom targetJoint, Vector3 offset) in _otherOffsets)
                {
                    Vector3 newPosition = current + offset;
                    targetJoint.Road.moveVertex(targetJoint.Index, newPosition);
                }

                _primaryLastPosition = current;
            }
        }

        CalculateHandleOffsets();
    }
    
    private void OnHandleTransformed(Matrix4x4 pivotToWorld)
    {
        if (_roadSelection == null) return;
        
        Matrix4x4 matrix = pivotToWorld * _roadSelection.RelativeToPivot;

        Vector3 point = matrix.GetPosition();
        float oldY = _roadSelection.Transform.position.y;
        if (EditorRoads.road != null)
        {
            if (EditorRoads.tangentIndex > -1)
            {
                EditorRoads.road.moveTangent(EditorRoads.vertexIndex, EditorRoads.tangentIndex, point - EditorRoads.joint.vertex);
            }
            else if (EditorRoads.vertexIndex > -1)
            {
                EditorRoads.road.moveVertex(EditorRoads.vertexIndex, point);
            }
            
            Dictionary<RoadJointCustom, Vector3> updatedPositions = new();
            foreach (KeyValuePair<RoadJointCustom, Vector3> kvp in _otherOffsets)
            {
                RoadJointCustom targetJoint = kvp.Key;
                RoadJoint? joint = targetJoint.Road.joints[targetJoint.Index];
                if (joint == null) continue;

                targetJoint.TangentPositions = [joint.getTangent(0), joint.getTangent(1)]; 
                
                updatedPositions[targetJoint] = joint.vertex - _primaryLastPosition;
            }

            _otherOffsets.Clear();
            foreach (KeyValuePair<RoadJointCustom, Vector3> keyValuePair in updatedPositions)
            {
                _otherOffsets.Add(keyValuePair.Key, keyValuePair.Value);
            }
            updatedPositions.Clear();

            _roadSelection.Tangents[0] = EditorRoads.joint.getTangent(0);
            _roadSelection.Tangents[1] = EditorRoads.joint.getTangent(1);

            if (!Mathf.Approximately(oldY, point.y))
            {
                float offset = point.y - oldY;

                EditorRoads.joint.offset = offset;
                EditorRoads.road.updatePoints();

                EditorEnvironmentRoadsUI.offsetField.Value = offset;
            }
        }

        CalculateHandleOffsets();
    }
    
    private void OnSwappedState(SleekButtonState button, int index)
    {
        _dragCoordinate = (EDragCoordinate)index;
        CalculateHandleOffsets();
    }
    
    private void OnSnapTransformFieldValueChanged(ISleekFloat32Field field, float value)
    {
        _snapTransform = value;
    }
    #endregion Event handlers

    #region Extension Functions

    public bool PreCustomUpdate()
    {
        bool buttonVisible = EditorEnvironmentRoadsUI.active && EditorRoads.selection != null;
        
        _depthToggleButton.IsVisible = buttonVisible;
        _coordinateButton.IsVisible = buttonVisible;
        _snapTransformField.IsVisible = buttonVisible;
        _handlePrioritizeToggleButton.IsVisible = buttonVisible;
        
        // While dragging, update the selection rectangle and highlight nodes under it
        if (_isSelecting)
        {
            _selectionRect = GetScreenRect(_startScreenPos, Input.mousePosition);
            SelectObjectsInRect();
        }
        
        if (!EditorRoads.isPaving || EditorInteract.isFlying || !Glazier.Get().ShouldGameProcessInput)
        {
            if (_isUsingHandle)
            {
                ReleaseHandle();
            }

            _hasDragStart = false;
            if (_isDragging)
            {
                StopDragging();
                ClearSelection();
            }

            return false;
        }
        
        // Ensure all selected paths are highlighted
        foreach (RoadPath? path in _selectedPaths)
            path.highlightVertex();

        // Start a new selection rectangle with middle mouse button
        if (LevelVisibility.roadsVisible && Input.GetMouseButtonDown(2) && !_isSelecting)
        {
            _startScreenPos = Input.mousePosition;
            _isAddingToSelection = KeybindManager.IsHeld(KeybindIds.RoadsAddToSelection);
            _isSelecting = true;
        }

        // End selection rectangle on mouse release
        if (Input.GetMouseButtonUp(2))
        {
            _isSelecting = false;
            _isAddingToSelection = false;
        }

        // Handle deletion of selected joints when pressing Delete
        if (_selectedJoints.Count > 0 && (KeybindManager.IsDown(KeybindIds.RoadsDelete) || KeybindManager.IsDown(KeybindIds.RoadsDeleteAlt)))
        {
            // Group joints by road so we can delete them in batches
            Dictionary<Road, List<RoadJointCustom>> jointsByRoad = new();
            foreach (RoadJointCustom joint in _selectedJoints)
            {
                if (!jointsByRoad.ContainsKey(joint.Road))
                    jointsByRoad[joint.Road] = [];
                jointsByRoad[joint.Road].Add(joint);
            }

            // Remove vertices in reverse index order to avoid shifting indices, because when removing indexes, the rest shifts down.
            foreach (KeyValuePair<Road, List<RoadJointCustom>> kvp in jointsByRoad)
            {
                List<RoadJointCustom>? joints = kvp.Value;
                joints.Sort((a, b) => b.Index.CompareTo(a.Index));

                foreach (RoadJointCustom joint in joints)
                    if (joint.Index >= 0 && joint.Index < joint.Road.joints.Count)
                        joint.Road.removeVertex(joint.Index);
            }

            Clear();
        }

        _handles.snapPositionInterval = _snapTransform;
        _handles.snapRotationIntervalDegrees = _snapRotation;
        if (_dragMode == EDragMode.TRANSFORM)
        {
            _handles.SetPreferredMode(TransformHandles.EMode.Position);
        }
        if (_dragMode == EDragMode.ROTATE)
        {
            _handles.SetPreferredMode(TransformHandles.EMode.Rotation);
        }

        bool selectingHandle = EditorRoads.selection && _handles.Raycast(EditorInteract.ray);
        if (_roadSelection != null && EditorRoads.road != null)
        {
            _handles.Render(EditorInteract.ray);
            Road road = EditorRoads.road;
            
            float radius = 0f;
            if (road._roadAssetRef.IsAssigned)
            {
                radius = road._roadAsset.Width / 2;
            }
            else if (road.material < LevelRoads.materials.Length)
            {
                radius = LevelRoads.materials[road.material].width;
            }

            if (_depthToggleButton.Value)
            {
                if (road._roadAssetRef.IsAssigned)
                {
                    radius += road._roadAsset.Depth / 2;
                }
                else if (road.material < LevelRoads.materials.Length)
                {
                    radius += LevelRoads.materials[road.material].depth;
                }
            }

            Quaternion rotation = _roadSelection.Transform.rotation;
            DrawRotationCircle(rotation * Vector3.right, rotation * Vector3.forward, radius, new Color(0, 1f, 0, 0.4f));
            DrawRotationCircle(rotation * Vector3.right, rotation * Vector3.up, radius, new Color(0, 0, 1f, 0.4f));
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

        if (InputEx.GetKeyDown(ControlsSettings.tool_0) && _dragMode != EDragMode.TRANSFORM)
        {
            _dragMode = EDragMode.TRANSFORM;
        }

        if (InputEx.GetKeyDown(ControlsSettings.tool_1) && _dragMode != EDragMode.ROTATE)
        {
            _dragMode = EDragMode.ROTATE;
        }

        if (KeybindManager.IsDown(KeybindIds.RoadsUndo))
        {
            Undo();
        }

        if (KeybindManager.IsDown(KeybindIds.RoadsRedo))
        {
            Redo();
        }
        
        if (!_isUsingHandle)
        {
            if (InputEx.GetKeyDown(ControlsSettings.primary))
            {
                if (selectingHandle)
                {
                    _handles.MouseDown(EditorInteract.ray);
                    _isUsingHandle = true;
                }
                else
                {
                    if (!_isDragging)
                    {
                        _hasDragStart = true;
                        _dragStartViewportPoint = InputEx.NormalizedMousePosition;
                        _dragStartScreenPoint = Input.mousePosition;
                    }

                    if (!InputEx.GetKey(ControlsSettings.modify))
                    {
                        ClearSelection();
                    }
                }
            }
            else if (InputEx.GetKey(ControlsSettings.primary) && _hasDragStart)
            {
                _dragEndViewportPoint = InputEx.NormalizedMousePosition;
                _dragEndScreenPoint = Input.mousePosition;
                if (_isDragging || Mathf.Abs(_dragEndScreenPoint.x - _dragStartScreenPoint.x) > 50f ||
                    Mathf.Abs(_dragEndScreenPoint.x - _dragStartScreenPoint.x) > 50f)
                {
                    Vector2 min = _dragStartViewportPoint;
                    Vector2 max = _dragEndViewportPoint;
                    if (max.x < min.x)
                    {
                        (max.x, min.x) = (min.x, max.x);
                    }

                    if (max.y < min.y)
                    {
                        (max.y, min.y) = (min.y, max.y);
                    }

                    if (!_isDragging)
                    {
                        _isDragging = true;
                        _editorDrag = null;
                        Vector3 newScreen = MainCamera.instance.WorldToViewportPoint(EditorRoads.selection?.position ?? Vector3.zero);
                        if (!(newScreen.z < 0f))
                        {
                            _editorDrag = new EditorDrag(EditorRoads.selection, newScreen);
                        }
                    }

                    if (!InputEx.GetKey(ControlsSettings.modify))
                    {
                        Vector3 vector = MainCamera.instance.WorldToViewportPoint(EditorRoads.selection?.transform.position ?? Vector3.zero);
                        if (vector.z < 0f)
                        {
                            ClearSelection();
                        }
                        else if (vector.x < min.x || vector.y < min.y || vector.x > max.x || vector.y > max.y)
                        {
                            ClearSelection();
                        }
                    }

                    if (_editorDrag != null && EditorRoads.selection != _editorDrag.transform && _editorDrag.screen.x > min.x &&
                        _editorDrag.screen.y > min.y && _editorDrag.screen.x < max.x && _editorDrag.screen.y < max.y)
                    {
                        EditorRoads.select(_editorDrag.transform);
                    }

                    return false;
                }
            }

            if (EditorRoads.selection != null)
            {
                if (KeybindManager.IsDown(KeybindIds.RoadsCopyTransform))
                {
                    _copyPosition = _handles.GetPivotPosition();
                }

                if (KeybindManager.IsDown(KeybindIds.RoadsPasteTransform) && _copyPosition != Vector3.zero)
                {
                    if (EditorRoads.road != null)
                    {
                        if (EditorRoads.tangentIndex > -1)
                        {
                            EditorRoads.road.moveTangent(EditorRoads.vertexIndex, EditorRoads.tangentIndex, _copyPosition - EditorRoads.joint.vertex);
                        }
                        else if (EditorRoads.vertexIndex > -1)
                        {
                            EditorRoads.road.moveVertex(EditorRoads.vertexIndex, _copyPosition);
                        }
                    }

                    CalculateHandleOffsets();
                }

                if (InputEx.GetKeyDown(ControlsSettings.tool_2) && EditorInteract.worldHit.transform != null)
                {
                    Select();
                    Vector3 point = EditorInteract.worldHit.point;
                    if (InputEx.GetKey(ControlsSettings.snap))
                    {
                        point += EditorInteract.worldHit.normal * _snapTransform;
                    }

                    Quaternion pivotRotation = _handles.GetPivotRotation();
                    _handles.ExternallyTransformPivot(point, pivotRotation, modifyRotation: false);
                }

                if (InputEx.GetKeyDown(ControlsSettings.focus))
                {
                    MainCamera.instance.transform.parent.position = _handles.GetPivotPosition() - 15f * MainCamera.instance.transform.forward;
                }
            }
        }
        
        if (EditorInteract.worldHit.transform != null)
        {
            EditorRoads.highlighter.gameObject.SetActive(!_isDragging && !_isUsingHandle);
            EditorRoads.highlighter.position = EditorInteract.worldHit.point;
        }

        return true;
    }

    public void PostCustomUpdate()
    {
        if (InputEx.GetKeyUp(ControlsSettings.primary))
        {
            _hasDragStart = false;
            if (_isDragging)
            {
                StopDragging();
            }
        }
    }
    
    public void Select()
    {
        if (!EditorRoads.selection)
        {
            _roadSelection = null;
            return;
        }

        RoadJoint joint = EditorRoads.joint;
        Vector3[] tangents = [Vector3.zero, Vector3.zero];
        if (joint != null)
        {
            tangents = [joint.getTangent(0), joint.getTangent(1)];
        }
        
        _roadSelection = new RoadSelection(EditorRoads.selection, tangents);
        CalculateHandleOffsets();
    }
    
    private void DrawRotationCircle(Vector3 axis0, Vector3 axis1, float radius, Color color)
    {
        if (_roadSelection == null) return;
        RuntimeGizmos.Get().Circle(_roadSelection.Transform.position, axis0, axis1, radius, color, 0f, 12, EGizmoLayer.Foreground);
    }
    
    private void ReleaseHandle()
    {
        _isUsingHandle = false;
        _handles.MouseUp();
        _step++;

        Vector3 toPosition = Vector3.zero;
        if (EditorRoads.tangentIndex > -1)
        {
            toPosition = EditorRoads.road.joints[EditorRoads.vertexIndex].tangents[EditorRoads.tangentIndex];
        }
        else if (EditorRoads.vertexIndex > -1)
        {
            toPosition = EditorRoads.road.joints[EditorRoads.vertexIndex].vertex;
        }

        if (_roadSelection == null) return;
        Register(new ReunRoadTransform(_step, _roadSelection.FromPosition, toPosition, EditorRoads.vertexIndex, EditorRoads.tangentIndex));
    }
    
    private void StopDragging()
    {
        _dragStartViewportPoint = Vector2.zero;
        _dragStartScreenPoint = Vector2.zero;
        _dragEndViewportPoint = Vector2.zero;
        _dragEndScreenPoint = Vector2.zero;
        _isDragging = false;
    }
    
    public void ClearSelection()
    {
        _roadSelection = null;
        CalculateHandleOffsets();
    }
    
    private void Clear()
    {
        foreach (Road? road in LevelRoads.roads)
        {
            foreach (RoadPath? path in road.paths)
                path.unhighlightVertex();
        }

        _selectedJoints.Clear();
        _selectedPaths.Clear();
        _primaryJoint = null;
        _otherOffsets.Clear();

        EditorRoads.deselect();
    }
    
    private void Register(IReun newReun)
    {
        if (_frame > 0)
        {
            _reun = new IReun[_reun.Length];
            _frame = 0;
        }

        for (int num = _reun.Length - 1; num > 0; num--)
        {
            _reun[num] = _reun[num - 1];
        }

        _reun[0] = newReun;
    }
    
    private void Undo()
    {
        while (_frame <= _reun.Length - 1)
        {
            if (_reun[_frame] != null)
            {
                _reun[_frame].undo();
            }
            if (_frame < _reun.Length - 1 && _reun[_frame + 1] != null)
            {
                _frame++;
                if (_reun[_frame].step != _step)
                {
                    _step--;
                    break;
                }
                continue;
            }
            break;
        }

        CalculateHandleOffsets();
    }

    private void Redo()
    {
        while (_frame >= 0)
        {
            if (_reun[_frame] != null)
            {
                _reun[_frame].redo();
            }
            if (_frame > 0 && _reun[_frame - 1] != null)
            {
                _frame--;
                if (_reun[_frame].step != _step)
                {
                    _step++;
                    break;
                }

                continue;
            }
            break;
        }

        CalculateHandleOffsets();
    }
    
    private Rect GetScreenRect(Vector2 start, Vector2 end)
    {
        start.y = Screen.height - start.y;
        end.y = Screen.height - end.y;

        return new Rect(
            Mathf.Min(start.x, end.x),
            Mathf.Min(start.y, end.y),
            Mathf.Abs(start.x - end.x),
            Mathf.Abs(start.y - end.y)
        );
    }
    
    /// <summary>
    /// Selects road joints that fall within the current selection rectangle.
    /// </summary>
    private void SelectObjectsInRect()
    {
        // If not holding Shift, clear previous selection.
        if (!_isAddingToSelection)
            Clear();

        Transform? currentSelection = EditorRoads.selection;
        RoadJointCustom? selectedJoint = null;

        List<RoadJointCustom> newJoints = [];
        List<RoadPath> newPaths = [];

        foreach (Road? road in LevelRoads.roads)
        {
            for (int index = 0; index < road.joints.Count; index++)
            {
                RoadJoint? joint = road.joints[index];
                Vector3 screenPoint = _sceneCamera!.WorldToScreenPoint(joint.vertex);

                if (screenPoint.z <= 0f)
                    continue;

                Vector2 screen2D = new(screenPoint.x, Screen.height - screenPoint.y);

                if (_selectionRect.Contains(screen2D))
                {
                    RoadJointCustom roadJoint = new()
                    {
                        Road = road,
                        Index = index,
                        Vertex = joint.vertex,
                        TangentPositions = [joint.getTangent(0), joint.getTangent(1)]
                    };

                    // Skip if already selected
                    if (_selectedJoints.Exists(j => j.Road == roadJoint.Road && j.Index == roadJoint.Index))
                        continue;

                    newJoints.Add(roadJoint);
                    newPaths.Add(road.paths[index]);

                    if (currentSelection != null && road.paths[index].vertex == currentSelection)
                        selectedJoint = roadJoint;
                }
            }
        }

        // Add any newly selected nodes
        _selectedJoints.AddRange(newJoints);
        _selectedPaths.AddRange(newPaths);

        if (_selectedJoints.Count > 0)
        {
            // Determine which joint is primary for movement
            if (!_primaryJoint.HasValue && !_isAddingToSelection && selectedJoint.HasValue)
                _primaryJoint = selectedJoint.Value;
            else
            {
                _primaryJoint = _selectedJoints[0];
                Transform? vertex = _primaryJoint.Value.Road.paths[_primaryJoint.Value.Index].vertex;
                EditorRoads.deselect();
                EditorRoads.select(vertex);
            }

            _primaryLastPosition = _primaryJoint.Value.Vertex;

            // Compute offsets for moving other joints relative to the primary
            _otherOffsets.Clear();
            foreach (RoadJointCustom joint in _selectedJoints)
            {
                if (joint.Road != _primaryJoint.Value.Road || joint.Index != _primaryJoint.Value.Index)
                    _otherOffsets[joint] = joint.Vertex - _primaryLastPosition;
            }
        }
        else
            // If nothing is selected now, clear any active selection in EditorRoads
        if (_primaryJoint.HasValue)
            EditorRoads.deselect();
    }
    
    private void CalculateHandleOffsets()
    {
        if (EditorRoads.selection == null)
        {
            return;
        }

        if (_dragCoordinate == EDragCoordinate.GLOBAL)
        {
            Vector3 zero = EditorRoads.selection.transform.position;
            _handles.SetPreferredPivot(zero, Quaternion.identity);
        }
        else
        {
            Road road = EditorRoads.road;

            _handles.SetPreferredPivot(EditorRoads.selection.position, CalculateLocalPoint(road, EditorRoads.path));
        }
    }
    
    private Quaternion CalculateLocalPoint(Road road, RoadPath actualPath)
    {
        if (road.paths.Count < 2) return actualPath.vertex.rotation;

        int actualIndex = road.paths.IndexOf(actualPath);
        int nextPathIndex = actualIndex;
        if (nextPathIndex >= 0 && nextPathIndex < road.paths.Count - 1)
        {
            nextPathIndex += 1;
        }
        else
        {
            nextPathIndex -= 1;
        }

        Vector3 position = GetBezierPoint(road.paths[nextPathIndex].vertex.position,
            road.paths[nextPathIndex].tangents[nextPathIndex > actualIndex ? 0 : 1].position,
            actualPath.vertex.position, actualPath.tangents[nextPathIndex > actualIndex ? 1 : 0].position, 0.75f);
        return nextPathIndex > actualIndex
            ? Quaternion.LookRotation(actualPath.vertex.position - position)
            : Quaternion.LookRotation(position - actualPath.vertex.position);
    }
    
    private Vector3 GetBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 point = uuu * p0; // (1 - t)^3 * p0
        point += 3 * uu * t * p1; // 3(1 - t)^2 * t * p1
        point += 3 * u * tt * p2; // 3(1 - t) * t^2 * p2
        point += ttt * p3; // t^3 * p3

        return point;
    }
    #endregion Extension Functions

    public void Dispose()
    {
        if (_container == null) return;
        _sceneCamera = null;
        _container.RemoveChild(_coordinateButton);
        _container.RemoveChild(_depthToggleButton);
        _container.RemoveChild(_snapTransformField);
        _container.RemoveChild(_handlePrioritizeToggleButton);
        
        _handles.OnPreTransform -= OnHandlePreTransform;
        _handles.OnTranslatedAndRotated -= OnHandleTranslatedAndRotated;
        _handles.OnTransformed -= OnHandleTransformed;
        _coordinateButton.onSwappedState = null;
        _snapTransformField.OnValueChanged -= OnSnapTransformFieldValueChanged;
        Camera.onPostRender -= OnPostRender;
        
        EditorRoadsPatches.OnRoadSelected -= OnRoadSelected;
        EditorRoadsPatches.OnRoadDeselected -= OnRoadDeselected;
    }
}
