using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Builders;

/// <summary>
/// Helper class under builder pattern to create multiple buttons in a seamless way
/// </summary>
public class ButtonBuilder
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
    
    // Almost all buttons require a text of some sort
    private string _text = "Button";

    public ButtonBuilder(float sizeOffsetX = 200f, float sizeOffsetY = 30f, float positionScaleX = 0f, float positionScaleY = 1f)
    {
        _sizeOffsetX = sizeOffsetX;
        _sizeOffsetY = sizeOffsetY;
        _positionScaleX = positionScaleX;
        _positionScaleY = positionScaleY;

        _spacing = _sizeOffsetY + 10f;
    }
    
    public ButtonBuilder SetPositionOffsetX(float x)
    {
        _positionOffsetX = x;
        return this;
    }
    
    public ButtonBuilder SetPositionOffsetY(float y)
    {
        _positionOffsetY = y;
        return this;
    }

    public ButtonBuilder SetSizeOffsetX(float x)
    {
        _sizeOffsetX = x;
        return this;
    }
    
    public ButtonBuilder SetSizeOffsetY(float y)
    {
        _sizeOffsetY = y;
        _spacing = _sizeOffsetY + 10f;
        return this;
    }

    public ButtonBuilder SetPositionScaleX(float x)
    {
        _positionScaleX = x;
        return this;
    }
    
    public ButtonBuilder SetPositionScaleY(float y)
    {
        _positionScaleY = y;
        return this;
    }

    /// <summary>
    /// Modify the spacing only for the next build.
    /// </summary>
    /// <param name="spacing">Amount of space to use</param>
    /// <returns>Button builder instance</returns>
    public ButtonBuilder SetOneTimeSpacing(float spacing)
    {
        _positionOffsetY += _positionScaleY < 1f ? -_spacing : _spacing;
        _positionOffsetY += _positionScaleY < 1f ? spacing : -spacing;
        return this;
    }
    
    public ButtonBuilder SetText(string text)
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
}