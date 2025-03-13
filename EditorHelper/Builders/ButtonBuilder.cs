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
    private readonly float _positionScaleY = 1f;

    private float _spacing;
    
    // Almost all buttons require a text of some sort
    private string _text = "Button";

    public ButtonBuilder(float sizeOffsetX = 200f, float sizeOffsetY = 30f)
    {
        _sizeOffsetX = sizeOffsetX;
        _sizeOffsetY = sizeOffsetY;

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

    /// <summary>
    /// Modify the spacing only for the next build.
    /// </summary>
    /// <param name="spacing">Amount of space to use</param>
    /// <returns>Button builder instance</returns>
    public ButtonBuilder SetOneTimeSpacing(float spacing)
    {
        _positionOffsetY -= -_spacing;
        _positionOffsetY += -spacing;
        return this;
    }
    
    public ButtonBuilder SetText(string text)
    {
        _text = text;
        return this;
    }

    public SleekButtonState BuildButtonState(params GUIContent[] states)
    {
        SleekButtonState buttonState = new(states)
        {
            PositionOffset_X = _positionOffsetX,
            PositionOffset_Y = _positionOffsetY,
            PositionScale_Y = _positionScaleY,
            SizeOffset_X = _sizeOffsetX,
            SizeOffset_Y = _sizeOffsetY,
            tooltip = _text
        };
        
        _positionOffsetY += -_spacing;
        return buttonState;
    }

    public SleekButtonIcon BuildButton(string tooltip, Texture2D icon = null)
    {        
        SleekButtonIcon button = new(icon)
        {
            PositionOffset_X = _positionOffsetX,
            PositionOffset_Y = _positionOffsetY,
            PositionScale_Y = _positionScaleY,
            SizeOffset_X = _sizeOffsetX,
            SizeOffset_Y = _sizeOffsetY,
            text = _text,
            tooltip = tooltip
        };
        
        _positionOffsetY += -_spacing;
        return button;
    }

    public ISleekFloat32Field BuildFloatInput(ESleekSide labelSide = ESleekSide.LEFT)
    {   
        ISleekFloat32Field floatField = Glazier.Get().CreateFloat32Field();
        floatField.PositionOffset_X = _positionOffsetX;
        floatField.PositionOffset_Y = _positionOffsetY;
        floatField.PositionScale_Y = _positionScaleY;
        floatField.SizeOffset_X = _sizeOffsetX;
        floatField.SizeOffset_Y = _sizeOffsetY;
        floatField.Value = 0f;
        if (_text.Length > 0)
        {
            floatField.AddLabel(_text, labelSide);
        }
        
        _positionOffsetY += -_spacing;
        return floatField;
    }
    
    public ISleekToggle BuildToggle(string tooltipText = "", ESleekSide labelSide = ESleekSide.RIGHT)
    {
        ISleekToggle toggle = Glazier.Get().CreateToggle();
        toggle.PositionOffset_X = _positionOffsetX;
        toggle.PositionOffset_Y = _positionOffsetY;
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

        _positionOffsetY += -_spacing;
        return toggle;
    }

    public ISleekLabel BuildLabel(TextAnchor textAnchor = TextAnchor.MiddleCenter)
    {
        ISleekLabel label = Glazier.Get().CreateLabel();
        label.PositionOffset_X = _positionOffsetX;
        label.PositionOffset_Y = _positionOffsetY;
        label.PositionScale_Y = _positionScaleY;
        label.SizeOffset_X = _sizeOffsetX;
        label.SizeOffset_Y = _sizeOffsetY;
        label.Text = _text;
        label.TextAlignment = textAnchor;

        _positionOffsetY += -_spacing;
        return label;
    }

    public ISleekField BuildStringField()
    {
        ISleekField stringField = Glazier.Get().CreateStringField();
        stringField.PositionOffset_X = _positionOffsetX;
        stringField.PositionOffset_Y = _positionOffsetY;
        stringField.PositionScale_Y = _positionScaleY;
        stringField.SizeOffset_X = _sizeOffsetX;
        stringField.SizeOffset_Y = _sizeOffsetY;
        if (_text.Length > 0)
        {
            stringField.PlaceholderText = _text;
        }
        
        _positionOffsetY += -_spacing;
        return stringField;
    }
}