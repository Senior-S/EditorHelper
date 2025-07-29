using System;
using System.Collections.Generic;
using System.Linq;
using EditorHelper.Builders;
using EditorHelper.Models;
using SDG.Framework.Rendering;
using SDG.Framework.Utilities;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Editor;

public class RoadsManager
{
    private readonly TransformHandles _handles;
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
    // Not used but kept for consistency with the EditorObjects
    private readonly float _snapRotation;
    // Used for Ctrl+Z
    private int _step;
    private IReun[] _reun = [];
    private int _frame;
    //private Vector3 _fromPosition = Vector3.zero;
    //private Vector3 _toPosition = Vector3.zero;

    private RoadSelection _roadSelection;
    private EditorDrag _editorDrag;
    
    private Vector3 _copyPosition = Vector3.zero;

    private readonly SleekButtonState _coordinateButton;
    private readonly ISleekToggle _depthToggleButton;
    private readonly ISleekFloat32Field _snapTransformField;
    private readonly ISleekToggle _handlePriorizeToggleButton;
    
    // Road Selection
    // State for drag selection rectangle
    private Vector2 _startScreenPos;
    private bool _isSelecting;
    private bool _isAddingToSelection;
    private Rect _selectionRect;

    // Currently selected joints and their associated paths
    public static List<RoadJointCustom> SelectedJoints = new();
    public static List<RoadPath> SelectedPaths = new();

    private Camera? _sceneCamera;

    // Information about the primary joint (the pivot for moving multiple joints)
    public static RoadJointCustom? _primaryJoint;
    private Vector3 _primaryLastPosition;

    // Stores relative offsets for multi-joint movement
    public static readonly Dictionary<RoadJointCustom, Vector3> _otherOffsets = new();

    public RoadsManager()
    {
        _sceneCamera = Camera.main;
        
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
        _step = 0;
        _frame = 0;

        UIBuilder builder = new(40f, 40f);
        builder.SetPositionOffsetX(5f);
        builder.SetPositionOffsetY(-330f);

        builder.SetText("Radius includes depth");
        _depthToggleButton = builder.BuildToggle("Should the display radius of the road include the depth?");

        builder.SetPositionOffsetX(10f);
        builder.SetOneTimeSpacing(30f);
        builder.SetSizeOffsetX(200f)
            .SetSizeOffsetY(30f);
        
        Local local = Localization.read("/Editor/EditorLevelObjects.dat"); // Just to keep consistency in case client have a translations mod.
        Bundle bundle = Bundles.getBundle("/Bundles/Textures/Edit/Icons/EditorLevelObjects/EditorLevelObjects.unity3d");
        builder.SetText(local.format("CoordinateButtonTooltip"));
        _coordinateButton = builder.BuildButtonState(
            new GUIContent(local.format("CoordinateButtonTextGlobal"), bundle.load<Texture>("Global")),
            new GUIContent(local.format("CoordinateButtonTextLocal"), bundle.load<Texture>("Local")));
        _coordinateButton.onSwappedState = OnSwappedState;

        builder.SetText(local.format("SnapTransformLabelText"));
        _snapTransformField = builder.BuildFloatInput(ESleekSide.RIGHT);
        _snapTransformField.OnValueChanged += OnSnapTransformFieldValueChanged;

        builder.SetText("Prioritize handle")
            .SetSizeOffsetX(40f)
            .SetSizeOffsetY(40f);
        _handlePriorizeToggleButton = builder.BuildToggle("When clicking should the click prioritize the arrows?");
        _handlePriorizeToggleButton.Value = true;
        
        bundle.unload();
        if (Level.isEditor)
        {
            _reun = new IReun[256];
            _step = 0;
            _frame = 0;
        }
    }
    
    public void Initialize()
    {
        EditorUI.window.AddChild(_depthToggleButton);
        EditorUI.window.AddChild(_coordinateButton);
        _snapTransformField.Value = _snapTransform;
        EditorUI.window.AddChild(_snapTransformField);
        EditorUI.window.AddChild(_handlePriorizeToggleButton);
    }
    
    private void OnSnapTransformFieldValueChanged(ISleekFloat32Field field, float value)
    {
        _snapTransform = value;
    }

    private void OnSwappedState(SleekButtonState button, int index)
    {
        _dragCoordinate = (EDragCoordinate)index;
        CalculateHandleOffsets();
    }

    public void Select()
    {
        if (!EditorRoads.selection)
        {
            _roadSelection = null;
            return;
        }

        _roadSelection = new RoadSelection(EditorRoads.selection);
        CalculateHandleOffsets();
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

    public void CustomUpdate()
    {
        bool buttonVisible = (EditorEnvironmentRoadsUI.active && EditorRoads.selection != null);

        _depthToggleButton.IsVisible = buttonVisible;
        _coordinateButton.IsVisible = buttonVisible;
        _snapTransformField.IsVisible = buttonVisible;
        _handlePriorizeToggleButton.IsVisible = buttonVisible;
        
        // Road Selection
        if (!EditorRoads.isPaving)
        {
            if (SelectedJoints.Count > 0)
                clear();
            return;
        }
        
        foreach (var road in LevelRoads.roads)
        {
            for (int i = 0; i < road.paths.Count; i++)
            {
                var path = road.paths[i];
                var t0 = path.tangents[0];
                var t1 = path.tangents[1];

                RuntimeGizmos.Get().Cube(path.vertex.position, 1.3f, Color.yellow);
                RuntimeGizmos.Get().Cube(t0.position, 1.3f, Color.white);
                RuntimeGizmos.Get().Cube(t1.position, 1.3f, Color.blue);

                RuntimeGizmos.Get().Line(path.vertex.position + Vector3.up * 0.1f, t0.position + Vector3.up * 0.1f, Color.red);
                RuntimeGizmos.Get().Line(path.vertex.position + Vector3.up * 0.1f, t1.position + Vector3.up * 0.1f, Color.red);
            }
        }
        
        // While dragging, update the selection rectangle and highlight nodes under it
        if (_isSelecting)
        {
            _selectionRect = GetScreenRect(_startScreenPos, Input.mousePosition);
            SelectObjectsInRect();
        }
        
        // Ensure all selected paths are highlighted
            foreach (var path in SelectedPaths)
                path.highlightVertex();

            // Start a new selection rectangle with middle mouse button
            if (Input.GetMouseButtonDown(2) && !_isSelecting)
            {
                _startScreenPos = Input.mousePosition;
                _isAddingToSelection = Input.GetKey(KeyCode.LeftShift);
                _isSelecting = true;
            }

            // End selection rectangle on mouse release
            if (Input.GetMouseButtonUp(2))
            {
                _isSelecting = false;
                _isAddingToSelection = false;
            }

            // Handle deletion of selected joints when pressing Delete
            if (SelectedJoints.Count > 0 && Input.GetKeyDown(KeyCode.Delete))
            {
                // Group joints by road so we can delete them in batches
                var jointsByRoad = new Dictionary<Road, List<RoadJointCustom>>();
                foreach (var joint in SelectedJoints)
                {
                    if (!jointsByRoad.ContainsKey(joint.road))
                        jointsByRoad[joint.road] = new List<RoadJointCustom>();
                    jointsByRoad[joint.road].Add(joint);
                }

                // Remove vertices in reverse index order to avoid shifting indices, because when removing indexes, the rest shifts down.
                foreach (var kvp in jointsByRoad)
                {
                    var joints = kvp.Value;
                    joints.Sort((a, b) => b.index.CompareTo(a.index));

                    foreach (var joint in joints)
                        if (joint.index >= 0 && joint.index < joint.road.joints.Count)
                            joint.road.removeVertex(joint.index);
                }

                clear();
            }

            // Handle dragging/moving multiple selected joints
            if (_primaryJoint.HasValue)
            {
                var joint = _primaryJoint.Value;
                Vector3 current = joint.road.joints[joint.index].vertex;

                if (current != _primaryLastPosition)
                {
                    foreach (var kvp in _otherOffsets)
                    {
                        var targetJoint = kvp.Key;
                        Vector3 offset = kvp.Value;
                        Vector3 newPosition = current + offset;
                        targetJoint.road.moveVertex(targetJoint.index, newPosition);
                    }

                    _primaryLastPosition = current;
                }
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

            return;
        }

        _handles.snapPositionInterval = _snapTransform;
        _handles.snapRotationIntervalDegrees = _snapRotation;
        if (_dragMode == EDragMode.TRANSFORM)
        {
            _handles.SetPreferredMode(TransformHandles.EMode.Position);
        }

        bool selectingHandle = EditorRoads.selection && _handles.Raycast(EditorInteract.ray);
        if (EditorRoads.selection && EditorRoads.road != null)
        {
            _handles.Render(EditorInteract.ray);
            RoadMaterial roadMaterial = LevelRoads.materials[EditorRoads.road.material];
            Quaternion rotation = EditorRoads.selection.rotation;

            float radius = roadMaterial.width;
            if (_depthToggleButton.Value)
            {
                radius += roadMaterial.depth;
            }
            DrawRotationCircle(rotation * Vector3.up, rotation * Vector3.forward, radius, new Color(1f, 0, 0, 0.4f));
            DrawRotationCircle(rotation * Vector3.right, rotation * Vector3.forward, radius, new Color(0, 1f, 0, 0.4f));
            DrawRotationCircle(rotation * Vector3.right, rotation * Vector3.up, radius, new Color(0, 0, 1f, 0.4f));
        }

        if (_isUsingHandle)
        {
            if (!InputEx.GetKey(ControlsSettings.primary))
            {
                ReleaseHandle();
                return;
            }
            
            _handles.wantsToSnap = InputEx.GetKey(ControlsSettings.snap);
            _handles.MouseMove(EditorInteract.ray);
            return;
        }

        if (InputEx.GetKeyDown(ControlsSettings.tool_0) && _dragMode != EDragMode.TRANSFORM)
        {
            _dragMode = EDragMode.TRANSFORM;
        }
        if (InputEx.GetKeyDown(KeyCode.Z) && InputEx.GetKey(KeyCode.LeftControl))
        {
            Undo();
        }
        if (InputEx.GetKeyDown(KeyCode.X) && InputEx.GetKey(KeyCode.LeftControl))
        {
            Redo();
        }
        if (InputEx.GetKeyDown(KeyCode.R) && InputEx.GetKeyDown(KeyCode.LeftControl))
        {
            LevelRoads.bakeRoads();
            return;
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
                        
                        Vector3 newScreen = MainCamera.instance.WorldToViewportPoint(EditorRoads.selection.position);
                        if (!(newScreen.z < 0f))
                        {
                            _editorDrag = new EditorDrag(EditorRoads.selection, newScreen);
                        }
                    }
                    if (!InputEx.GetKey(ControlsSettings.modify))
                    {
                        Vector3 vector = MainCamera.instance.WorldToViewportPoint(EditorRoads.selection.transform.position);
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

                    return;
                }
            }
            if (EditorRoads.selection != null)
            {
                if (InputEx.GetKeyDown(KeyCode.B) && InputEx.GetKey(KeyCode.LeftControl))
                {
                    _copyPosition = _handles.GetPivotPosition();
                }
                if (InputEx.GetKeyDown(KeyCode.N) && _copyPosition != Vector3.zero && InputEx.GetKey(KeyCode.LeftControl))
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

        #region Original Update Code
        if (EditorInteract.worldHit.transform != null)
        {
            EditorRoads.highlighter.gameObject.SetActive(!_isDragging && !_isUsingHandle);
            EditorRoads.highlighter.position = EditorInteract.worldHit.point;
        }

        if ((InputEx.GetKeyDown(KeyCode.Delete) || InputEx.GetKeyDown(KeyCode.Backspace)) && EditorRoads.selection != null &&
            EditorRoads.road != null)
        {
            if (InputEx.GetKey(ControlsSettings.other))
            {
                LevelRoads.removeRoad(EditorRoads.road);
            }
            else
            {
                EditorRoads.road.removeVertex(EditorRoads.vertexIndex);
            }

            EditorRoads.deselect();
        }

        if (InputEx.GetKeyDown(ControlsSettings.tool_2) && EditorInteract.worldHit.transform != null)
        {
            Vector3 point = EditorInteract.worldHit.point;
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
        }

        if (!InputEx.GetKeyDown(ControlsSettings.primary) || (selectingHandle && _handlePriorizeToggleButton.Value))
        {
            return;
        }

        if (EditorInteract.logicHit.transform != null)
        {
            if (EditorInteract.logicHit.transform.name.IndexOf("Path", StringComparison.Ordinal) != -1 ||
                EditorInteract.logicHit.transform.name.IndexOf("Tangent", StringComparison.Ordinal) != -1)
            {
                EditorRoads.select(EditorInteract.logicHit.transform);
            }
        }
        else
        {
            if (!EditorInteract.worldHit.transform)
            {
                return;
            }
            
            Vector3 point2 = EditorInteract.worldHit.point;
            if (EditorRoads.road != null)
            {
                if (EditorRoads.tangentIndex > -1)
                {
                    EditorRoads.select(EditorRoads.road.addVertex(EditorRoads.vertexIndex + EditorRoads.tangentIndex, point2));
                    return;
                }

                float num = Vector3.Dot(point2 - EditorRoads.joint.vertex, EditorRoads.joint.getTangent(0));
                float num2 = Vector3.Dot(point2 - EditorRoads.joint.vertex, EditorRoads.joint.getTangent(1));
                if (num > num2)
                {
                    EditorRoads.select(EditorRoads.road.addVertex(EditorRoads.vertexIndex, point2));
                }
                else
                {
                    EditorRoads.select(EditorRoads.road.addVertex(EditorRoads.vertexIndex + 1, point2));
                }
            }
            else
            {
                EditorRoads.select(LevelRoads.addRoad(point2));
            }
        }
        
        if (InputEx.GetKeyUp(ControlsSettings.primary))
        {
            _hasDragStart = false;
            if (_isDragging)
            {
                StopDragging();
            }
        }

        #endregion
    }
    
    // This is kind of taken from the vanilla SelectionTool
    private void OnRenderObject()
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

        GL.Vertex(v0); GL.Vertex(v1);
        GL.Vertex(v1); GL.Vertex(v2);
        GL.Vertex(v2); GL.Vertex(v3);
        GL.Vertex(v3); GL.Vertex(v0);

        GL.End();
    }

    /// <summary>
    /// Selects road joints that fall within the current selection rectangle.
    /// </summary>
    private void SelectObjectsInRect()
    {
        // If not holding Shift, clear previous selection.
        // I am too lazy to implement real-time clearing for shift-select.
        if (!_isAddingToSelection)
            clear();

        Transform? currentSelection = EditorRoads.selection;
        RoadJointCustom? selectedJoint = null;

        var newJoints = new List<RoadJointCustom>();
        var newPaths = new List<RoadPath>();

        foreach (var road in LevelRoads.roads)
        {
            for (int index = 0; index < road.joints.Count; index++)
            {
                var joint = road.joints[index];
                Vector3 screenPoint = _sceneCamera!.WorldToScreenPoint(joint.vertex);

                if (screenPoint.z <= 0f)
                    continue;

                Vector2 screen2D = new(screenPoint.x, Screen.height - screenPoint.y);

                if (_selectionRect.Contains(screen2D))
                {
                    var roadJoint = new RoadJointCustom()
                    {
                        road = road,
                        index = index,
                        vertex = joint.vertex
                    };

                    // Skip if already selected
                    if (SelectedJoints.Exists(j => j.road == roadJoint.road && j.index == roadJoint.index))
                        continue;

                    newJoints.Add(roadJoint);
                    newPaths.Add(road.paths[index]);

                    if (currentSelection != null && road.paths[index].vertex == currentSelection)
                        selectedJoint = roadJoint;
                }
            }
        }

        // Add any newly selected nodes
        SelectedJoints.AddRange(newJoints);
        SelectedPaths.AddRange(newPaths);

        if (SelectedJoints.Count > 0)
        {
            // Determine which joint is primary for movement
            if (_primaryJoint.HasValue && _isAddingToSelection)
                selectedJoint = _primaryJoint;
            
            else if (selectedJoint.HasValue)
                _primaryJoint = selectedJoint.Value;
            
            else
            {
                _primaryJoint = SelectedJoints[0];
                var vertex = _primaryJoint.Value.road.paths[_primaryJoint.Value.index].vertex;
                EditorRoads.deselect();
                EditorRoads.select(vertex);
            }

            _primaryLastPosition = _primaryJoint.Value.vertex;

            // Compute offsets for moving other joints relative to the primary
            _otherOffsets.Clear();
            foreach (var joint in SelectedJoints)
                if (joint.road != _primaryJoint.Value.road || joint.index != _primaryJoint.Value.index)
                    _otherOffsets[joint] = joint.vertex - _primaryLastPosition;
        }
        else
            // If nothing is selected now, clear any active selection in EditorRoads
            if (_primaryJoint.HasValue)
                EditorRoads.deselect();
    }
    
    /// <summary>
    /// Creates a Rect from two screen points, adjusting for Unity's flipped Y coordinates in GUI.
    /// </summary>
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
        
    private void clear()
    {
        foreach (var road in LevelRoads.roads)
        foreach (var path in road.paths)
            path.unhighlightVertex(); 

        SelectedJoints.Clear();
        SelectedPaths.Clear();
        _primaryJoint = null;
        _otherOffsets.Clear();

        EditorRoads.deselect();
    }

    public void ClearSelection()
    {
        _roadSelection = null;
        CalculateHandleOffsets();
    }

    private void DrawRotationCircle(Vector3 axis0, Vector3 axis1, float radius, Color color)
    {
        RuntimeGizmos.Get().Circle(EditorRoads.selection.position, axis0, axis1, radius, color, 0f, 12, EGizmoLayer.Foreground);
    }
    
    private void OnHandlePreTransform(Matrix4x4 worldToPivot)
    {
        _roadSelection ??= new RoadSelection(EditorRoads.selection);
        
        _roadSelection.fromPosition = _roadSelection.transform.position;
        _roadSelection.fromRotation = _roadSelection.transform.rotation;
        _roadSelection.relativeToPivot = worldToPivot * _roadSelection.transform.localToWorldMatrix;
    }

    private void OnHandleTranslatedAndRotated(Vector3 worldPositionDelta, Quaternion worldRotationDelta, Vector3 pivotPosition, bool modifyRotation)
    {
        Vector3 vector = _roadSelection.fromPosition - pivotPosition;
        Vector3 point = _roadSelection.fromPosition + worldPositionDelta;
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
            _roadSelection.transform.rotation = worldRotationDelta * _roadSelection.fromRotation;
        }

        CalculateHandleOffsets();
    }

    private void OnHandleTransformed(Matrix4x4 pivotToWorld)
    {
        Matrix4x4 matrix = pivotToWorld * _roadSelection.relativeToPivot;
        
        Vector3 point = matrix.GetPosition();
        float oldY = _roadSelection.transform.position.y;
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
        
        Register(new ReunRoadTransform(_step, _roadSelection.fromPosition, toPosition, EditorRoads.vertexIndex, EditorRoads.tangentIndex));
    }

    private void StopDragging()
    {
        _dragStartViewportPoint = Vector2.zero;
        _dragStartScreenPoint = Vector2.zero;
        _dragEndViewportPoint = Vector2.zero;
        _dragEndScreenPoint = Vector2.zero;
        _isDragging = false;
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
    
    // Cubic bezier formula
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
        point += ttt * p3;        // t^3 * p3

        return point;
    }

    private Quaternion CalculateLocalPoint(Road road, RoadPath actualPath)
    {
        if (road.paths.Count < 2) return actualPath.vertex.rotation;
        
        int actualIndex = road.paths.IndexOf(actualPath);
        int nextPathIndex = actualIndex;
        if (nextPathIndex >= 0 && nextPathIndex < road.paths.Count() - 1)
        {
            nextPathIndex += 1;
        }
        else
        {
            nextPathIndex -= 1;
        }
        
        Vector3 position = GetBezierPoint(road.paths[nextPathIndex].vertex.position, road.paths[nextPathIndex].tangents[nextPathIndex > actualIndex ? 0 : 1].position,
            actualPath.vertex.position, actualPath.tangents[nextPathIndex > actualIndex ? 1 : 0].position, 0.75f);
        return nextPathIndex > actualIndex 
            ? Quaternion.LookRotation(actualPath.vertex.position - position)
            : Quaternion.LookRotation(position - actualPath.vertex.position);
    }
}