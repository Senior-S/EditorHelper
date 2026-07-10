using System;
using System.Collections.Generic;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Keybinds;
using EditorHelper2.Extensions.Level.Objects;
using EditorHelper2.Extensions.Terrain.Foliage;
using EditorHelper2.Loader;
using EditorHelper2.UI.Builders;
using SDG.Framework.Devkit.Transactions;
using SDG.Unturned;
using UnityEngine;
using Action = System.Action;
using DiagnosticsDebug = System.Diagnostics.Debug;

namespace EditorHelper2.Extensions.Editor.Dashboard;

/// <summary>
/// Provides a searchable, keyboard-driven launcher for common editor actions.
/// </summary>
[UIExtension(typeof(EditorDashboardUI))]
[EHExtension("Command Palette Prototype", "Senior S", alwaysEnabled: true)]
public sealed class CommandPaletteExtension : UIExtension, IExtension
{
    private const int VisibleRowCount = 7;

    private static CommandPaletteExtension? _instance;

    [ExistingMember("terrainMenu")]
    private readonly EditorTerrainUI? _terrainUI;

    [ExistingMember("levelUI")]
    private readonly EditorLevelUI? _levelUI;

    private readonly SleekFullscreenBox _root;
    private readonly ISleekField _searchField;
    private readonly ISleekLabel _resultCountLabel;
    private readonly ISleekButton[] _rows = new ISleekButton[VisibleRowCount];
    private readonly List<PaletteAction> _actions = [];
    private readonly List<PaletteAction> _filteredActions = [];

    private bool _isOpen;
    private int _selectedIndex;
    private int _firstVisibleIndex;

    /// <summary>
    /// Gets whether the command palette is currently visible.
    /// </summary>
    public static bool IsOpen => _instance?._isOpen == true;

    /// <summary>
    /// Creates the command palette UI and its prototype action catalog.
    /// </summary>
    public CommandPaletteExtension()
    {
        UIBuilder builder = new(0f, 0f);
        builder.SetScaleHorizontal(1f).SetScaleVertical(1f);
        _root = builder.BuildFullscreenBox();
        _root.IsVisible = false;

        builder.ResetProperties()
            .SetScaleHorizontal(1f)
            .SetScaleVertical(1f);
        ISleekBox backdrop = builder.BuildColoredBox(new SleekColor(ESleekTint.BACKGROUND, 0.75f));
        _root.AddChild(backdrop);

        builder.ResetProperties()
            .SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(0.16f)
            .SetOffsetHorizontal(-340f)
            .SetSizeHorizontal(680f)
            .SetSizeVertical(430f);
        ISleekBox panel = builder.BuildColoredBox(new SleekColor(ESleekTint.BACKGROUND, 0.98f));
        _root.AddChild(panel);

        builder.ResetProperties()
            .SetOffsetHorizontal(20f)
            .SetOffsetVertical(14f)
            .SetSizeHorizontal(300f)
            .SetSizeVertical(30f)
            .SetText("COMMAND PALETTE");
        ISleekLabel title = builder.BuildLabel(TextAnchor.MiddleLeft, ESleekFontSize.Medium);
        panel.AddChild(title);

        builder.ResetProperties()
            .SetOffsetHorizontal(430f)
            .SetOffsetVertical(14f)
            .SetSizeHorizontal(230f)
            .SetSizeVertical(30f);
        _resultCountLabel = builder.BuildLabel(TextAnchor.MiddleRight);
        panel.AddChild(_resultCountLabel);

        builder.ResetProperties()
            .SetOffsetHorizontal(20f)
            .SetOffsetVertical(52f)
            .SetSizeHorizontal(640f)
            .SetSizeVertical(42f)
            .SetText("Search actions, tabs, and tools...");
        _searchField = builder.BuildStringField();
        panel.AddChild(_searchField);

        for (int rowIndex = 0; rowIndex < _rows.Length; rowIndex++)
        {
            builder.ResetProperties()
                .SetOffsetHorizontal(20f)
                .SetOffsetVertical(106f + rowIndex * 40f)
                .SetSizeHorizontal(640f)
                .SetSizeVertical(34f);

            ISleekButton row = builder.BuildButton();
            int capturedRowIndex = rowIndex;
            row.OnClicked += _ => ExecuteVisibleRow(capturedRowIndex);
            panel.AddChild(row);
            _rows[rowIndex] = row;
        }

        builder.ResetProperties()
            .SetOffsetHorizontal(20f)
            .SetOffsetVertical(392f)
            .SetSizeHorizontal(640f)
            .SetSizeVertical(24f)
            .SetText("Up/Down navigate    Enter run    Esc close");
        ISleekLabel footer = builder.BuildLabel(TextAnchor.MiddleLeft);
        panel.AddChild(footer);

        BuildActionCatalog();
        DiagnosticsDebug.Assert(_actions.Count == 22 && _actions.Exists(action => action.Matches("navmesh")));
        FilterActions(string.Empty);
        Initialize();
    }

    /// <summary>
    /// Attaches the palette to the editor and subscribes to search changes.
    /// </summary>
    public void Initialize()
    {
        _instance = this;
        _searchField.OnTextChanged += OnSearchTextChanged;
        EditorUI.window.AddChild(_root);
    }

    /// <summary>
    /// Handles the palette keybind and keyboard navigation once per editor update.
    /// </summary>
    public void CustomUpdate()
    {
        if (!_isOpen)
        {
            if (Glazier.Get().ShouldGameProcessInput && KeybindManager.IsDown(KeybindIds.CommandPalette))
            {
                Open();
            }

            return;
        }

        if (KeybindManager.IsDown(KeybindIds.CommandPalette))
        {
            Close();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveSelection(1);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveSelection(-1);
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ExecuteSelected();
        }
    }

    /// <summary>
    /// Closes the active palette, if one exists.
    /// </summary>
    public static void CloseIfOpen()
    {
        _instance?.Close();
    }

    private void BuildActionCatalog()
    {
        AddAction("Save Level", "Editor", "write persist", SDG.Unturned.Level.save);
        AddAction("Undo", "Editor", "transaction history", () => { DevkitTransactionManager.undo(); });
        AddAction("Redo", "Editor", "transaction history", () => { DevkitTransactionManager.redo(); });

        AddAction("Terrain: Heights", "Terrain", "height sculpt", () => OpenTerrain(() => _terrainUI!.GoToHeightsTab()));
        AddAction("Terrain: Materials", "Terrain", "paint splatmap", () => OpenTerrain(() => _terrainUI!.GoToMaterialsTab()));
        AddAction("Terrain: Foliage", "Terrain", "details resources", () => OpenTerrain(() => _terrainUI!.GoToFoliageTab()));
        AddAction("Foliage: Grid View", "Terrain", "foliage details exact icons", OpenExactFoliageGrid);
        AddAction("Terrain: Tiles", "Terrain", "landscape layers", () => OpenTerrain(() => _terrainUI!.GoToTilesTab()));

        AddAction("Environment: Lighting", "Environment", "ambience sky weather", () => OpenEnvironment(() => EditorEnvironmentUI.onClickedLightingButton(null)));
        AddAction("Environment: Roads", "Environment", "street path", () => OpenEnvironment(() => EditorEnvironmentUI.onClickedRoadsButton(null)));
        AddAction("Environment: Navigation", "Environment", "zombie navmesh", () => OpenEnvironment(() => EditorEnvironmentUI.onClickedNavigationButton(null)));
        AddAction("Environment: Nodes", "Environment", "location effect", () => OpenEnvironment(() => EditorEnvironmentUI.onClickedNodesButton(null)));

        AddAction("Spawns: Animals", "Spawns", "fauna", () => OpenSpawns(() => EditorSpawnsUI.onClickedAnimalsButton(null)));
        AddAction("Spawns: Items", "Spawns", "loot jars", () => OpenSpawns(() => EditorSpawnsUI.onClickItemsButton(null)));
        AddAction("Spawns: Zombies", "Spawns", "infected", () => OpenSpawns(() => EditorSpawnsUI.onClickedZombiesButton(null)));
        AddAction("Spawns: Vehicles", "Spawns", "cars", () => OpenSpawns(() => EditorSpawnsUI.onClickedVehiclesButton(null)));

        AddAction("Level: Objects", "Level", "assets placement", () => OpenLevel(() => _levelUI?.onClickedObjectsButton(null)));
        AddAction("Objects: Grid View", "Level", "objects assets icons", OpenObjectGrid);
        AddAction("Objects: Schematics", "Level", "objects templates blueprints", OpenObjectSchematics);
        AddAction("Level: Visibility", "Level", "regions performance", () => OpenLevel(() => _levelUI?.onClickedVisibilityButton(null)));
        AddAction("Level: Players", "Level", "barricades structures", () => OpenLevel(() => _levelUI?.onClickedPlayersButton(null)));
        AddAction("Level: Volumes", "Level", "deadzone clip water", () => OpenLevel(() => _levelUI?.OnClickedVolumesButton(null)));
    }

    private void AddAction(string title, string category, string keywords, Action execute)
    {
        _actions.Add(new PaletteAction(title, category, keywords, execute));
    }

    private void OpenTerrain(Action? openSubtab)
    {
        if (_terrainUI == null || openSubtab == null) return;
        _terrainUI.open();
        EditorEnvironmentUI.close();
        EditorSpawnsUI.close();
        EditorLevelUI.close();
        openSubtab();
    }

    private void OpenEnvironment(Action openSubtab)
    {
        _terrainUI?.close();
        EditorEnvironmentUI.open();
        EditorSpawnsUI.close();
        EditorLevelUI.close();
        openSubtab();
    }

    private void OpenSpawns(Action openSubtab)
    {
        _terrainUI?.close();
        EditorEnvironmentUI.close();
        EditorSpawnsUI.open();
        EditorLevelUI.close();
        openSubtab();
    }

    private void OpenLevel(Action openSubtab)
    {
        _terrainUI?.close();
        EditorEnvironmentUI.close();
        EditorSpawnsUI.close();
        EditorLevelUI.open();
        openSubtab();
    }

    private void OpenObjectGrid()
    {
        OpenLevel(() => _levelUI?.onClickedObjectsButton(null));
        if (ExtensionManager.TryGetInstance(out IconsExtension? iconsExtension))
        {
            iconsExtension.ShowIconGridContainer();
        }
    }

    private void OpenObjectSchematics()
    {
        OpenLevel(() => _levelUI?.onClickedObjectsButton(null));
        if (ExtensionManager.TryGetInstance(out SchematicsExtension? schematicsExtension))
        {
            schematicsExtension.ShowSchematicsContainer();
        }
    }

    private void OpenExactFoliageGrid()
    {
        OpenTerrain(() => _terrainUI!.GoToFoliageTab());
        if (ExtensionManager.TryGetInstance(out FoliageIconsExtension? foliageIconsExtension))
        {
            foliageIconsExtension.ShowExactIconGrid();
        }
    }

    private void Open()
    {
        _isOpen = true;
        _root.IsVisible = true;
        _searchField.Text = string.Empty;
        FilterActions(string.Empty);
        _searchField.FocusControl();
    }

    private void Close()
    {
        _isOpen = false;
        _searchField.ClearFocus();
        _root.IsVisible = false;
    }

    private void OnSearchTextChanged(ISleekField field, string text)
    {
        FilterActions(text);
    }

    private void FilterActions(string query)
    {
        _filteredActions.Clear();
        string trimmedQuery = query.Trim();

        foreach (PaletteAction action in _actions)
        {
            if (trimmedQuery.Length == 0 || action.Matches(trimmedQuery))
            {
                _filteredActions.Add(action);
            }
        }

        _selectedIndex = 0;
        _firstVisibleIndex = 0;
        RefreshRows();
    }

    private void MoveSelection(int direction)
    {
        if (_filteredActions.Count == 0) return;

        _selectedIndex = (_selectedIndex + direction + _filteredActions.Count) % _filteredActions.Count;
        if (_selectedIndex < _firstVisibleIndex)
        {
            _firstVisibleIndex = _selectedIndex;
        }
        else if (_selectedIndex >= _firstVisibleIndex + VisibleRowCount)
        {
            _firstVisibleIndex = _selectedIndex - VisibleRowCount + 1;
        }

        RefreshRows();
    }

    private void RefreshRows()
    {
        _resultCountLabel.Text = $"{_filteredActions.Count} action{(_filteredActions.Count == 1 ? string.Empty : "s")}";

        for (int rowIndex = 0; rowIndex < _rows.Length; rowIndex++)
        {
            ISleekButton row = _rows[rowIndex];
            int actionIndex = _firstVisibleIndex + rowIndex;
            bool hasAction = actionIndex < _filteredActions.Count;
            row.IsVisible = hasAction;
            if (!hasAction) continue;

            PaletteAction action = _filteredActions[actionIndex];
            row.Text = $"[{action.Category.ToUpperInvariant()}]  {action.Title}";
            row.TooltipText = action.Keywords;
            row.BackgroundColor = new SleekColor(ESleekTint.BACKGROUND, actionIndex == _selectedIndex ? 0.9f : 0.45f);
        }
    }

    private void ExecuteVisibleRow(int rowIndex)
    {
        int actionIndex = _firstVisibleIndex + rowIndex;
        if (actionIndex >= _filteredActions.Count) return;
        Execute(_filteredActions[actionIndex]);
    }

    private void ExecuteSelected()
    {
        if (_selectedIndex >= _filteredActions.Count) return;
        Execute(_filteredActions[_selectedIndex]);
    }

    private void Execute(PaletteAction action)
    {
        Close();
        action.Execute();
    }

    /// <summary>
    /// Detaches the palette UI and input handlers.
    /// </summary>
    public void Dispose()
    {
        Close();
        _searchField.OnTextChanged -= OnSearchTextChanged;
        EditorUI.window.RemoveChild(_root);
        if (ReferenceEquals(_instance, this)) _instance = null;
    }

    private sealed class PaletteAction(string title, string category, string keywords, Action execute)
    {
        public string Title { get; } = title;
        public string Category { get; } = category;
        public string Keywords { get; } = keywords;

        public bool Matches(string query)
        {
            return Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                   || Category.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                   || Keywords.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public void Execute()
        {
            execute();
        }
    }
}