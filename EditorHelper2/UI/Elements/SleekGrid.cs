using EditorHelper2.UI.Builders;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EditorHelper2.UI.Elements;

public class SleekGrid<T> : SleekWrapper where T : class
{
    private readonly ISleekScrollView _verticalScrollView;
    private ISleekBox _scrollBackground = null!;

    public int itemSize
    {
        set
        {
            itemHeight = value;
            itemWidth = value;
        }
    }
    public int itemHeight;
    public int itemWidth;
    public int itemPadding;

    public int Columns { get; private set; }
    public int Rows { get; private set; }
    private void RecalculateColumnsRows()
    {
        if (_data == null || _data.Count == 0)
        {
            Columns = 0;
            Rows = 0;
            return;
        }

        int width = Mathf.FloorToInt(SizeOffset_X);
        Columns = width / (itemWidth + itemPadding);
        Rows = Mathf.CeilToInt((float)_data.Count / (float)Columns);
    }

    public float ContentHeight
    {
        get => _verticalScrollView.ContentSizeOffset.y;
        private set
        {
            _verticalScrollView.ContentSizeOffset = new Vector2(0f, value);
        }
    }

    public delegate ISleekElement CreateElement(T item);

    public CreateElement? OnCreateElement;
    public Action<ISleekElement>? OnRemoveElement;

    private List<T>? _data;
    private List<VisibleEntry>? _visibleEntries;

    private readonly struct VisibleEntry(T item, ISleekElement element)
    {
        public readonly T Item = item;
        public readonly ISleekElement Element = element;
    }

    public SleekGrid()
    {
        _verticalScrollView = Glazier.Get().CreateScrollView();
        _verticalScrollView.SizeScale_X = 1f;
        _verticalScrollView.SizeScale_Y = 1f;
        _verticalScrollView.OnNormalizedValueChanged += OnNormalizedScrollChanged;
        AddChild(_verticalScrollView);

        AddScrollBackground();
    }

    // Allows for scrolling between grid items
    private void AddScrollBackground()
    {
        UIBuilder builder = new(0, 0);

        builder.SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(0f)
            .SetOffsetHorizontal(0f)
            .SetOffsetVertical(0f)
            .SetScaleHorizontal(1f)
            .SetScaleVertical(1f);

        _scrollBackground = builder.BuildAlphaBox(); // Needs to always be a new Instance otherwise AddChild throws a NullReferenceException
        _verticalScrollView.AddChild(_scrollBackground);
    }

    private void DestroyAllVisibleEntries()
    {
        if (_visibleEntries == null) return;

        if (OnRemoveElement != null)
        {
            foreach (var visibleEntry in _visibleEntries)
                OnRemoveElement(visibleEntry.Element);
        }
        _verticalScrollView.RemoveAllChildren();
        AddScrollBackground();
        _visibleEntries.Clear();
    }

    private void OnNormalizedScrollChanged(Vector2 normalizedScrollPosition) => UpdateVisibleEntries(normalizedScrollPosition.y);
    private void UpdateVisibleEntries() => UpdateVisibleEntries(_verticalScrollView.NormalizedVerticalPosition);
    private void UpdateVisibleEntries(float normalizedScrollPosition)
    {
        int visibleRows = Mathf.CeilToInt(_verticalScrollView.NormalizedViewportHeight * Rows);
        int minRow = Mathf.Max(0, Mathf.FloorToInt(normalizedScrollPosition * (float)(Rows - visibleRows)));
        int maxRow = Mathf.Min(Rows, minRow + visibleRows + 1);

        for (int i = _visibleEntries!.Count - 1; i >= 0; i--)
        {
            VisibleEntry visibleEntry = _visibleEntries[i];
            if (IsItemVisible(visibleEntry.Item, minRow, maxRow)) continue;

            OnRemoveElement?.Invoke(visibleEntry.Element);
            _verticalScrollView.RemoveChild(visibleEntry.Element);
            _visibleEntries.RemoveAtFast(i);
        }

        if (OnCreateElement == null)
        {
            CommandWindow.LogError($"{nameof(OnCreateElement)} is null in {nameof(SleekGrid<T>)}!");
            return;
        }

        for (int i = (minRow * Columns); i < (maxRow * Columns); i++)
        {
            if (i >= _data!.Count) break;
            T item = _data[i];
            if (_visibleEntries.Any(v => v.Item == item)) continue;

            int column = i % Columns;
            int row = i / Columns;

            ISleekElement sleekElement = OnCreateElement(_data[i]);
            sleekElement.SizeOffset_X = itemWidth;
            sleekElement.SizeOffset_Y = itemHeight;
            sleekElement.PositionOffset_X = column * (itemWidth + itemPadding);
            sleekElement.PositionOffset_Y = row * (itemHeight + itemPadding);
            _verticalScrollView.AddChild(sleekElement);

            _visibleEntries.Add(new VisibleEntry(_data[i], sleekElement));
        }
        _verticalScrollView.Update(); // Had issues with incorrect ordering without this
    }

    private bool IsItemVisible(T item, int minRow, int maxRow)
    {
        for (int d = (minRow * Columns); d < (maxRow * Columns); d++)
        {
            if (d >= _data!.Count) return false;
            if (_data[d] == item) return true;
        }

        return false;
    }

    public void ForceRebuildElements()
    {
        DestroyAllVisibleEntries();

        RecalculateColumnsRows();
        ContentHeight = (Rows * (itemHeight + itemPadding)) - (Rows > 0 ? itemPadding : 0f);
        _scrollBackground.SizeOffset_X = SizeOffset_X;
        _scrollBackground.SizeOffset_Y = ContentHeight;

        UpdateVisibleEntries();
    }

    public void SetData(List<T> data)
    {
        DestroyAllVisibleEntries();

        _data = data;
        RecalculateColumnsRows();
        ContentHeight = (Rows * (itemHeight + itemPadding)) - (Rows > 0 ? itemPadding : 0f);
        _scrollBackground.SizeOffset_X = SizeOffset_X;
        _scrollBackground.SizeOffset_Y = ContentHeight;

        _visibleEntries = new(_data.Count);
        UpdateVisibleEntries();
    }

    public T? GetItemFromVisibleElement(ISleekElement visibleElement)
    {
        return _visibleEntries.FirstOrDefault(v => v.Element == visibleElement).Item;
    }
}
