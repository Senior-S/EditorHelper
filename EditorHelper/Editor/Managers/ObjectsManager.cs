using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using EditorHelper.Builders;
using EditorHelper.Models;
using HighlightingSystem;
using Newtonsoft.Json;
using SDG.Unturned;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorHelper.Editor.Managers;

// TODO: Add "tabs" or so for buttons to avoid flooding the user screen
public class ObjectsManager
{
    /// <summary>
    /// Index of the focused object
    /// </summary>
    private int _focusHighlightIndex = 0;
    private readonly List<Transform> _highlightedTransforms = [];
    private readonly List<Highlighter> _highlightedObjects = [];
    private readonly SleekButtonIcon _highlightButton;
    private readonly SleekButtonState _highlightColorsButton;
    
    private readonly SleekButtonIcon _filterButton;
    private readonly ISleekField _filterField;
    private string _filterText = string.Empty;

    private readonly SleekButtonIcon _adjacentPlaceButton;
    private readonly SleekButtonIcon _highlightWrongScaleButton;
    private readonly SleekButtonIcon _selectHighlightedButton;
    
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

    public int ObjectsLayerMask { get; private set; } = LayerMask.GetMask("Large", "Medium", "Small", "Barricade", "Structure");
    private readonly ISleekBox _layersContainer;
    //private readonly ISleekToggle[] _layersToggle;
    private readonly Dictionary<ISleekToggle, string> _toggleToLayer = new();
    private readonly SleekButtonIcon _layersMaskButton;

    private readonly SleekButtonIcon _schematicsButton;
    private readonly ISleekBox _schematicsContainer;
    private readonly SleekList<SchematicModel> _schematicsScrollBox;
    private readonly ISleekField _schematicName;
    private string _schematicNameValue = string.Empty;
    private readonly SleekButtonIcon _saveSchematicButton;
    private readonly SleekButtonIcon _schematicsHowToButton;
    private readonly SleekButtonIcon _schematicsReload;
    private readonly ISleekField _schematicSearch;
    private string _schematicSearchValue = string.Empty;

    private readonly ISleekField _tagField;
    
    // https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.conditionalweaktable-2?view=net-9.0
    private readonly ConditionalWeakTable<ReunObjectRemove, ReunObjectRemoveExtension> _cwtObjectRemove;
    private readonly ConditionalWeakTable<EditorCopy, EditorCopyExtension> _cwtEditorCopy;
    
    private readonly Dictionary<LevelObject, LevelObjectExtension> _dicLevelObjects;
    
    private readonly Color[] _highlightColors = [Color.yellow, Color.red, Color.magenta, Color.blue];
    private int _currentColorIndex = 0;
    private Transform? _selectedObject = null;
    
    // TODO: Add a way to automatically initialize the button
    public ObjectsManager()
    {
        UIBuilder builder = new();
        
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

        builder.SetText("Highlight wrong objects");
        _highlightWrongScaleButton = builder.BuildButton("Highlight all objects with a negative scale");
        _highlightWrongScaleButton.onClickedButton += OnHighlightWrongScaleClicked;

        builder.SetText("Select highlighted objects");
        _selectHighlightedButton = builder.BuildButton("Select all highlighted objects");
        _selectHighlightedButton.onClickedButton += OnSelectHighlightedClicked;
        
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
        
        builder.SetPositionScaleX(0.5f)
            .SetPositionScaleY(1)
            .SetPositionOffsetX(-125f)
            .SetPositionOffsetY(-210f)
            .SetSizeOffsetX(250f)
            .SetSizeOffsetY(160f)
            .SetText("");

        _layersContainer = builder.BuildBox();
        builder.SetPositionScaleX(0.5f)
            .SetPositionScaleY(0.5f)
            .SetSizeOffsetX(40f)
            .SetSizeOffsetY(40f)
            .SetPositionOffsetX(-120f)
            .SetPositionOffsetY(-60f);
        
        List<ISleekToggle> toggles = 
        [
            builder.SetText("Large").BuildToggle(),
            builder.SetText("Medium").BuildToggle()
        ];
        builder.SetPositionOffsetX(5f)
            .SetPositionOffsetY(-60f);
        toggles.AddRange([
            builder.SetText("Small").BuildToggle(),
            builder.SetText("Structure").BuildToggle()
        ]);
        builder.SetPositionOffsetX(-60f)
            .SetPositionOffsetY(30f);
        toggles.Add(builder.SetText("Barricade").BuildToggle());
        
        _toggleToLayer[toggles[0]] = "Large";
        _toggleToLayer[toggles[1]] = "Medium";
        _toggleToLayer[toggles[2]] = "Small";
        _toggleToLayer[toggles[3]] = "Structure";
        _toggleToLayer[toggles[4]] = "Barricade";
        
        //_layersToggle = toggles.ToArray();
        for (int i = 0; i < toggles.Count; i++)
        {
            ISleekToggle toggle = toggles[i];
            _layersContainer.AddChild(toggle);
            toggle.OnValueChanged += OnLayerToggleChanged;
        }

        _layersContainer.IsVisible = false;
        
        builder.SetPositionScaleX(1f)
            .SetPositionScaleY(1f)
            .SetPositionOffsetX(-230f)
            .SetPositionOffsetY(-30f)
            .SetSizeOffsetX(200)
            .SetSizeOffsetY(30)
            .SetText("Change layer mask");
        
        _layersMaskButton = builder.BuildButton("Change the layer mask that determines what can be selected");
        _layersMaskButton.onClickedButton += OnLayersMaskButtonClicked;

        builder.SetText("Schematics");
        _schematicsButton = builder.BuildButton("Open the schematics screen");
        _schematicsButton.onClickedButton += OnSchematicsButtonClicked;

        builder.SetText("Object tag");
        
        _tagField = builder.BuildStringField();
        _tagField.OnTextSubmitted += TagFieldOnOnTextSubmitted;
        
        builder.SetPositionScaleX(0.5f)
            .SetPositionScaleY(0.5f)
            .SetPositionOffsetX(-325f)
            .SetPositionOffsetY(200f)
            .SetSizeOffsetX(650f)
            .SetSizeOffsetY(400f)
            .SetOneTimeSpacing(0f)
            .SetText("");

        _schematicsContainer = builder.BuildBox();
        _schematicsContainer.IsVisible = false;

        builder.SetOneTimeSpacing(0)
            .SetPositionScaleX(0)
            .SetPositionScaleY(0)
            .SetPositionOffsetX(-210f)
            .SetPositionOffsetY(0)
            .SetSizeOffsetX(200f)
            .SetSizeOffsetY(30f)
            .SetText("Schematic name");
        
        _schematicName = builder.BuildStringField();
        _schematicName.OnTextSubmitted += SchematicNameOnTextSubmitted;
        _schematicsContainer.AddChild(_schematicName);
        
        builder.SetOneTimeSpacing(0)
            .SetPositionOffsetX(0)
            .SetPositionOffsetX(-210f)
            .SetPositionOffsetY(40f)
            .SetSizeOffsetX(200f)
            .SetSizeOffsetY(30f)
            .SetText("Save schematic");
        
        _saveSchematicButton = builder.BuildButton("Save schematic");
        _saveSchematicButton.onClickedButton += OnSaveSchematicButtonClickedButton;
        _schematicsContainer.AddChild(_saveSchematicButton);
        
        builder.SetOneTimeSpacing(0)
            .SetPositionOffsetX(-210f)
            .SetPositionOffsetY(80f)
            .SetSizeOffsetX(200f)
            .SetSizeOffsetY(30f)
            .SetText("How to use schematics");
        
        _schematicsHowToButton = builder.BuildButton("How to use schematics");
        _schematicsHowToButton.onClickedButton += OnSchematicsHowToButtonClickedButton;
        _schematicsContainer.AddChild(_schematicsHowToButton);
        
        builder.SetOneTimeSpacing(0)
            .SetPositionScaleX(0.5f)
            .SetPositionScaleY(0)
            .SetPositionOffsetX(-100f)
            .SetPositionOffsetY(-40f)
            .SetSizeOffsetX(200f)
            .SetSizeOffsetY(30f)
            .SetText("Search Term");

        _schematicSearch = builder.BuildStringField();
        _schematicSearch.OnTextSubmitted += OnSchematicSearchTextChanged;
        _schematicsContainer.AddChild(_schematicSearch);

        builder.SetOneTimeSpacing(0)
            .SetPositionScaleX(0.5f)
            .SetPositionScaleY(1f)
            .SetPositionOffsetX(-100f)
            .SetPositionOffsetY(5f)
            .SetSizeOffsetX(200f)
            .SetSizeOffsetY(30f)
            .SetText("Reload schematics");
        
        _schematicsReload = builder.BuildButton("Reload all schematics in the schematics folder");
        _schematicsReload.onClickedButton += OnSchematicsReloadClickedButton;
        _schematicsContainer.AddChild(_schematicsReload);

        // This shitty scroll box took like half an hour due Nelson's way of doing it sucks :>
        // So for anyone reading this and for the future me, I added comments to know how to read it and use it.
        builder = new UIBuilder().SetPositionScaleX(0f)
            .SetPositionScaleY(1f) // IMPORTANT! We want to start the scroll box at the bottom
            .SetPositionOffsetX(10f)
            .SetPositionOffsetY(-390f) // Then we define the area of the scroll box. It's negative due we're already at the bottom so we need to go up.
            .SetSizeOffsetX(630f) // Size/Area of the scrollbox
            .SetSizeOffsetY(-10f) // And once the scrollbox area is done we added the bottom padding
            .SetOneTimeSpacing(0f);

        _schematicsScrollBox = builder.BuildScrollBox<SchematicModel>(50, 10);
        _schematicsScrollBox.onCreateElement = OnCreateSchematicModel;

        UpdateSchematicsScrollbox();
        _schematicsContainer.AddChild(_schematicsScrollBox);
        
        _cwtObjectRemove = new ConditionalWeakTable<ReunObjectRemove, ReunObjectRemoveExtension>();
        _cwtEditorCopy = new ConditionalWeakTable<EditorCopy, EditorCopyExtension>();
        _dicLevelObjects = new Dictionary<LevelObject, LevelObjectExtension>();
        
        LoadObjectTags();
    }

    private void TagFieldOnOnTextSubmitted(ISleekField field)
    {
        if (_selectedObject == null) return;
        LevelObject? levelObject = FindLevelObjectByGameObject(_selectedObject);
        if (levelObject == null) return;
        
        if (_dicLevelObjects.TryGetValue(levelObject, out LevelObjectExtension extension))
        {
            extension?.UpdateTag(field.Text);
        }
        else
        {
            _dicLevelObjects.Add(levelObject, new LevelObjectExtension(field.Text));
        }
    }

    private void OnSchematicSearchTextChanged(ISleekField field)
    {
        _schematicSearchValue = field.Text.ToLower();
        UpdateSchematicsScrollbox();
    }

    private void OnSchematicsReloadClickedButton(ISleekElement button)
    {
        EditorHelper.Instance.SchematicsManager.ReloadSchematics();
        UpdateSchematicsScrollbox();
    }

    private void OnSchematicsHowToButtonClickedButton(ISleekElement button)
    {
        Provider.openURL("https://www.youtube.com/watch?v=9myiTLl7Eq4");
    }

    private void OnSaveSchematicButtonClickedButton(ISleekElement button)
    {
        if (EditorObjects.copies.Count < 1)
        {
            EditorHelper.Instance.EditorManager.DisplayAlert("You need to have copied at least 1 object to create a schematic.");
            return;
        }

        if (_schematicNameValue.Length < 2)
        {
            EditorHelper.Instance.EditorManager.DisplayAlert("Please provide a schematic name with at least 2 letters!");
            return;
        }
        
        EditorHelper.Instance.SchematicsManager.SaveSchematic(_schematicNameValue);
        UpdateSchematicsScrollbox();
    }

    private void SchematicNameOnTextSubmitted(ISleekField field)
    {
        _schematicNameValue = field.Text;
    }

    private ISleekElement OnCreateSchematicModel(SchematicModel item)
    {
        UIBuilder builder = new();
        // The schematic model may not be fully loaded yet, if that's the case the SchematicModel won't have any objects.
        string objectsCount = item.Objects.Count == 0 ? "Unknown objects" : $"{item.Objects.Count} objects";
        
        builder.SetText($"{item.Name} by {item.Author} ({objectsCount})");
        ISleekButton button = builder.CreateSimpleButton();
        button.OnClicked += OnSchematicClicked;
        
        return button;
    }

    private void OnSchematicClicked(ISleekElement button)
    {
        int index = Mathf.FloorToInt(button.PositionOffset_Y / 60f);
        
        SchematicModel? model = EditorHelper.Instance.SchematicsManager.TryLoadSchematic(index, _schematicSearchValue);
        if (model == null)
        {
            // Errors are provided by the try load method so it isn't required here
            return; 
        }

        List<EditorCopy> copies = model.Objects.Select(c => c.ToEditorCopy()).ToList();
        EditorObjects.copies.Clear();
        EditorObjects.copies.AddRange(copies);
        UpdateSchematicsScrollbox();
    }

    private void OnSchematicsButtonClicked(ISleekElement button)
    {
        _schematicsContainer.IsVisible = !_schematicsContainer.IsVisible;
    }

    private void OnLayerToggleChanged(ISleekToggle toggle, bool state)
    {
        ObjectsLayerMask = _toggleToLayer.Where(kvp => kvp.Key.Value).Aggregate(0, (current, kvp) => current | 1 << LayerMask.NameToLayer(kvp.Value));;
    }

    private void OnLayersMaskButtonClicked(ISleekElement button)
    {
        _layersContainer.IsVisible = !_layersContainer.IsVisible;
    }

    // This code is still an internal WIP so until it's finished it won't be added into the final module
    /*private void TestRoadsOnonClickedButton(ISleekElement button)
    {
        if (!_selectedObject) return;
        LevelObject levelObject = FindLevelObjectByGameObject(_selectedObject.gameObject);
        if (levelObject == null) return;
        
        LSystemGenerator generator = new([new Rule('F', ["[+F][-F]"])], "[F]-F+", 3);
        GameObject toDestroy = new GameObject("Testing");
        RoadHelper roadHelper = toDestroy.AddComponent<RoadHelper>();
        roadHelper.roadStraight = Assets.find(Guid.Parse("b832729132a546a29eaedac06b84a46f")) as ObjectAsset;
        roadHelper.roadTee = Assets.find(Guid.Parse("8221c9c6360c4629a7dc1a197fac0196")) as ObjectAsset;
        roadHelper.roadQuad = Assets.find(Guid.Parse("bec22e8664b54b06aaee361c062980e3")) as ObjectAsset;
        roadHelper.roadCorner = Assets.find(Guid.Parse("329682e42ea141ea8d033278e88d763c")) as ObjectAsset;
        roadHelper.roadEnd = Assets.find(Guid.Parse("27501590239d4699a8dc001efa4dee37")) as ObjectAsset;

        Visualizer visualizer = new(generator, roadHelper, levelObject);
        visualizer.Start();
        
        Object.Destroy(toDestroy);
        generator = null;
        roadHelper = null;
        visualizer = null;
    }*/
    
    public void Initialize(ref EditorLevelObjectsUI uiInstance)
    {
        EditorLevelObjectsUI.assetsScrollBox.SizeOffset_Y = -340f;
        
        uiInstance.AddChild(_highlightButton);
        uiInstance.AddChild(_highlightColorsButton);
        uiInstance.AddChild(_filterButton);
        uiInstance.AddChild(_filterField);
        uiInstance.AddChild(_adjacentPlaceButton);
        uiInstance.AddChild(_highlightWrongScaleButton);
        uiInstance.AddChild(_selectHighlightedButton);
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
        uiInstance.AddChild(_layersContainer);
        uiInstance.AddChild(_layersMaskButton);
        uiInstance.AddChild(_schematicsButton);
        uiInstance.AddChild(_schematicsContainer);
        uiInstance.AddChild(_tagField);
        
        _schematicsScrollBox.SetData(EditorHelper.Instance.SchematicsManager.Schematics);
        _filterText = string.Empty;
    }
    
    private void OnSelectHighlightedClicked(ISleekElement button)
    {
        if (_highlightedObjects.Count < 1) return;
        
        List<Transform> toSelect = _highlightedObjects.Select(c => c.transform).ToList();
        UnhighlightAll();
        
        toSelect.ForEach(EditorObjects.addSelection);
    }
    
    private void OnHighlightWrongScaleClicked(ISleekElement button)
    {
        List<LevelObject> levelObjects = GetWrongScaledObjects();
        if (levelObjects.Count < 1) return;
        
        PrivateHighlight(levelObjects);
        _highlightWrongScaleButton.text = $"Highlight wrong objects ({levelObjects.Count})";
    }
    
    private void OnAdjacentPlaceClicked(ISleekElement button)
    {
        if (!_selectedObject) return;
        LevelObject? levelObject = FindLevelObjectByGameObject(_selectedObject);
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
        
        LevelObject? levelObject = FindLevelObjectByGameObject(_selectedObject);
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
        _highlightedTransforms.Clear();
        _focusHighlightIndex = 0;
        if (ignore != null)
        {
            _highlightedTransforms.Add(ignore.transform);
        }
        foreach (LevelObject lObj in levelObjects)
        {
            if (lObj == null || lObj.transform == null || lObj.transform.gameObject == null || lObj == ignore || lObj.transform == _selectedObject) continue;
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

    private void OnPositionValueUpdated(ISleekFloat32Field field, float value)
    {
        if (_selectedObject == null) return;
        LevelObject? levelObject = FindLevelObjectByGameObject(_selectedObject);
        if (levelObject == null) return; // Just in case

        Vector3 position = _selectedObject.position;

        // Probably there's a better way of check this
        if (field == _objectPositionX)
        {
            if (_objectPositionX.Value > Level.size || _objectPositionX.Value < Level.size * -1)
            {
                _objectPositionX.Value = position.x;
                return;
            }
            
            position.x = _objectPositionX.Value;
        }
        else if (field == _objectPositionY)
        {
            if (_objectPositionY.Value > Level.size || _objectPositionY.Value < Level.size * -1)
            {
                _objectPositionY.Value = position.y;
                return;
            }
            
            position.y = _objectPositionY.Value;
        }
        else if (field == _objectPositionZ)
        {
            if (_objectPositionZ.Value > Level.size || _objectPositionZ.Value < Level.size * -1)
            {
                _objectPositionZ.Value = position.z;
                return;
            }
            
            position.z = _objectPositionZ.Value;
        }
        
        _selectedObject.position = position;
        EditorObjects.calculateHandleOffsets();
    }
    
    private void OnRotationValueUpdated(ISleekFloat32Field field, float value)
    {
        if (_selectedObject == null) return;
        LevelObject? levelObject = FindLevelObjectByGameObject(_selectedObject);
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
        if (_selectedObject == null) return;
        LevelObject? levelObject = FindLevelObjectByGameObject(_selectedObject);
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

    public void OnObjectRemoved(ReunObjectRemove reunObjectRemove, Transform transform)
    {
        _cwtObjectRemove.Add(reunObjectRemove, new ReunObjectRemoveExtension(transform));
    }

    public void OnObjectRemovedUndo(ReunObjectRemove reunObjectRemove, Transform transform)
    {
        if (_cwtObjectRemove.TryGetValue(reunObjectRemove, out ReunObjectRemoveExtension value))
        {
            value.Undo(transform);
        }
    }

    public void OnObjectCopied(EditorCopy editorCopy, EditorSelection selection)
    {
        _cwtEditorCopy.Add(editorCopy, new EditorCopyExtension(selection.transform));
    }
    
    public void OnObjectPasted(EditorCopy editorCopy, Transform transform)
    {
        if (_cwtEditorCopy.TryGetValue(editorCopy, out EditorCopyExtension value))
        {
            value.Apply(transform);
        }
    }

    public void SelectObject(Transform selectedObject)
    {
        LevelObject? levelObject = FindLevelObjectByGameObject(selectedObject);
        string tag = string.Empty;
        if (levelObject != null && _dicLevelObjects.TryGetValue(levelObject, out LevelObjectExtension extension))
        {
            tag = extension.Tag;
        }

        _tagField.Text = tag;
        
        if (!InputEx.GetKey(ControlsSettings.modify) && EditorObjects.selection.Count == 1 && tag != string.Empty && selectedObject != null)
        {
            List<KeyValuePair<LevelObject, LevelObjectExtension>> objects = _dicLevelObjects.Where(c => c.Value.Tag == tag).ToList();
            if (objects.Count > 1)
            {
                SelectSameTagObjects(objects.Select(c => c.Key.transform).ToList());
            }
        }
        
        _selectedObject = selectedObject;
        
        if (_selectedObject == null)
        {
            return;
        }
        
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

    private void SelectSameTagObjects(List<Transform> selectedObjects)
    {
        EditorObjects.selection.Clear();
        //selectedObjects.ForEach(EditorObjects.addSelection);
        foreach (Transform select in selectedObjects)
        {
            HighlighterTool.highlight(select, Color.yellow);
            EditorObjects.selectDecals(select, true);
            EditorObjects.selection.Add(new EditorSelection(select, select.position, select.rotation, select.localScale));    
        }
        EditorObjects.calculateHandleOffsets();
    }
    
    public void UnhighlightAll(Transform? ignore = null)
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

    // Basically a CustomUpdate which doesn't replace the original update function.
    public void LateUpdate()
    {
        if (EditorObjects.selection == null) return;
        ChangeButtonsVisibility(EditorObjects.selection.Count == 1);
        // TODO: Implement a grid system with options to automatically align objects with the grid corners.

        // In a normal function I'll usually avoid extra indentation, but if in a future I add more stuff I can't cancel the whole execution just
        // for a feature.
        if (_highlightedTransforms.Count > 0 && EditorObjects.selection.Count == 1)
        {
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
        
    }
    
    private void ChangeButtonsVisibility(bool visible)
    {
        if (!visible)
        {
            UnhighlightAll();
        }

        _tagField.IsVisible = visible;
        EditorLevelObjectsUI.assetsScrollBox.SizeOffset_Y = visible ? -340f : -300f;
        _highlightButton.IsVisible = visible;
        _highlightColorsButton.IsVisible = visible;
        
        bool oldVisible = _filterButton.IsVisible;
        _filterButton.IsVisible = visible;
        if (visible && EditorLevelObjectsUI.active && !oldVisible)
        {
            _filterButton.text = "Filter objects";
        }
        _filterField.IsVisible = visible;

        _adjacentPlaceButton.IsVisible = visible;
        oldVisible = _highlightWrongScaleButton.IsVisible;
        _highlightWrongScaleButton.IsVisible = visible;
        if (visible && EditorLevelObjectsUI.active && !oldVisible)
        {
            _highlightWrongScaleButton.text = "Highlight wrong objects";
        }

        _selectHighlightedButton.IsVisible = visible;
        
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

    private void UpdateSchematicsScrollbox()
    {
        if (_schematicSearchValue.Length > 0)
        {
            _schematicsScrollBox.SetData(EditorHelper.Instance.SchematicsManager.Schematics
                .Where(c => c.Name.ToLower().Contains(_schematicSearchValue)).ToList());
        }
        else
        {
            _schematicsScrollBox.SetData(EditorHelper.Instance.SchematicsManager.Schematics);
        }
    }

    private LevelObject? FindLevelObjectByGameObject(Transform transform)
    {
        if (transform == null)
        {
            return null;
        }
        
        if (Regions.tryGetCoordinate(transform.position, out byte x, out byte y))
        {
            for (int i = 0; i < LevelObjects.objects[x, y].Count; i++)
            {
                if (LevelObjects.objects[x, y][i] != null && LevelObjects.objects[x, y][i].transform == transform)
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

    private List<LevelObject> GetWrongScaledObjects()
    {
        IEnumerable<LevelObject> levelObjects = LevelObjects.objects.Cast<List<LevelObject>>().SelectMany(list => list);

        return levelObjects.Where(c => c != null && c.transform != null && HaveNegativeScale(c.transform.localScale)).ToList();
    }

    private bool HaveNegativeScale(Vector3 scale)
    {
        for (int i = 0; i < 3; i++)
        {
            if (scale[i] < 0f)
            {
                return true;
            }
        }

        return false;
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

    public void SaveObjectTags()
    {
        if (_dicLevelObjects.Count < 1) return;

        List<SerializableLevelObjectTag> tags = [];
        foreach (KeyValuePair<LevelObject, LevelObjectExtension> lObj in _dicLevelObjects)
        {
            tags.Add(new SerializableLevelObjectTag(lObj.Key.instanceID, lObj.Value.Tag));
        }
        
        string? json = JsonConvert.SerializeObject(tags);
        if (json == null) return;
        
        using StreamWriter writer =  File.CreateText(Path.Combine(Level.info.path, "Level/ObjectsTags.json"));
        writer.Write(json);
        writer.Flush();
        writer.Close();
    }

    public void LoadObjectTags()
    {
        string path = Path.Combine(Level.info.path, "Level/ObjectsTags.json");
        if (!File.Exists(path)) return;
        string text = File.ReadAllText(path);
        List<SerializableLevelObjectTag>? objectTags = JsonConvert.DeserializeObject<List<SerializableLevelObjectTag>>(text);
        if (objectTags == null)
        {
            return;
        }
        List<LevelObject> objects = LevelObjects.objects.Cast<List<LevelObject>>().SelectMany(list => list).ToList();
        if (objects == null || !objects.Any()) return;
        foreach (SerializableLevelObjectTag objTag in objectTags)
        {
            LevelObject? levelObject = objects.FirstOrDefault(c => c.instanceID == objTag.InstanceID);
            if (levelObject == null)
            {
                UnturnedLog.warn($"[ObjectTag] Level Object with instance id {objTag.InstanceID} for tag: {objTag.Tag} not found!");
                continue;
            }
            
            _dicLevelObjects.Add(levelObject, new LevelObjectExtension(objTag.Tag));
        }
    }
}