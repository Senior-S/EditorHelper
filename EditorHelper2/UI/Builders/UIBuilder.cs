using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.UI.Builders;

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

    /// <summary>
    /// Horizontal stretch for the UI
    /// </summary>
    private float _scaleX;
    
    /// <summary>
    /// Vertical stretch for the UI
    /// </summary>
    private float _scaleY;
    
    private float _spacing;
    
    /// <summary>
    /// Default text for elements that require it
    /// </summary>
    private string _text = string.Empty;
    
    /// <summary>
    /// Create a new UIBuilder for the creation of different UI elements
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
    /// <returns>UIBuilder with the updated value</returns>
    public UIBuilder SetOffsetHorizontal(float x)
    {
        _positionOffsetX = x;
        return this;
    }
    
    /// <summary>
    /// Sets the vertical offset
    /// </summary>
    /// <param name="y">New value for the vertical offset</param>
    /// <returns>UIBuilder with the updated value</returns>
    public UIBuilder SetOffsetVertical(float y)
    {
        _positionOffsetY = y;
        return this;
    }

    /// <summary>
    /// Sets the horizontal size
    /// </summary>
    /// <param name="x">New value for the horizontal size</param>
    /// <returns>UIBuilder with the updated value</returns>
    public UIBuilder SetSizeHorizontal(float x)
    {
        _sizeX = x;
        return this;
    }
    
    /// <summary>
    /// Sets the vertical size
    /// </summary>
    /// <param name="y">New value for the vertical size</param>
    /// <returns>UIBuilder with the updated value</returns>
    public UIBuilder SetSizeVertical(float y)
    {
        _sizeY = y;
        _spacing = _sizeY + 10f;
        return this;
    }

    /// <summary>
    /// Sets the horizontal anchor
    /// </summary>
    /// <param name="x">New value for the horizontal anchor. 1 = right</param>
    /// <returns>UIBuilder with the updated value</returns>
    public UIBuilder SetAnchorHorizontal(float x)
    {
        _anchorX = x;
        return this;
    }
    
    /// <summary>
    /// Sets the vertical anchor
    /// </summary>
    /// <param name="y">New value for the vertical anchor. 1 = bottom</param>
    /// <returns>UIBuilder with the updated value</returns>
    public UIBuilder SetAnchorVertical(float y)
    {
        _anchorY = y;
        return this;
    }
    
    /// <summary>
    /// Sets the horizontal scale
    /// </summary>
    /// <param name="x">New value for the horizontal scale. 1 = cover the total horizontal</param>
    /// <returns>UIBuilder with the updated value</returns>
    public UIBuilder SetScaleHorizontal(float x)
    {
        _scaleX = x;
        return this;
    }
    
    /// <summary>
    /// Sets the vertical scale
    /// </summary>
    /// <param name="y">New value for the vertical scale. 1 = covert the total vertical</param>
    /// <returns>UIBuilder with the updated value</returns>
    public UIBuilder SetScaleVertical(float y)
    {
        _scaleY = y;
        return this;
    }
    
    /// <summary>
    /// Sets the text value
    /// </summary>
    /// <param name="text">New value for the text</param>
    /// <returns>UIBuilder with the updated value</returns>
    public UIBuilder SetText(string text)
    {
        _text = text;
        return this;
    }

    /// <summary>
    /// Sets the spacing value
    /// </summary>
    /// <param name="spacing">New value for the spacing</param>
    /// <returns>UIBuilder with the updated value</returns>
    public UIBuilder SetSpacing(float spacing)
    {
        _positionOffsetY += _anchorY < 1f ? -_spacing : _spacing;
        _spacing = spacing;
        _positionOffsetY += _anchorY < 1f ? _spacing : -_spacing;
        return this;
    }

    public UIBuilder ResetProperties()
    {
        _positionOffsetX = 0;
        _positionOffsetY = 0;
        _anchorX = 0f;
        _anchorY = 0f;
        _sizeX = 0f;
        _sizeY = 0f;
        _scaleX = 0f;
        _scaleY = 0f;
        _spacing = 0f;
        _text = string.Empty;
        return this;
    }
    
    
    /// <summary>
    /// Method called after every build to apply a default spacing
    /// </summary>
    private void ApplySpacing()
    {
        _positionOffsetY += _anchorY < 1f ? _spacing : -_spacing;
    }
    
    #region Builder functions
    public SleekButtonState BuildButtonState(params GUIContent[] states)
    {
        SleekButtonState buttonState = new(states)
        {
            tooltip = _text
        };
        FormatElement(ref buttonState);

        ApplySpacing();
        return buttonState;
    }

    public SleekButtonIcon BuildButton(string tooltip, Texture2D icon = null!, ESleekFontSize fontSize = ESleekFontSize.Medium)
    {        
        SleekButtonIcon button = new(icon)
        {
            tooltip = tooltip,
            fontSize = fontSize,
            iconColor = ESleekTint.FOREGROUND
        };
        if (_text.Length > 0)
        {
            button.text = _text;
            button.textColor = ESleekTint.FONT;
        }
        FormatElement(ref button);
        
        ApplySpacing();
        return button;
    }

    public ISleekFloat32Field BuildFloatInput(ESleekSide labelSide = ESleekSide.LEFT)
    {   
        ISleekFloat32Field floatField = Glazier.Get().CreateFloat32Field();
        floatField.Value = 0f;
        if (_text.Length > 0)
        {
            floatField.AddLabel(_text, labelSide);
            floatField.TextColor = ESleekTint.FONT;
        }
        FormatElement(ref floatField);
        
        ApplySpacing();
        return floatField;
    }

    public SleekFullscreenBox BuildFullscreenBox()
    {
        SleekFullscreenBox sleekFullscreenBox = new();
        FormatElement(ref sleekFullscreenBox);
        
        ApplySpacing();
        return sleekFullscreenBox;
    }
    
    public ISleekToggle BuildToggle(string tooltipText = "", ESleekSide labelSide = ESleekSide.RIGHT)
    {
        ISleekToggle toggle = Glazier.Get().CreateToggle();
        toggle.Value = true;
        toggle.ForegroundColor = ESleekTint.FOREGROUND;
        if (tooltipText.Length > 0)
        {
            toggle.TooltipText = tooltipText;
        }
        if (_text.Length > 0)
        {
            toggle.AddLabel(_text, labelSide);
            toggle.SideLabel!.TextColor = ESleekTint.FONT;
        }
        FormatElement(ref toggle);

        ApplySpacing();
        return toggle;
    }

    public ISleekLabel BuildLabel(TextAnchor textAnchor = TextAnchor.MiddleCenter)
    {
        ISleekLabel label = Glazier.Get().CreateLabel();
        label.Text = _text;
        label.TextAlignment = textAnchor;
        label.TextColor = ESleekTint.FONT;
        FormatElement(ref label);
        
        ApplySpacing();
        return label;
    }

    public ISleekField BuildStringField()
    {
        ISleekField stringField = Glazier.Get().CreateStringField();
        if (_text.Length > 0)
        {
            stringField.PlaceholderText = _text;
            stringField.TextColor = ESleekTint.FONT;
        }
        FormatElement(ref stringField);
        
        ApplySpacing();
        return stringField;
    }

    public ISleekInt32Field BuildInt32Field(string tooltipText = "")
    {
        ISleekInt32Field int32Field = Glazier.Get().CreateInt32Field();
        int32Field.TooltipText = tooltipText;
        if (_text.Length > 0)
        {
            int32Field.AddLabel(_text, ESleekSide.LEFT);
            int32Field.TextColor = ESleekTint.FONT;
        }
        FormatElement(ref int32Field);
        
        ApplySpacing();
        return int32Field;
    }

    public ISleekBox BuildBox(TextAnchor textAnchor = TextAnchor.MiddleCenter)
    {
        ISleekBox box = Glazier.Get().CreateBox();
        if (_text.Length > 0)
        {
            box.Text = _text;
            box.TextAlignment = textAnchor;
            box.AllowRichText = true;
            box.TextColor = ESleekTint.FONT;
        }
        FormatElement(ref box);
        
        ApplySpacing();
        return box;
    }
    
    public ISleekBox BuildAlphaBox(TextAnchor textAnchor = TextAnchor.MiddleCenter)
    {
        ISleekBox box = Glazier.Get().CreateBox();
        box.BackgroundColor = new SleekColor(ESleekTint.NONE, 0f);
        if (_text.Length > 0)
        {
            box.Text = _text;
            box.TextAlignment = textAnchor;
            box.AllowRichText = true;
            box.TextColor = ESleekTint.FONT;
        }
        FormatElement(ref box);
        
        ApplySpacing();
        return box;
    }

    public SleekList<T> BuildScrollBox<T>(int itemHeight, int itemPadding) where T : class
    {
        SleekList<T> scrollBox = new()
        {
            itemHeight = itemHeight,
            itemPadding = itemPadding
        };
        FormatElement(ref scrollBox);
        
        ApplySpacing();
        return scrollBox;
    }

    public ISleekScrollView BuildScrollView(bool scaleContentToWidth = false, bool scaleContentToHeight = false)
    {
        ISleekScrollView scrollView = Glazier.Get().CreateScrollView();
        scrollView.ScaleContentToWidth = scaleContentToWidth;
        scrollView.ScaleContentToHeight = scaleContentToHeight;
        FormatElement(ref scrollView);
        
        ApplySpacing();
        return scrollView;
    }

    public ISleekButton CreateSimpleButton()
    {
        ISleekButton button = Glazier.Get().CreateButton();
        if (_text.Length > 0)
        {
            button.Text = _text;
            button.TextColor = ESleekTint.FONT;
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
            box.TextColor = ESleekTint.FONT;
        } 
        return box;
    }
    
    public ISleekBox CreateSimpleAlphaBox(TextAnchor textAnchor = TextAnchor.MiddleCenter)
    {
        ISleekBox box = Glazier.Get().CreateBox();
        box.BackgroundColor = new SleekColor(ESleekTint.NONE, 0f);
        if (_text.Length > 0)
        {
            box.TextAlignment = textAnchor;
            box.Text = _text;
            box.TextColor = ESleekTint.FONT;
        } 
        return box;
    }
    #endregion

    /// <summary>
    /// Generic method to apply common properties to the <see cref="SDG.Unturned.ISleekElement"/> 
    /// </summary>
    /// <param name="element">UI Element</param>
    /// <typeparam name="T">Class extending from <see cref="SDG.Unturned.ISleekElement"/></typeparam>
    public void FormatElement<T>(ref T element) where T : ISleekElement
    {
        element.PositionOffset_X = _positionOffsetX;
        element.PositionOffset_Y = _positionOffsetY;
        element.PositionScale_X = _anchorX;
        element.PositionScale_Y = _anchorY;
        if (_sizeX != 0)
        {
            element.SizeOffset_X = _sizeX;
        }
        if (_sizeY != 0)
        {
            element.SizeOffset_Y = _sizeY;
        }
        if (_scaleX != 0)
        {
            element.SizeScale_X = _scaleX;
        }
        if (_scaleY != 0)
        {
            element.SizeScale_Y = _scaleY;
        }
    }
}