using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Factories;

/// <summary>
/// UI builder to create different UI elements
/// </summary>
// ReSharper disable once InconsistentNaming
public class UIBuilder
{
    /// <summary>
    /// Horizontal size
    /// </summary>
    private float _sizeX;
    
    /// <summary>
    /// Vertical size
    /// </summary>
    private float _sizeY;

    /// <summary>
    /// Absolute horizontal offset from the pivot
    /// </summary>
    private float _positionOffsetX;
    
    /// <summary>
    /// Absolute vertical offset from the pivot
    /// </summary>
    private float _positionOffsetY;
    
    /// <summary>
    /// Horizontal anchor for the UI placement
    /// 0 = Left
    /// </summary>
    private float _anchorX;
    
    /// <summary>
    /// Vertical anchor for the UI placement
    /// 0 = Top
    /// </summary>
    private float _anchorY;
    
    private float _spacing;
    
    /// <summary>
    /// Default text for elements that require it
    /// </summary>
    private string _text = string.Empty;
    
    /// <summary>
    /// Create a new UIFactory for the creation of different UI elements
    /// Defaulting pivots to Left-Bottom
    /// </summary>
    /// <param name="sizeX">Horizontal size</param>
    /// <param name="sizeY">Vertical size</param>
    public UIBuilder(float sizeX, float sizeY)
    {
        _sizeX = sizeX;
        _sizeY = sizeY;
        
        _positionOffsetX = 0;
        _positionOffsetY = 0;
        _anchorX = 0f;
        _anchorY = 1f;
        
        _spacing = _sizeY + 10f;
    }
    
    /// <summary>
    /// Sets the horizontal offset
    /// </summary>
    /// <param name="x">New value for the horizontal offset</param>
    /// <returns>UIFactory with the updated value</returns>
    public UIBuilder SetHorizontalOffset(float x)
    {
        _positionOffsetX = x;
        return this;
    }
    
    /// <summary>
    /// Sets the vertical offset
    /// </summary>
    /// <param name="y">New value for the vertical offset</param>
    /// <returns>UIFactory with the updated value</returns>
    public UIBuilder SetVerticalOffset(float y)
    {
        _positionOffsetY = y;
        return this;
    }

    /// <summary>
    /// Sets the horizontal size
    /// </summary>
    /// <param name="x">New value for the horizontal size</param>
    /// <returns>UIFactory with the updated value</returns>
    public UIBuilder SetHorizontalSize(float x)
    {
        _sizeX = x;
        return this;
    }
    
    /// <summary>
    /// Sets the vertical size
    /// </summary>
    /// <param name="y">New value for the vertical size</param>
    /// <returns>UIFactory with the updated value</returns>
    public UIBuilder SetVerticalSize(float y)
    {
        _sizeY = y;
        _spacing = _sizeY + 10f;
        return this;
    }

    /// <summary>
    /// Sets the horizontal anchor
    /// </summary>
    /// <param name="x">New value for the horizontal anchor. 1 = right</param>
    /// <returns>UIFactory with the updated value</returns>
    public UIBuilder SetHorizontalAnchor(float x)
    {
        _anchorX = x;
        return this;
    }
    
    /// <summary>
    /// Sets the vertical anchor
    /// </summary>
    /// <param name="y">New value for the vertical anchor. 1 = bottom</param>
    /// <returns>UIFactory with the updated value</returns>
    public UIBuilder SetVerticalAnchor(float y)
    {
        _anchorY = y;
        return this;
    }
    
    /// <summary>
    /// Sets the text value
    /// </summary>
    /// <param name="text">New value for the text</param>
    /// <returns>UIFactory with the updated value</returns>
    public UIBuilder SetText(string text)
    {
        _text = text;
        return this;
    }

    /// <summary>
    /// Sets the spacing value
    /// </summary>
    /// <param name="spacing">New value for the spacing</param>
    /// <returns>UIFactory with the updated value</returns>
    public UIBuilder SetSpacing(float spacing)
    {
        _positionOffsetY += _anchorY < 1f ? -_spacing : _spacing;
        _spacing = spacing;
        _positionOffsetY += _anchorY < 1f ? _spacing : -_spacing;
        return this;
    }
    
    /// <summary>
    /// Method called after every build to apply a default spacing
    /// </summary>
    private void ApplySpacing()
    {
        _positionOffsetY += _anchorY < 1f ? _spacing : -_spacing;
    }
    
    public SleekButtonState BuildButtonState(params GUIContent[] states)
    {
        SleekButtonState buttonState = new(states)
        {
            PositionOffset_X = _positionOffsetX,
            PositionOffset_Y = _positionOffsetY,
            PositionScale_X = _anchorX,
            PositionScale_Y = _anchorY,
            SizeOffset_X = _sizeX,
            SizeOffset_Y = _sizeY,
            tooltip = _text
        };

        ApplySpacing();
        return buttonState;
    }

    public SleekButtonIcon BuildButton(string tooltip, Texture2D icon = null!)
    {        
        SleekButtonIcon button = new(icon)
        {
            PositionOffset_X = _positionOffsetX,
            PositionOffset_Y = _positionOffsetY,
            PositionScale_X = _anchorX,
            PositionScale_Y = _anchorY,
            SizeOffset_X = _sizeX,
            SizeOffset_Y = _sizeY,
            text = _text,
            tooltip = tooltip
        };
        
        ApplySpacing();
        return button;
    }

    public ISleekFloat32Field BuildFloatInput(ESleekSide labelSide = ESleekSide.LEFT)
    {   
        ISleekFloat32Field floatField = Glazier.Get().CreateFloat32Field();
        floatField.PositionOffset_X = _positionOffsetX;
        floatField.PositionOffset_Y = _positionOffsetY;
        floatField.PositionScale_X = _anchorX;
        floatField.PositionScale_Y = _anchorY;
        floatField.SizeOffset_X = _sizeX;
        floatField.SizeOffset_Y = _sizeY;
        floatField.Value = 0f;
        if (_text.Length > 0)
        {
            floatField.AddLabel(_text, labelSide);
        }
        
        ApplySpacing();
        return floatField;
    }
    
    
    
    public ISleekToggle BuildToggle(string tooltipText = "", ESleekSide labelSide = ESleekSide.RIGHT)
    {
        ISleekToggle toggle = Glazier.Get().CreateToggle();
        toggle.PositionOffset_X = _positionOffsetX;
        toggle.PositionOffset_Y = _positionOffsetY;
        toggle.PositionScale_X = _anchorX;
        toggle.PositionScale_Y = _anchorY;
        toggle.SizeOffset_X = _sizeX;
        toggle.SizeOffset_Y = _sizeY;
        toggle.Value = true;
        if (tooltipText.Length > 0)
        {
            toggle.TooltipText = tooltipText;
        }
        
        if (_text.Length > 0)
        {
            toggle.AddLabel(_text, labelSide);
        }

        ApplySpacing();
        return toggle;
    }

    public ISleekLabel BuildLabel(TextAnchor textAnchor = TextAnchor.MiddleCenter)
    {
        ISleekLabel label = Glazier.Get().CreateLabel();
        label.PositionOffset_X = _positionOffsetX;
        label.PositionOffset_Y = _positionOffsetY;
        label.PositionScale_X = _anchorX;
        label.PositionScale_Y = _anchorY;
        label.SizeOffset_X = _sizeX;
        label.SizeOffset_Y = _sizeY;
        label.Text = _text;
        label.TextAlignment = textAnchor;

        ApplySpacing();
        return label;
    }

    public ISleekField BuildStringField()
    {
        ISleekField stringField = Glazier.Get().CreateStringField();
        stringField.PositionOffset_X = _positionOffsetX;
        stringField.PositionOffset_Y = _positionOffsetY;
        stringField.PositionScale_X = _anchorX;
        stringField.PositionScale_Y = _anchorY;
        stringField.SizeOffset_X = _sizeX;
        stringField.SizeOffset_Y = _sizeY;
        if (_text.Length > 0)
        {
            stringField.PlaceholderText = _text;
        }
        
        ApplySpacing();
        return stringField;
    }

    public ISleekInt32Field BuildInt32Field(string tooltipText = "")
    {
        ISleekInt32Field int32Field = Glazier.Get().CreateInt32Field();
        int32Field.PositionOffset_X = _positionOffsetX;
        int32Field.PositionOffset_Y = _positionOffsetY;
        int32Field.PositionScale_X = _anchorX;
        int32Field.PositionScale_Y = _anchorY;
        int32Field.SizeOffset_X = _sizeX;
        int32Field.SizeOffset_Y = _sizeY;
        int32Field.TooltipText = tooltipText;
        if (_text.Length > 0)
        {
            int32Field.AddLabel(_text, ESleekSide.LEFT);
        }
        
        ApplySpacing();
        return int32Field;
    }

    public ISleekBox BuildBox(TextAnchor textAnchor = TextAnchor.MiddleCenter)
    {
        ISleekBox box = Glazier.Get().CreateBox();
        box.PositionOffset_X = _positionOffsetX;
        box.PositionOffset_Y = _positionOffsetY;
        box.PositionScale_X = _anchorX;
        box.PositionScale_Y = _anchorY;
        box.SizeOffset_X = _sizeX;
        box.SizeOffset_Y = _sizeY;
        if (_text.Length > 0)
        {
            box.Text = _text;
            box.TextAlignment = textAnchor;
            box.AllowRichText = true;
        }
        
        ApplySpacing();
        return box;
    }
    
    public ISleekBox BuildAlphaBox(TextAnchor textAnchor = TextAnchor.MiddleCenter)
    {
        ISleekBox box = Glazier.Get().CreateBox();
        box.PositionOffset_X = _positionOffsetX;
        box.PositionOffset_Y = _positionOffsetY;
        box.PositionScale_X = _anchorX;
        box.PositionScale_Y = _anchorY;
        box.SizeOffset_X = _sizeX;
        box.SizeOffset_Y = _sizeY;
        box.BackgroundColor = new SleekColor(ESleekTint.NONE, 0f);
        if (_text.Length > 0)
        {
            box.Text = _text;
            box.TextAlignment = textAnchor;
            box.AllowRichText = true;
        }
        
        ApplySpacing();
        return box;
    }

    public SleekList<T> BuildScrollBox<T>(int itemHeight, int itemPadding) where T : class
    {
        SleekList<T> scrollBox = new()
        {
            PositionOffset_X = _positionOffsetX,
            PositionOffset_Y = _positionOffsetY,
            PositionScale_X = _anchorX,
            PositionScale_Y = _anchorY,
            SizeOffset_X = _sizeX,
            SizeOffset_Y = _sizeY,
            SizeScale_Y = 1f,
            itemHeight = itemHeight,
            itemPadding = itemPadding
        };

        ApplySpacing();
        return scrollBox;
    }

    public ISleekButton CreateSimpleButton()
    {
        ISleekButton button = Glazier.Get().CreateButton();
        if (_text.Length > 0)
        {
            button.Text = _text;
        }

        return button;
    }

    public ISleekBox CreateSimpleBox(TextAnchor textAnchor = TextAnchor.MiddleCenter)
    {
        ISleekBox box = Glazier.Get().CreateBox();
        if (_text.Length > 0)
        {
            box.TextAlignment = textAnchor;
            box.Text = _text;
        } 
        return box;
    }
    
    public ISleekBox CreateAlphaBox(TextAnchor textAnchor = TextAnchor.MiddleCenter)
    {
        ISleekBox box = Glazier.Get().CreateBox();
        box.BackgroundColor = new SleekColor(ESleekTint.NONE, 0f);
        if (_text.Length > 0)
        {
            box.TextAlignment = textAnchor;
            box.Text = _text;
        } 
        return box;
    }
}