using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EditorHelper.Builders;
using EditorHelper.Models;
using SDG.Framework.Utilities;
using SDG.Unturned;

namespace EditorHelper.Editor;

public class VisibilityManager
{
    // Usually the button builder shouldn't be saved into a field, but in this case it's required to properly align new button positions
    private readonly UIBuilder _builder;
    private readonly SleekButtonIcon _saveLocationButton;
    private readonly SleekButtonIcon _removeLocationButton;

    private readonly Dictionary<CameraPosition, SleekButtonIcon> _savedLocationsButton;

    private CameraPosition _selectedPosition;
    
    public VisibilityManager()
    {
        _savedLocationsButton = [];
        _selectedPosition = null;
        
        _builder = new UIBuilder();

        _builder.SetPositionOffsetY(-40f)
            .SetText("Remove location");

        _removeLocationButton = _builder.BuildButton("Remove your selected position");
        _removeLocationButton.onClickedButton += OnRemoveLocationButtonClicked;

        _builder.SetText("Save location");
        
        _saveLocationButton = _builder.BuildButton("Save your current camera location");
        _saveLocationButton.onClickedButton += OnSaveLocationButtonClicked;
    }
    
    public void Initialize()
    {
        TimeUtility.InvokeAfterDelay(() => DelayedStart(), 0f);

        UpdatePositions();
    }

    private IEnumerator DelayedStart()
    {
        yield return null;
        yield return null;
        EditorLevelVisibilityUI.container.AddChild(_removeLocationButton);
        EditorLevelVisibilityUI.container.AddChild(_saveLocationButton);
        yield break;
    } 

    private void OnRemoveLocationButtonClicked(ISleekElement button)
    {
        if (_selectedPosition == null || !_savedLocationsButton.TryGetValue(_selectedPosition, out SleekButtonIcon positionButton))
        {
            return;
        }
        
        positionButton.onClickedButton -= OnPositionButtonClicked;
        EditorLevelVisibilityUI.container.RemoveChild(positionButton);
        _savedLocationsButton.Remove(_selectedPosition);
        _selectedPosition = null;
        UpdatePositions();
    }

    private void OnSaveLocationButtonClicked(ISleekElement button)
    {
        _savedLocationsButton.Add(new CameraPosition(MainCamera.instance.transform.parent.position, MainCamera.instance.transform.rotation), null);
        
        UpdatePositions();
    }
    
    private void OnPositionButtonClicked(ISleekElement element)
    {
        SleekButtonIcon button = element as SleekButtonIcon;

        if (_savedLocationsButton.Any(c => c.Value == button))
        {
            KeyValuePair<CameraPosition, SleekButtonIcon> positionValuePair = _savedLocationsButton.First(c => c.Value == button);

            MainCamera.instance.transform.parent.position = positionValuePair.Key.Position;
            MainCamera.instance.transform.parent.rotation = positionValuePair.Key.Rotation;
            _selectedPosition = positionValuePair.Key;
        }
    }

    private void UpdatePositions()
    {
        _builder.SetPositionOffsetY(-130f);
        SleekFullscreenBox container = EditorLevelVisibilityUI.container;
        List<KeyValuePair<CameraPosition, SleekButtonIcon>> list = _savedLocationsButton.ToList();
        if (list.Count < 1) return;
        const string tooltip = "Teleport to the saved position";
        for (int i = 0; i < list.Count; i++)
        {
            KeyValuePair<CameraPosition, SleekButtonIcon> info = list[i];
            
            if (info.Value != null)
            {
                container.RemoveChild(info.Value);
            }

            _builder.SetText($"Position #{i + 1}");
            SleekButtonIcon button = _builder.BuildButton(tooltip);
            button.onClickedButton += OnPositionButtonClicked;
            container.AddChild(button);

            _savedLocationsButton[info.Key] = button;
        }
    }
}