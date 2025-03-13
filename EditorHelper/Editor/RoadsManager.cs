using System;
using EditorHelper.Builders;
using EditorHelper.Models;
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
    // Not used but kept for consistency with the EditorObjects
    private readonly float _snapTransform;
    private readonly float _snapRotation;

    private RoadSelection _roadSelection;
    private EditorDrag _editorDrag;

    private Vector3 _copyPosition = Vector3.zero;

    private readonly SleekButtonState _coordinateButton;
    private readonly ISleekToggle _depthToggleButton;

    public RoadsManager()
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

        ButtonBuilder builder = new(40f, 40f);
        builder.SetPositionOffsetX(5f);
        builder.SetPositionOffsetY(-260f);

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
        
        bundle.unload();
    }
    
    public void Initialize()
    {
        EditorUI.window.AddChild(_depthToggleButton);
        EditorUI.window.AddChild(_coordinateButton);
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

    public void CustomUpdate()
    {
        bool buttonVisible = (EditorEnvironmentRoadsUI.active && EditorRoads.selection != null);

        _depthToggleButton.IsVisible = buttonVisible;
        _coordinateButton.IsVisible = buttonVisible;
        
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

        bool flag = EditorRoads.selection && _handles.Raycast(EditorInteract.ray);
        if (EditorRoads.selection)
        {
            _handles.Render(EditorInteract.ray);
            RoadMaterial roadMaterial = LevelRoads.materials[EditorRoads.selected];
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

        if (!_isUsingHandle)
        {
            if (InputEx.GetKeyDown(ControlsSettings.primary))
            {
                if (flag)
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

                    //onDragStarted?.Invoke(min, max);
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
            EditorRoads.highlighter.gameObject.SetActive(!_isDragging);
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

        if (!InputEx.GetKeyDown(ControlsSettings.primary))
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
            if (!EditorInteract.worldHit.transform || flag)
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

    public void ClearSelection()
    {
        _roadSelection = null;
        CalculateHandleOffsets();
    }

    private void DrawRotationCircle(Vector3 axis0, Vector3 axis1, float radius, Color color)
    {
        RuntimeGizmos.Get().Circle(EditorRoads.selection.position, axis0, axis1, radius, color, 0f, 32, EGizmoLayer.Foreground);
    }
    
    private void OnHandlePreTransform(Matrix4x4 worldToPivot)
    {
        _roadSelection.fromPosition = _roadSelection.transform.position;
        _roadSelection.fromRotation = _roadSelection.transform.rotation;
        _roadSelection.fromScale = _roadSelection.transform.localScale;
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
        //_roadSelection.transform.position = matrix.GetPosition();
        //_roadSelection.transform.SetRotation_RoundIfNearlyAxisAligned(matrix.GetRotation());
        //_roadSelection.transform.SetLocalScale_RoundIfNearlyEqualToOne(matrix.lossyScale);
        
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
            Quaternion rotation = EditorRoads.selection.rotation;
            if (EditorRoads.road.joints.Count > 1 && EditorRoads.vertexIndex > 0)
            {
                Vector3 forward = EditorRoads.road.joints[EditorRoads.vertexIndex - 1].vertex - EditorRoads.joint.vertex;
                rotation = Quaternion.LookRotation(forward);
            }
            
            _handles.SetPreferredPivot(EditorRoads.selection.position, rotation);
        }
    }
}