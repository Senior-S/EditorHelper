using System;
using System.Collections.Generic;
using System.Linq;
using HighlightingSystem;
using SDG.Unturned;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorHelper.Editor;

public class ObjectsManager
{
    public readonly List<Highlighter> HighlightedObjects = [];
    private readonly SleekButtonIcon _highlightButton;
    private readonly SleekButtonState _highlightColorsButton;
    
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

    // TODO: Make a library or some help functions to create UI elements
    public ObjectsManager()
    {
        _highlightButton = new SleekButtonIcon(null)
        {
            PositionOffset_X = 210f,
            PositionOffset_Y = -30f,
            PositionScale_Y = 1f,
            SizeOffset_X = 200f,
            SizeOffset_Y = 30f,
            text = "Highlight objects",
            tooltip = "Highlight all objects of the selected type"
        };
        _highlightButton.onClickedButton += HighlightObjects;
        _highlightColorsButton = new SleekButtonState(new GUIContent("Yellow"), new GUIContent("Red"), new GUIContent("Purple"), new GUIContent("Blue"))
        {
            PositionOffset_X = 210f,
            PositionOffset_Y = -70f,
            PositionScale_Y = 1f,
            SizeOffset_X = 200f,
            SizeOffset_Y = 30f,
            tooltip = "Change the highlight color",
            onSwappedState = OnSwappedStateColor
        };

        _objectPositionLabel = Glazier.Get().CreateLabel();
        _objectPositionLabel.PositionOffset_Y = -415f;
        _objectPositionLabel.PositionScale_Y = 1f;
        _objectPositionLabel.SizeOffset_X = 200f;
        _objectPositionLabel.SizeOffset_Y = 30f;
        _objectPositionLabel.Text = "Position";
        _objectPositionLabel.TextAlignment = TextAnchor.MiddleLeft;
        
        _objectPositionX = Glazier.Get().CreateFloat32Field();
        _objectPositionX.PositionOffset_Y = -390f;
        _objectPositionX.PositionOffset_X = 30f;
        _objectPositionX.PositionScale_Y = 1f;
        _objectPositionX.SizeOffset_X = 120f;
        _objectPositionX.SizeOffset_Y = 30f;
        _objectPositionX.Value = 0f;
        _objectPositionX.AddLabel("X:", ESleekSide.LEFT);
        _objectPositionX.OnValueChanged += OnPositionValueUpdated;
        
        _objectPositionY = Glazier.Get().CreateFloat32Field();
        _objectPositionY.PositionOffset_Y = -390f;
        _objectPositionY.PositionOffset_X = 180f;
        _objectPositionY.PositionScale_Y = 1f;
        _objectPositionY.SizeOffset_X = 120f;
        _objectPositionY.SizeOffset_Y = 30f;
        _objectPositionY.Value = 0f;
        _objectPositionY.AddLabel("Y:", ESleekSide.LEFT);
        _objectPositionY.OnValueChanged += OnPositionValueUpdated;
        
        _objectPositionZ = Glazier.Get().CreateFloat32Field();
        _objectPositionZ.PositionOffset_Y = -390f;
        _objectPositionZ.PositionOffset_X = 330f;
        _objectPositionZ.PositionScale_Y = 1f;
        _objectPositionZ.SizeOffset_X = 120f;
        _objectPositionZ.SizeOffset_Y = 30f;
        _objectPositionZ.Value = 0f;
        _objectPositionZ.AddLabel("Z:", ESleekSide.LEFT);
        _objectPositionZ.OnValueChanged += OnPositionValueUpdated;
        
        _objectRotationLabel = Glazier.Get().CreateLabel();
        _objectRotationLabel.PositionOffset_Y = -470f;
        _objectRotationLabel.PositionScale_Y = 1f;
        _objectRotationLabel.SizeOffset_X = 200f;
        _objectRotationLabel.SizeOffset_Y = 30f;
        _objectRotationLabel.Text = "Rotation";
        _objectRotationLabel.TextAlignment = TextAnchor.MiddleLeft;
        
        _objectRotationX = Glazier.Get().CreateFloat32Field();
        _objectRotationX.PositionOffset_Y = -445f;
        _objectRotationX.PositionOffset_X = 30f;
        _objectRotationX.PositionScale_Y = 1f;
        _objectRotationX.SizeOffset_X = 120f;
        _objectRotationX.SizeOffset_Y = 30f;
        _objectRotationX.Value = 0f;
        _objectRotationX.AddLabel("X:", ESleekSide.LEFT);
        _objectRotationX.OnValueChanged += OnRotationValueUpdated;
            
        _objectRotationY = Glazier.Get().CreateFloat32Field();
        _objectRotationY.PositionOffset_Y = -445f;
        _objectRotationY.PositionOffset_X = 180f;
        _objectRotationY.PositionScale_Y = 1f;
        _objectRotationY.SizeOffset_X = 120f;
        _objectRotationY.SizeOffset_Y = 30f;
        _objectRotationY.Value = 0f;
        _objectRotationY.AddLabel("Y:", ESleekSide.LEFT);
        _objectRotationY.OnValueChanged += OnRotationValueUpdated;
        
        _objectRotationZ = Glazier.Get().CreateFloat32Field();
        _objectRotationZ.PositionOffset_Y = -445f;
        _objectRotationZ.PositionOffset_X = 330f;
        _objectRotationZ.PositionScale_Y = 1f;
        _objectRotationZ.SizeOffset_X = 120f;
        _objectRotationZ.SizeOffset_Y = 30f;
        _objectRotationZ.Value = 0f;
        _objectRotationZ.AddLabel("Z:", ESleekSide.LEFT);
        _objectRotationZ.OnValueChanged += OnRotationValueUpdated;
        
        _objectScaleLabel = Glazier.Get().CreateLabel();
        _objectScaleLabel.PositionOffset_Y = -525f;
        _objectScaleLabel.PositionScale_Y = 1f;
        _objectScaleLabel.SizeOffset_X = 200f;
        _objectScaleLabel.SizeOffset_Y = 30f;
        _objectScaleLabel.Text = "Scale";
        _objectScaleLabel.TextAlignment = TextAnchor.MiddleLeft;
        
        _objectScaleX = Glazier.Get().CreateFloat32Field();
        _objectScaleX.PositionOffset_Y = -500f;
        _objectScaleX.PositionOffset_X = 30f;
        _objectScaleX.PositionScale_Y = 1f;
        _objectScaleX.SizeOffset_X = 120f;
        _objectScaleX.SizeOffset_Y = 30f;
        _objectScaleX.Value = 0f;
        _objectScaleX.AddLabel("X:", ESleekSide.LEFT);
        _objectScaleX.OnValueChanged += OnScaleValueUpdated;
        
        _objectScaleY = Glazier.Get().CreateFloat32Field();
        _objectScaleY.PositionOffset_Y = -500f;
        _objectScaleY.PositionOffset_X = 180f;
        _objectScaleY.PositionScale_Y = 1f;
        _objectScaleY.SizeOffset_X = 120f;
        _objectScaleY.SizeOffset_Y = 30f;
        _objectScaleY.Value = 0f;
        _objectScaleY.AddLabel("Y:", ESleekSide.LEFT);
        _objectScaleY.OnValueChanged += OnScaleValueUpdated;
        
        _objectScaleZ = Glazier.Get().CreateFloat32Field();
        _objectScaleZ.PositionOffset_Y = -500f;
        _objectScaleZ.PositionOffset_X = 330f;
        _objectScaleZ.PositionScale_Y = 1f;
        _objectScaleZ.SizeOffset_X = 120f;
        _objectScaleZ.SizeOffset_Y = 30f;
        _objectScaleZ.Value = 0f;
        _objectScaleZ.AddLabel("Z:", ESleekSide.LEFT);
        _objectScaleZ.OnValueChanged += OnScaleValueUpdated;
    }

    public void Initialize(ref EditorLevelObjectsUI uiInstance)
    {
        uiInstance.AddChild(_highlightButton);
        uiInstance.AddChild(_highlightColorsButton);
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
    }

    private void HighlightObjects(ISleekElement button)
    {
        if (_selectedObject == null) return;
        
        LevelObject levelObject = FindLevelObject(_selectedObject.gameObject);
        if (levelObject?.asset == null) return;

        List<LevelObject> levelObjects = GetObjectsByGuid(levelObject.asset.GUID);
        foreach (LevelObject lObj in levelObjects)
        {
            if (lObj == null || lObj.transform == null || lObj.transform.gameObject == null || lObj == levelObject) continue;
            Highlighter highlighter = lObj.transform.GetComponent<Highlighter>();
            if (!highlighter)
            {
                highlighter = lObj.transform.gameObject.AddComponent<Highlighter>();
            }
            highlighter.overlay = true;
            highlighter.ConstantOn(_highlightColors[_currentColorIndex]);
            HighlightedObjects.Add(highlighter);
        }

        _highlightButton.text = $"Highlight objects ({levelObjects.Count})";
    }

    private void OnPositionValueUpdated(ISleekFloat32Field field, float value)
    {
        LevelObject levelObject = FindLevelObject(_selectedObject.gameObject);
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
        LevelObject levelObject = FindLevelObject(_selectedObject.gameObject);
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
        LevelObject levelObject = FindLevelObject(_selectedObject.gameObject);
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
    
    public void UnhighlightAll(Transform ignore = null)
    {
        if (HighlightedObjects.Count < 1) return;

        foreach (Highlighter highlightedObject in HighlightedObjects)
        {
            if (highlightedObject.transform == ignore) continue;
            
            Object.DestroyImmediate(highlightedObject);
        }
        HighlightedObjects.Clear();
        _highlightButton.text = "Highlight objects";
    }
    
    public void ChangeButtonsVisibility(bool visible)
    {
        _highlightButton.IsVisible = visible;
        _highlightColorsButton.IsVisible = visible;

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

    private LevelObject FindLevelObject(GameObject rootGameObject)
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

    private List<LevelObject> GetObjectsByGuid(Guid guid)
    {
        IEnumerable<LevelObject> levelObjects = LevelObjects.objects.Cast<List<LevelObject>>().SelectMany(list => list);

        return levelObjects.Where(c => c != null && c.asset != null && c.asset.GUID == guid).ToList();
    }
}