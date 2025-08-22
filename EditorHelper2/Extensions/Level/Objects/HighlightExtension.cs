using System.Collections.Generic;
using System.Linq;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Helpers.Level.Objects;
using EditorHelper2.Patches.Editor;
using EditorHelper2.UI.Builders;
using HighlightingSystem;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Level.Objects;

[UIExtension(typeof(EditorLevelObjectsUI))]
[EHExtension("Highlight Extension", "Senior S")]
public class HighlightExtension : UIExtension, IExtension
{
    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;
    
    /// <summary>
    /// Index of the focused object
    /// </summary>
    private int _focusHighlightIndex;
    private readonly List<Transform> _highlightedTransforms;
    private readonly List<Highlighter> _highlightedObjects;
    
    private readonly SleekButtonIcon _highlightButton;
    private readonly SleekButtonState _highlightColorsButton;
    private readonly SleekButtonIcon _highlightWrongScaleButton;
    private readonly SleekButtonIcon _selectHighlightedButton;
    
    private readonly Color[] _highlightColors = [Color.yellow, Color.red, Color.magenta, Color.blue];
    private int _currentColorIndex;
    
    private readonly SleekButtonIcon _filterByModButton;
    private readonly ISleekField _filterByModField;
    private string _filterByModText = string.Empty;
    
    public HighlightExtension()
    {
        _focusHighlightIndex = 0;
        _highlightedTransforms = [];
        _highlightedObjects = [];
        _currentColorIndex = 0;
        
        UIBuilder builder = new(150f, 30f);
        
        builder.SetOffsetHorizontal(205f)
            .SetOffsetVertical(-30f)
            .SetAnchorVertical(1f)
            .SetText("Highlight objects");

        _highlightButton = builder.BuildButton("Highlight all objects of the selected type");
        
        builder.SetText("Highlight wrong objects");
        
        _highlightWrongScaleButton = builder.BuildButton("Highlight all objects with a negative scale");
        
        builder.SetText("Change the highlight color")
            .SetOffsetHorizontal(360f)
            .SetOffsetVertical(-30f);

        _highlightColorsButton = builder.BuildButtonState(new GUIContent("Yellow"), new GUIContent("Red"), 
            new GUIContent("Purple"), new GUIContent("Blue"));
        
        builder.SetOffsetVertical(-110f)
            .SetText("Select highlighted objects");
        
        _selectHighlightedButton = builder.BuildButton("Select all highlighted objects");
        
        builder.SetText("Filter objects")
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(205f)
            .SetOffsetVertical(-110f);
        _filterByModButton = builder.BuildButton("Highlight all objects that derive from this mod.");

        builder.SetText("Mod ID");
        _filterByModField = builder.BuildStringField();

        Initialize();
    }

    public void Initialize()
    {
        if (_container == null) return;
        
        _container.AddChild(_highlightButton);
        _container.AddChild(_highlightColorsButton);
        _container.AddChild(_highlightWrongScaleButton);
        _container.AddChild(_selectHighlightedButton);
        _container.AddChild(_filterByModButton);
        _container.AddChild(_filterByModField);
        
        _highlightButton.onClickedButton += OnHighlightButtonClicked;
        _highlightColorsButton.onSwappedState = OnSwappedStateColor;
        _highlightWrongScaleButton.onClickedButton += OnHighlightWrongScaleButtonClicked;
        _selectHighlightedButton.onClickedButton += OnSelectHighlightedClicked;
        _filterByModButton.onClickedButton += OnFilterByModClicked;
        _filterByModField.OnTextSubmitted += OnFilterByModFieldSubmitted;
        
        EditorObjectsPatches.OnClearSelection += OnClearSelection;
    }

    #region Event Handlers
    private void OnClearSelection(EditorObjects obj)
    {
        UnhighlightAll();
    }
    
    private void OnHighlightButtonClicked(ISleekElement button)
    {
        if (EditorObjects.selection == null || EditorObjects.selection.Count != 1) return;
        Transform selectedObject = EditorObjects.selection.First().transform;
        
        LevelObject? levelObject = LevelObjects.FindLevelObject(selectedObject.gameObject);
        if (levelObject?.asset == null) return;
        if (_highlightedObjects.Count > 0)
        {
            UnhighlightAll(selectedObject);
        }

        List<LevelObject> levelObjects = ObjectsHelper.GetObjectsByGuid(levelObject.asset.GUID);
        PrivateHighlight(levelObjects, levelObject);

        _highlightButton.text = $"Highlight objects ({levelObjects.Count})";
    }
    
    private void OnSwappedStateColor(SleekButtonState button, int index)
    {
        _currentColorIndex = index;
        
        if (EditorObjects.selection == null) return;
        UnhighlightAll(EditorObjects.selection.FirstOrDefault()?.transform);
    }
    
    private void OnHighlightWrongScaleButtonClicked(ISleekElement button)
    {
        List<LevelObject> levelObjects = ObjectsHelper.GetWrongScaledObjects();
        if (levelObjects.Count < 1) return;
        
        PrivateHighlight(levelObjects);
        _highlightWrongScaleButton.text = $"Highlight wrong objects ({levelObjects.Count})";
    }

    private void OnSelectHighlightedClicked(ISleekElement button)
    {
        if (_highlightedObjects.Count < 1) return;
        
        List<Transform> toSelect = _highlightedObjects.Select(c => c.transform).ToList();
        UnhighlightAll();
        
        toSelect.ForEach(EditorObjects.addSelection);
    }
    
    private void OnFilterByModClicked(ISleekElement button)
    {
        _filterByModText = _filterByModField.Text;
        List<LevelObject> levelObjects = ObjectsHelper.GetObjectsByMod(_filterByModText);
        PrivateHighlight(levelObjects);
        _filterByModButton.text = $"Filter objects ({levelObjects.Count})";
    }
    
    private void OnFilterByModFieldSubmitted(ISleekField field)
    {
        _filterByModText = field.Text;
    }
    #endregion
    
    #region Extension functions

    public void CustomUpdate()
    {
        _selectHighlightedButton.IsVisible = _highlightedTransforms.Count > 0; 
        if (_highlightedTransforms.Count < 1 && EditorObjects.selection.Count != 1) return;
        
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            _focusHighlightIndex -= 1;
                
            if (_focusHighlightIndex < 0)
            {
                _focusHighlightIndex = _highlightedTransforms.Count - 1;
            }
                
            Transform selection = _highlightedTransforms[_focusHighlightIndex].transform;
            // It doesn't use EditorObjects::clearSelection due it removes the highlight from the selection
            EditorObjects.selection.Clear();
            EditorObjects.selection.Add(new EditorSelection(selection, selection.position, selection.rotation, selection.localScale));
            EditorObjects.calculateHandleOffsets();
            MainCamera.instance.transform.parent.position = EditorObjects.handles.GetPivotPosition() - 15f * MainCamera.instance.transform.forward;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            _focusHighlightIndex += 1;
            if (_focusHighlightIndex >= _highlightedTransforms.Count)
            {
                _focusHighlightIndex = 0;
            }
                
            Transform selection = _highlightedTransforms[_focusHighlightIndex].transform;
            // It doesn't use EditorObjects::clearSelection due it removes the highlight from the selection
            EditorObjects.selection.Clear();
            EditorObjects.selection.Add(new EditorSelection(selection, selection.position, selection.rotation, selection.localScale));
            EditorObjects.calculateHandleOffsets();
            MainCamera.instance.transform.parent.position = EditorObjects.handles.GetPivotPosition() - 15f * MainCamera.instance.transform.forward;
        }
    }
    
    private void UnhighlightAll(Transform? ignore = null)
    {
        _highlightedTransforms.Clear();
        if (_highlightedObjects.Count < 1) return;

        foreach (Highlighter highlightedObject in _highlightedObjects)
        {
            if (!highlightedObject || !highlightedObject.transform || highlightedObject.transform == ignore) continue;
            
            _highlightedTransforms.Remove(highlightedObject.transform);
            Object.DestroyImmediate(highlightedObject);
        }
        _highlightedObjects.Clear();
        _highlightButton.text = "Highlight objects";
    }
    
    private void PrivateHighlight(List<LevelObject> levelObjects, LevelObject? ignore = null)
    {
        _highlightedTransforms.Clear();
        _focusHighlightIndex = 0;
        if (ignore != null)
        {
            _highlightedTransforms.Add(ignore.transform);
        }
        foreach (LevelObject lObj in levelObjects)
        {
            if (lObj == null || lObj.transform == null || lObj.transform.gameObject == null || lObj == ignore 
                || EditorObjects.selection.Any(c => c.transform == lObj.transform)) continue;
            _highlightedTransforms.Add(lObj.transform);
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
    #endregion
    
    public void Dispose()
    {
        if (_container == null) return;
        
        _container.RemoveChild(_highlightButton);
        _container.RemoveChild(_highlightColorsButton);
        _container.RemoveChild(_highlightWrongScaleButton);
        _container.RemoveChild(_selectHighlightedButton);
        
        _highlightButton.onClickedButton -= OnHighlightButtonClicked;
        _highlightColorsButton.onSwappedState = null;
        _highlightWrongScaleButton.onClickedButton -= OnHighlightWrongScaleButtonClicked;
        _selectHighlightedButton.onClickedButton -= OnSelectHighlightedClicked;
        
        EditorObjectsPatches.OnClearSelection -= OnClearSelection;
    }
}