using System;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Builders;

/// <summary>
/// Helper class under builder pattern to create multiple UI elements in a seamless way
/// </summary>
public class UIBuilder
{
    private float _sizeOffsetX;
    private float _sizeOffsetY;
    
    private float _positionOffsetX = 10f;
    private float _positionOffsetY = 10f;
    // For future reference, the position scale is basically the pivot
    // X:0 Y:0 = Left Top
    private float _positionScaleX;
    private float _positionScaleY;

    private float _spacing;
    
    // Almost all elements require a text of some sort
    private string _text = string.Empty;

    public UIBuilder(float sizeOffsetX = 200f, float sizeOffsetY = 30f, float positionScaleX = 0f, float positionScaleY = 1f)
    {
        _sizeOffsetX = sizeOffsetX;
        _sizeOffsetY = sizeOffsetY;
        _positionScaleX = positionScaleX;
        _positionScaleY = positionScaleY;

        _spacing = _sizeOffsetY + 10f;
    }
    
    public UIBuilder SetPositionOffsetX(float x)
    {
        _positionOffsetX = x;
        return this;
    }
    
    public UIBuilder SetPositionOffsetY(float y)
    {
        _positionOffsetY = y;
        return this;
    }

    public UIBuilder SetSizeOffsetX(float x)
    {
        _sizeOffsetX = x;
        return this;
    }
    
    public UIBuilder SetSizeOffsetY(float y)
    {
        _sizeOffsetY = y;
        _spacing = _sizeOffsetY + 10f;
        return this;
    }

    public UIBuilder SetPositionScaleX(float x)
    {
        _positionScaleX = x;
        return this;
    }
    
    public UIBuilder SetPositionScaleY(float y)
    {
        _positionScaleY = y;
        return this;
    }

    /// <summary>
    /// Modify the spacing only for the next build.
    /// </summary>
    /// <param name="spacing">Amount of space to use</param>
    /// <returns>Button builder instance</returns>
    public UIBuilder SetOneTimeSpacing(float spacing)
    {
        _positionOffsetY += _positionScaleY < 1f ? -_spacing : _spacing;
        _positionOffsetY += _positionScaleY < 1f ? spacing : -spacing;
        return this;
    }
    
    public UIBuilder SetText(string text)
    {
        _text = text;
        return this;
    }

    private void ApplySpacing()
    {
        _positionOffsetY += _positionScaleY < 1f ? _spacing : -_spacing;
    }

    public SleekButtonState BuildButtonState(params GUIContent[] states)
    {
        SleekButtonState buttonState = new(states)
        {
            PositionOffset_X = _positionOffsetX,
            PositionOffset_Y = _positionOffsetY,
            PositionScale_X = _positionScaleX,
            PositionScale_Y = _positionScaleY,
            SizeOffset_X = _sizeOffsetX,
            SizeOffset_Y = _sizeOffsetY,
            tooltip = _text
        };

        ApplySpacing();
        return buttonState;
    }

    public SleekButtonIcon BuildButton(string tooltip, Texture2D icon = null)
    {        
        SleekButtonIcon button = new(icon)
        {
            PositionOffset_X = _positionOffsetX,
            PositionOffset_Y = _positionOffsetY,
            PositionScale_X = _positionScaleX,
            PositionScale_Y = _positionScaleY,
            SizeOffset_X = _sizeOffsetX,
            SizeOffset_Y = _sizeOffsetY,
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
        floatField.PositionScale_X = _positionScaleX;
        floatField.PositionScale_Y = _positionScaleY;
        floatField.SizeOffset_X = _sizeOffsetX;
        floatField.SizeOffset_Y = _sizeOffsetY;
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
        toggle.PositionScale_X = _positionScaleX;
        toggle.PositionScale_Y = _positionScaleY;
        toggle.SizeOffset_X = _sizeOffsetX;
        toggle.SizeOffset_Y = _sizeOffsetY;
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
        label.PositionScale_X = _positionScaleX;
        label.PositionScale_Y = _positionScaleY;
        label.SizeOffset_X = _sizeOffsetX;
        label.SizeOffset_Y = _sizeOffsetY;
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
        stringField.PositionScale_X = _positionScaleX;
        stringField.PositionScale_Y = _positionScaleY;
        stringField.SizeOffset_X = _sizeOffsetX;
        stringField.SizeOffset_Y = _sizeOffsetY;
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
        int32Field.PositionScale_X = _positionScaleX;
        int32Field.PositionScale_Y = _positionScaleY;
        int32Field.SizeOffset_X = _sizeOffsetX;
        int32Field.SizeOffset_Y = _sizeOffsetY;
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
        box.PositionScale_X = _positionScaleX;
        box.PositionScale_Y = _positionScaleY;
        box.SizeOffset_X = _sizeOffsetX;
        box.SizeOffset_Y = _sizeOffsetY;
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
        box.PositionScale_X = _positionScaleX;
        box.PositionScale_Y = _positionScaleY;
        box.SizeOffset_X = _sizeOffsetX;
        box.SizeOffset_Y = _sizeOffsetY;
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
            PositionScale_X = _positionScaleX,
            PositionScale_Y = _positionScaleY,
            SizeOffset_X = _sizeOffsetX,
            SizeOffset_Y = _sizeOffsetY,
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