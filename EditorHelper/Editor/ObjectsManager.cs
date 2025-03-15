using System;
using System.Collections.Generic;
using System.Linq;
using EditorHelper.Builders;
using HighlightingSystem;
using SDG.Unturned;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorHelper.Editor;

public class ObjectsManager
{
    private readonly List<Highlighter> _highlightedObjects = [];
    private readonly SleekButtonIcon _highlightButton;
    private readonly SleekButtonState _highlightColorsButton;
    
    private readonly SleekButtonIcon _filterButton;
    private readonly ISleekField _filterField;
    private string _filterText = string.Empty;

    private readonly SleekButtonIcon _adjacentPlaceButton;

    private readonly ISleekLabel _objectPositionLabel;
    private readonly ISleekFloat32Field _objectPositionX;
    private readonly ISleekFloat32Field _objectPositionY;
    private readonly ISleekFloat32Field _objectPositionZ;
    
    private readonly ISleekLabel _objectRotationLabel;
    private readonly ISleekFloat32Field _objectRotationX;
    private readonly ISleekFloat32Field _objectRotationY;
    private readonly ISleekFloat32Field _objectRotationZ;

    private readonly ISleekLabel _objectScaleLabel;
    private readonly ISleekFloat32Field _objectScaleX;
    private readonly ISleekFloat32Field _objectScaleY;
    private readonly ISleekFloat32Field _objectScaleZ;

    private readonly Color[] _highlightColors = [Color.yellow, Color.red, Color.magenta, Color.blue];
    private int _currentColorIndex = 0;
    private Transform _selectedObject = null;
    
    public ObjectsManager()
    {
        ButtonBuilder builder = new();
        
        builder.SetPositionOffsetX(210f)
            .SetPositionOffsetY(-30f)
            .SetText("Highlight objects");

        _highlightButton = builder.BuildButton("Highlight all objects of the selected type");
        _highlightButton.onClickedButton += HighlightObjects;

        builder.SetText("Change the highlight color");

        _highlightColorsButton = builder.BuildButtonState(new GUIContent("Yellow"), new GUIContent("Red"), new GUIContent("Purple"), new GUIContent("Blue"));
        _highlightColorsButton.onSwappedState = OnSwappedStateColor;
        
        builder.SetText("Filter objects");

        _filterButton = builder.BuildButton("Highlight all objects that derive from this mod.");
        _filterButton.onClickedButton += OnFilterClicked;

        builder.SetText("Mod ID");
        _filterField = builder.BuildStringField();
        _filterField.OnTextSubmitted += OnFilterFieldSubmitted;
        
        builder.SetPositionOffsetX(420f)
            .SetPositionOffsetY(-30f)
            .SetText("Place adjacent");
        _adjacentPlaceButton = builder.BuildButton("Place the selected object adjacent to the world selected object");
        _adjacentPlaceButton.onClickedButton += OnAdjacentPlaceClicked;
        
        builder.SetPositionOffsetX(20f)
            .SetPositionOffsetY(-390f)
            .SetSizeOffsetX(120f)
            .SetText("X:");
        _objectPositionX = builder.BuildFloatInput();
        _objectPositionX.OnValueChanged += OnPositionValueUpdated;

        builder.SetOneTimeSpacing(0f);
        builder.SetPositionOffsetX(170f);
        builder.SetText("Y:");
        _objectPositionY = builder.BuildFloatInput();
        _objectPositionY.OnValueChanged += OnPositionValueUpdated;
        
        builder.SetOneTimeSpacing(0f);
        builder.SetPositionOffsetX(320f);
        builder.SetText("Z:");
        _objectPositionZ = builder.BuildFloatInput();
        _objectPositionZ.OnValueChanged += OnPositionValueUpdated;
        
        builder
            .SetOneTimeSpacing(25f)
            .SetPositionOffsetX(5f)
            .SetText("Position");
        _objectPositionLabel = builder.BuildLabel(TextAnchor.MiddleLeft);
        
        builder.SetPositionOffsetX(20f);
        builder.SetText("X:");
        _objectRotationX = builder.BuildFloatInput();
        _objectRotationX.OnValueChanged += OnRotationValueUpdated;

        builder.SetOneTimeSpacing(0f);
        builder.SetPositionOffsetX(170f);
        builder.SetText("Y:");
        _objectRotationY = builder.BuildFloatInput();
        _objectRotationY.OnValueChanged += OnRotationValueUpdated;
        
        builder.SetOneTimeSpacing(0f);
        builder.SetPositionOffsetX(320f);
        builder.SetText("Z:");
        _objectRotationZ = builder.BuildFloatInput();
        _objectRotationZ.OnValueChanged += OnRotationValueUpdated;
        
        builder
            .SetOneTimeSpacing(25f)
            .SetPositionOffsetX(5f)
            .SetText("Rotation");
        _objectRotationLabel = builder.BuildLabel(TextAnchor.MiddleLeft);

        builder.SetText("X:");
        builder.SetPositionOffsetX(20f);
        _objectScaleX = builder.BuildFloatInput();
        _objectScaleX.OnValueChanged += OnScaleValueUpdated;
        
        builder.SetOneTimeSpacing(0f);
        builder.SetPositionOffsetX(170f);
        builder.SetText("Y:");
        _objectScaleY = builder.BuildFloatInput();
        _objectScaleY.OnValueChanged += OnScaleValueUpdated;
        
        builder.SetOneTimeSpacing(0f);
        builder.SetPositionOffsetX(320f);
        builder.SetText("Z:");
        _objectScaleZ = builder.BuildFloatInput();
        _objectScaleZ.OnValueChanged += OnScaleValueUpdated;
        
        builder.SetOneTimeSpacing(25f)
            .SetPositionOffsetX(5f)
            .SetText("Scale");
        _objectScaleLabel = builder.BuildLabel(TextAnchor.MiddleLeft);
    }

    public void Initialize(ref EditorLevelObjectsUI uiInstance)
    {
        uiInstance.AddChild(_highlightButton);
        uiInstance.AddChild(_highlightColorsButton);
        uiInstance.AddChild(_filterButton);
        uiInstance.AddChild(_filterField);
        uiInstance.AddChild(_adjacentPlaceButton);
        uiInstance.AddChild(_objectPositionLabel);
        uiInstance.AddChild(_objectPositionX);
        uiInstance.AddChild(_objectPositionY);
        uiInstance.AddChild(_objectPositionZ);
        uiInstance.AddChild(_objectRotationLabel);
        uiInstance.AddChild(_objectRotationX);
        uiInstance.AddChild(_objectRotationY);
        uiInstance.AddChild(_objectRotationZ);
        uiInstance.AddChild(_objectScaleLabel);
        uiInstance.AddChild(_objectScaleX);
        uiInstance.AddChild(_objectScaleY);
        uiInstance.AddChild(_objectScaleZ);
        _filterText = string.Empty;
    }
    
    private void OnAdjacentPlaceClicked(ISleekElement button)
    {
        if (!_selectedObject) return;
        LevelObject levelObject = FindLevelObjectByGameObject(_selectedObject.gameObject);
        if (levelObject == null) return;

        Vector3 chosenDirection = GetObjectFace(levelObject.transform);
        
        Bounds bounds = GetObjectBounds(levelObject.transform);
        
        if (EditorObjects.selectedObjectAsset == null && EditorObjects.selectedItemAsset == null)
        {
            EditorObjects.selectedObjectAsset = levelObject.asset;
            EditorObjects.selectedItemAsset = null;
        }
        
        
        Vector3 point;
        if (EditorObjects.selectedObjectAsset == levelObject.asset)
        {
            float boundSize = Mathf.Abs(Vector3.Dot(chosenDirection, levelObject.transform.right)) > 0.5f
                ? bounds.size.x
                : bounds.size.y;
            Vector3 offset = chosenDirection * boundSize;
            point = levelObject.transform.position + offset;            
        }
        else
        {
            // https://stackoverflow.com/questions/58089093/place-an-object-on-the-right-side-of-another-object-in-unity
            float boundSize = Mathf.Abs(Vector3.Dot(chosenDirection, levelObject.transform.right)) > 0.5f
                ? bounds.extents.x
                : bounds.extents.y;
            // ReSharper disable once PossibleNullReferenceException
            Transform targetTransform = EditorObjects.selectedObjectAsset.GetOrLoadModel().transform;
            Bounds targetBound = GetObjectBounds(targetTransform);
            float targetBoundSize = Mathf.Abs(Vector3.Dot(chosenDirection, targetTransform.right)) > 0.5f
                ? targetBound.extents.x
                : targetBound.extents.y;
            Vector3 offset = chosenDirection * (boundSize + targetBoundSize);
            point = levelObject.transform.position + offset;
        }
        
        EditorObjects.handles.SetPreferredPivot(point, _selectedObject.rotation);
        LevelObjects.step++;
        Transform transform = LevelObjects.registerAddObject(point, _selectedObject.rotation, Vector3.one, EditorObjects.selectedObjectAsset, EditorObjects.selectedItemAsset);
        if (!transform) return;
        
        EditorObjects.clearSelection();
        EditorObjects.addSelection(transform);
    }

    private void OnFilterFieldSubmitted(ISleekField field)
    {
        _filterText = field.Text;
    }

    private void OnFilterClicked(ISleekElement button)
    {
        _filterText = _filterField.Text;
        List<LevelObject> levelObjects = GetObjectsByMod(_filterText);
        PrivateHighlight(levelObjects);
        _filterButton.text = $"Filter objects ({levelObjects.Count})";
    }

    private void HighlightObjects(ISleekElement button)
    {
        if (_selectedObject == null) return;
        
        LevelObject levelObject = FindLevelObjectByGameObject(_selectedObject.gameObject);
        if (levelObject?.asset == null) return;

        if (_highlightedObjects.Count > 0)
        {
            UnhighlightAll(_selectedObject);
        }

        List<LevelObject> levelObjects = GetObjectsByGuid(levelObject.asset.GUID);
        PrivateHighlight(levelObjects, levelObject);

        _highlightButton.text = $"Highlight objects ({levelObjects.Count})";
    }

    private void PrivateHighlight(List<LevelObject> levelObjects, LevelObject ignore = null)
    {
        foreach (LevelObject lObj in levelObjects)
        {
            if (lObj == null || lObj.transform == null || lObj.transform.gameObject == null || lObj == ignore || lObj.transform == _selectedObject) continue;
            Highlighter highlighter = lObj.transform.GetComponent<Highlighter>();
            if (!highlighter)
            {
                highlighter = lObj.transform.gameObject.AddComponent<Highlighter>();
            }
            highlighter.overlay = true;
            highlighter.ConstantOn(_highlightColors[_currentColorIndex]);
            _highlightedObjects.Add(highlighter);
        }
    }

    private void OnPositionValueUpdated(ISleekFloat32Field field, float value)
    {
        LevelObject levelObject = FindLevelObjectByGameObject(_selectedObject.gameObject);
        if (levelObject == null) return; // Just in case

        Vector3 position = _selectedObject.position;

        // Probably there's a better way of check this
        if (field == _objectPositionX)
        {
            position.x = _objectPositionX.Value;
        }
        else if (field == _objectPositionY)
        {
            position.y = _objectPositionY.Value;
        }
        else if (field == _objectPositionZ)
        {
            position.z = _objectPositionZ.Value;
        }
        
        _selectedObject.position = position;
        EditorObjects.calculateHandleOffsets();
    }
    
    private void OnRotationValueUpdated(ISleekFloat32Field field, float value)
    {
        LevelObject levelObject = FindLevelObjectByGameObject(_selectedObject.gameObject);
        if (levelObject == null) return; // Just in case

        Vector3 rotation = _selectedObject.rotation.eulerAngles;

        // Probably there's a better way of check this
        if (field == _objectRotationX)
        {
            rotation.x = _objectRotationX.Value;
        }
        else if (field == _objectRotationY)
        {
            rotation.y = _objectRotationY.Value;
        }
        else if (field == _objectRotationZ)
        {
            rotation.z = _objectRotationZ.Value;
        }
        
        _selectedObject.rotation = Quaternion.Euler(rotation);
        EditorObjects.calculateHandleOffsets();
    }
    
    private void OnScaleValueUpdated(ISleekFloat32Field field, float value)
    {
        LevelObject levelObject = FindLevelObjectByGameObject(_selectedObject.gameObject);
        if (levelObject == null) return; // Just in case

        Vector3 scale = _selectedObject.localScale;

        // Probably there's a better way of check this
        if (field == _objectScaleX)
        {
            scale.x = _objectScaleX.Value;
        }
        else if (field == _objectScaleY)
        {
            scale.y = _objectScaleY.Value;
        }
        else if (field == _objectScaleZ)
        {
            scale.z = _objectScaleZ.Value;
        }
        
        _selectedObject.localScale = scale;
        EditorObjects.calculateHandleOffsets();
    }
    
    private void OnSwappedStateColor(SleekButtonState button, int index)
    {
        _currentColorIndex = index;
        UnhighlightAll(_selectedObject);
    }

    public void SelectObject(Transform selectedObject)
    {
        _selectedObject = selectedObject;

        _objectPositionX.Value = selectedObject.position.x;
        _objectPositionY.Value = selectedObject.position.y;
        _objectPositionZ.Value = selectedObject.position.z;
        
        _objectRotationX.Value = selectedObject.rotation.eulerAngles.x;
        _objectRotationY.Value = selectedObject.rotation.eulerAngles.y;
        _objectRotationZ.Value = selectedObject.rotation.eulerAngles.z;
        
        _objectScaleX.Value = selectedObject.localScale.x;
        _objectScaleY.Value = selectedObject.localScale.y;
        _objectScaleZ.Value = selectedObject.localScale.z;
    }
    
    public void UpdateSelectedObject()
    {
        if (_selectedObject == null) return;

        SelectObject(_selectedObject);
    }
    
    public void UnhighlightAll(Transform ignore = null)
    {
        if (_highlightedObjects.Count < 1) return;

        foreach (Highlighter highlightedObject in _highlightedObjects)
        {
            if (highlightedObject.transform == ignore) continue;
            
            Object.DestroyImmediate(highlightedObject);
        }
        _highlightedObjects.Clear();
        _highlightButton.text = "Highlight objects";
    }

    // Basically a CustomUpdate which doesn't replace the original update function.
    public void LateUpdate()
    {
        ChangeButtonsVisibility(EditorObjects.selection != null && EditorObjects.selection.Count == 1);
        // TODO: Implement a grid system with options to automatically align objects with the grid corners.
    }
    
    private void ChangeButtonsVisibility(bool visible)
    {
        if (!visible)
        {
            UnhighlightAll();
        }

        _highlightButton.IsVisible = visible;
        _highlightColorsButton.IsVisible = visible;
        
        _filterButton.IsVisible = visible;
        if (visible && EditorLevelObjectsUI.active)
        {
            _filterButton.text = "Filter objects";
        }
        _filterField.IsVisible = visible;

        _adjacentPlaceButton.IsVisible = visible;
        
        _objectPositionLabel.IsVisible = visible;
        _objectPositionX.IsVisible = visible;
        _objectPositionY.IsVisible = visible;
        _objectPositionZ.IsVisible = visible;
        
        _objectRotationLabel.IsVisible = visible;
        _objectRotationX.IsVisible = visible;
        _objectRotationY.IsVisible = visible;
        _objectRotationZ.IsVisible = visible;
        
        _objectScaleLabel.IsVisible = visible;
        _objectScaleX.IsVisible = visible;
        _objectScaleY.IsVisible = visible;
        _objectScaleZ.IsVisible = visible;
    }

    private LevelObject FindLevelObjectByGameObject(GameObject rootGameObject)
    {
        if (rootGameObject == null)
        {
            return null;
        }
        
        Transform transform = rootGameObject.transform;
        if (Regions.tryGetCoordinate(transform.position, out byte x, out byte y))
        {
            for (int i = 0; i < LevelObjects.objects[x, y].Count; i++)
            {
                if (LevelObjects.objects[x, y][i].transform == transform)
                {
                    return LevelObjects.objects[x, y][i];
                }
            }
        }
        return null;
    }

    private List<LevelObject> GetObjectsByMod(string filter)
    {
        IEnumerable<LevelObject> levelObjects = LevelObjects.objects.Cast<List<LevelObject>>().SelectMany(list => list);
        
        return levelObjects.Where(c => c != null && c.asset != null && c.asset.GetOriginName().Contains(filter)).ToList();
    }

    private List<LevelObject> GetObjectsByGuid(Guid guid)
    {
        IEnumerable<LevelObject> levelObjects = LevelObjects.objects.Cast<List<LevelObject>>().SelectMany(list => list);

        return levelObjects.Where(c => c != null && c.asset != null && c.asset.GUID == guid).ToList();
    }
    
    private Vector3 GetObjectFace(Transform objTransform)
    {
        Vector3 cameraDirection = MainCamera.instance.transform.forward;
        
        Vector3[] faces = {
            objTransform.up, // Up? Yeah, using forward makes the object spawn up or down due the object by default is rotated 90 degrees
            -objTransform.up,
            objTransform.right,
            -objTransform.right
        };

        // Vector3.Distance doesn't make almost any impact on performance unless you use it in the update function or similar
        return faces.OrderBy(c => Vector3.Distance(c, cameraDirection)).First();
    }
    
    private Bounds GetObjectBounds(Transform objectTransform)
    {
        if (objectTransform.TryGetComponent(out MeshFilter meshFilter))
        {
            return meshFilter.mesh.bounds;
        }
        List<MeshFilter> filters = objectTransform.GetComponentsInChildren<MeshFilter>(true)
            .Where(c => !c.transform.name.Equals("nav", StringComparison.OrdinalIgnoreCase)).ToList();
        
        return filters.Count > 0 ? filters[0].mesh.bounds : new Bounds(objectTransform.position, Vector3.one);
    }
}