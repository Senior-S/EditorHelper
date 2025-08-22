using System.Collections.Generic;
using System.Linq;
using DanielWillett.UITools;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Helpers;
using EditorHelper2.common.Types;
using EditorHelper2.UI.Builders;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Level.Objects;

[UIExtension(typeof(EditorLevelObjectsUI))]
[EHExtension("Schematics Extension", "Senior S")]
public class SchematicsExtension : UIExtension, IExtension
{
    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;
    
    private readonly SleekButtonIcon _schematicsButton;
    private readonly ISleekBox _schematicsContainer;
    private readonly SleekList<Schematic> _schematicsScrollBox;
    private readonly ISleekField _schematicNameField;
    private readonly SleekButtonIcon _saveSchematicButton;
    private readonly SleekButtonIcon _schematicsHowToButton;
    private readonly SleekButtonIcon _schematicsReload;
    private readonly ISleekField _schematicSearch;

    private string _schematicNameValue = string.Empty;
    private string _schematicSearchValue = string.Empty;
    private SchematicsHelper _schematicsHelper;
    
    public SchematicsExtension()
    {
        UIBuilder builder = new(200f, 30f);

        builder.SetAnchorHorizontal(1f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(-230f)
            .SetOffsetVertical(EditorLevelObjectsUI.assetsScrollBox.SizeOffset_Y + 200f)
            .SetText("Schematics");
        
        _schematicsButton = builder.BuildButton("Open the schematics screen");
        
        builder.ResetProperties()
            .SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(0.5f)
            .SetOffsetHorizontal(-325f)
            .SetOffsetVertical(-200f)
            .SetSizeHorizontal(650f)
            .SetSizeVertical(400f)
            .SetText("");

        _schematicsContainer = builder.BuildBox();
        _schematicsContainer.IsVisible = false;

        builder.SetAnchorHorizontal(0)
            .SetAnchorVertical(0)
            .SetOffsetHorizontal(-210f)
            .SetOffsetVertical(0)
            .SetSizeHorizontal(200f)
            .SetSizeVertical(30f)
            .SetText("Schematic name");
        
        _schematicNameField = builder.BuildStringField();
        
        builder.SetOffsetHorizontal(-210f)
            .SetOffsetVertical(40f)
            .SetSizeHorizontal(200f)
            .SetSizeVertical(30f)
            .SetText("Save schematic");
        
        _saveSchematicButton = builder.BuildButton("Save schematic");
        
        builder.SetOffsetHorizontal(-210f)
            .SetOffsetVertical(80f)
            .SetSizeHorizontal(200f)
            .SetSizeVertical(30f)
            .SetText("How to use schematics");
        
        _schematicsHowToButton = builder.BuildButton("How to use schematics");
        
        builder.SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(0)
            .SetOffsetHorizontal(-100f)
            .SetOffsetVertical(-40f)
            .SetSizeHorizontal(200f)
            .SetSizeVertical(30f)
            .SetText("Search Term");

        _schematicSearch = builder.BuildStringField();
        
        builder.SetAnchorVertical(1f)
            .SetOffsetHorizontal(-100f)
            .SetOffsetVertical(5f)
            .SetSizeHorizontal(200f)
            .SetSizeVertical(30f)
            .SetText("Reload schematics");
        
        _schematicsReload = builder.BuildButton("Reload all schematics in the schematics folder");
        
        // This shitty scroll box took like half an hour due Nelson's way of doing it sucks :>
        // So for anyone reading this and for the future me, I added comments to know how to read it and use it.
        builder = builder.ResetProperties()
            .SetAnchorVertical(1f) // IMPORTANT! We want to start the scroll box at the bottom
            .SetOffsetHorizontal(10f)
            .SetOffsetVertical(-390f) // Then we define the area of the scroll box. It's negative due we're already at the bottom so we need to go up
            .SetSizeHorizontal(630f) // Size/Area of the scrollbox
            .SetSizeVertical(-10f) // And once the scrollbox area is done we added the bottom padding
            .SetScaleVertical(1f); // IMPORTANT 2! We want the scrollbox to automatically resize based on the objects

        _schematicsScrollBox = builder.BuildScrollBox<Schematic>(50, 10);
        
        Initialize();
    }
    
    public void Initialize()
    {
        if (_container == null) return;
        _schematicsHelper = new SchematicsHelper();
        
        EditorLevelObjectsUI.assetsScrollBox.SizeOffset_Y -= 40f;
        
        _container.AddChild(_schematicsContainer);
        _container.AddChild(_schematicsButton);
        _schematicsContainer.AddChild(_schematicNameField);
        _schematicsContainer.AddChild(_saveSchematicButton);
        _schematicsContainer.AddChild(_schematicsHowToButton);
        _schematicsContainer.AddChild(_schematicSearch);
        _schematicsContainer.AddChild(_schematicsReload);
        _schematicsContainer.AddChild(_schematicsScrollBox);
        
        _schematicsButton.onClickedButton += OnSchematicsButtonClicked;
        _schematicNameField.OnTextSubmitted += SchematicNameOnTextSubmitted;
        _saveSchematicButton.onClickedButton += OnSaveSchematicButtonClickedButton;
        _schematicsHowToButton.onClickedButton += OnSchematicsHowToButtonClickedButton;
        _schematicSearch.OnTextSubmitted += OnSchematicSearchTextChanged;
        _schematicsReload.onClickedButton += OnSchematicsReloadClickedButton;
        _schematicsScrollBox.onCreateElement = OnCreateSchematicModel;
        
        UpdateSchematicsScrollbox();
    }
    
    #region Event Handlers
    private void OnSchematicsButtonClicked(ISleekElement button)
    {
        _schematicsContainer.IsVisible = !_schematicsContainer.IsVisible;
        if (_schematicsContainer.IsVisible)
        {
            IconsExtension? iconsExtension = UnturnedUIToolsNexus.UIExtensionManager.GetInstance<IconsExtension>();
            iconsExtension?.HideIconGridContainer();
        }
    }

    private void SchematicNameOnTextSubmitted(ISleekField field)
    {
        _schematicNameValue = field.Text;
    }
    
    private void OnSaveSchematicButtonClickedButton(ISleekElement button)
    {
        if (EditorObjects.copies.Count < 1)
        {
            //EditorHelper.Instance.EditorManager.DisplayAlert("You need to have copied at least 1 object to create a schematic.");
            return;
        }
        
        if (_schematicNameValue.Length < 2)
        {
            _schematicNameValue = _schematicNameField.Text;
            if (_schematicNameValue.Length < 2)
            {
                //EditorHelper.Instance.EditorManager.DisplayAlert("Please provide a schematic name with at least 2 letters!");
                return;
            }
        }
        
        _schematicsHelper.SaveSchematic(_schematicNameValue);
        UpdateSchematicsScrollbox();
    }
    
    private void OnSchematicsHowToButtonClickedButton(ISleekElement button)
    {
        Provider.openURL("https://www.youtube.com/watch?v=9myiTLl7Eq4");
    }
    
    private void OnSchematicSearchTextChanged(ISleekField field)
    {
        _schematicSearchValue = field.Text.ToLower();
        UpdateSchematicsScrollbox();
    }
    
    private void OnSchematicsReloadClickedButton(ISleekElement button)
    {
        _schematicsHelper.ReloadSchematics();
        UpdateSchematicsScrollbox();
    }
    
    private void OnSchematicClicked(ISleekElement button)
    {
        int index = Mathf.FloorToInt(button.PositionOffset_Y / 60f);
        
        Schematic? model = _schematicsHelper.TryLoadSchematic(index, _schematicSearchValue);
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
    #endregion

    #region Extension functions
    public void HideSchematicsContainer() => _schematicsContainer.IsVisible = false;

    private ISleekElement OnCreateSchematicModel(Schematic item)
    {
        UIBuilder builder = new(0f, 0f);
        // The schematic model may not be fully loaded yet, if that's the case the SchematicModel won't have any objects.
        string objectsCount = item.Objects.Count == 0 ? "Unknown objects" : $"{item.Objects.Count} objects";
        
        builder.SetText($"{item.Name} by {item.Author} ({objectsCount})");
        ISleekButton button = builder.CreateSimpleButton();
        button.OnClicked += OnSchematicClicked;
        
        return button;
    }

    private void UpdateSchematicsScrollbox()
    {
        if (_schematicSearchValue.Length > 0)
        {
            _schematicsScrollBox.SetData(_schematicsHelper.Schematics
                .Where(c => c.Name.ToLower().Contains(_schematicSearchValue)).ToList());
        }
        else
        {
            _schematicsScrollBox.SetData(_schematicsHelper.Schematics);
        }
    }
    #endregion

    public void Dispose()
    {
        if (_container == null) return;
        
        EditorLevelObjectsUI.assetsScrollBox.SizeOffset_Y += 40f;
        
        _container.RemoveChild(_schematicsContainer);
        _container.RemoveChild(_schematicsButton);
        _schematicsContainer.RemoveChild(_schematicNameField);
        _schematicsContainer.RemoveChild(_saveSchematicButton);
        _schematicsContainer.RemoveChild(_schematicsHowToButton);
        _schematicsContainer.RemoveChild(_schematicSearch);
        _schematicsContainer.RemoveChild(_schematicsReload);
        _schematicsContainer.RemoveChild(_schematicsScrollBox);
        
        _schematicsButton.onClickedButton -= OnSchematicsButtonClicked;
        _schematicNameField.OnTextSubmitted -= SchematicNameOnTextSubmitted;
        _saveSchematicButton.onClickedButton -= OnSaveSchematicButtonClickedButton;
        _schematicsHowToButton.onClickedButton -= OnSchematicsHowToButtonClickedButton;
        _schematicSearch.OnTextSubmitted -= OnSchematicSearchTextChanged;
        _schematicsReload.onClickedButton -= OnSchematicsReloadClickedButton;
        _schematicsScrollBox.onCreateElement = null;
    }
}