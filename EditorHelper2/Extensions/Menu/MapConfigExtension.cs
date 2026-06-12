using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Extensions;
using EditorHelper2.Patches.UI;
using EditorHelper2.UI.Builders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Menu;

[UIExtension(typeof(MenuWorkshopEditorUI))]
[EHExtension("Map Config Editor", "Senior S")]
public class MapConfigExtension : UIExtension, IExtension
{
    private const float HeaderHeight = 40f;
    private const float RowHeight = 34f;
    private const float SectionButtonWidth = 128f;
    private const float BottomButtonY = -50f;
    private const float WarningHeight = 34f;

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        Formatting = Formatting.Indented,
        ObjectCreationHandling = ObjectCreationHandling.Replace
    };

    private static readonly FieldInfo? MenuSelectedLevelField = typeof(MenuWorkshopEditorUI)
        .GetField("selectedLevel", BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly HashSet<string> IgnoredFields = new(StringComparer.Ordinal)
    {
        nameof(LevelInfoConfigData.Hash),
        nameof(LevelInfoConfigData.PackedVersion)
    };

    private static readonly HashSet<string> SpecialFields = new(StringComparer.Ordinal)
    {
        nameof(LevelInfoConfigData.Creators),
        nameof(LevelInfoConfigData.Collaborators),
        nameof(LevelInfoConfigData.Thanks),
        nameof(LevelInfoConfigData.CustomCredits),
        nameof(LevelInfoConfigData.Trains),
        nameof(LevelInfoConfigData.Mode_Config_Overrides),
        nameof(LevelInfoConfigData.EasyDifficulty_Config_Overrides),
        nameof(LevelInfoConfigData.NormalDifficulty_Config_Overrides),
        nameof(LevelInfoConfigData.HardDifficulty_Config_Overrides),
        nameof(LevelInfoConfigData.Arena_Loadouts),
        nameof(LevelInfoConfigData.Spawn_Loadouts),
        nameof(LevelInfoConfigData.RequiredWorkshopFileIds)
    };

    private static readonly string[] Sections =
    [
        "General",
        "World",
        "Player UI",
        "Credits",
        "Trains",
        "Overrides",
        "Workshop",
        "Loadouts"
    ];

    private sealed class FieldMetadata
    {
        public FieldMetadata(string section, string hint)
        {
            Section = section;
            Hint = hint;
        }

        public string Section { get; }

        public string Hint { get; }
    }

    private static readonly Dictionary<string, FieldMetadata> FieldMetadataByName = new(StringComparer.Ordinal)
    {
        [nameof(LevelInfoConfigData.Creators)] = new("Credits", "Shown under Creators in the play menu and loading screen credits. Format: Name, Name."),
        [nameof(LevelInfoConfigData.Collaborators)] = new("Credits", "Shown under Collaborators in the play menu and loading screen credits. Format: Name, Name."),
        [nameof(LevelInfoConfigData.Thanks)] = new("Credits", "Shown under Thanks in the play menu and loading screen credits. Format: Name, Name."),
        [nameof(LevelInfoConfigData.CustomCredits)] = new("Credits", "Additional credit groups. Group keys are localized from the map localization file. Format: Group_Key: Name, Name; Other_Key: Name."),

        [nameof(LevelInfoConfigData.Item)] = new("Workshop", "Steam inventory item DefID featured on the map credits panel. Use 0 to disable."),
        [nameof(LevelInfoConfigData.Associated_Stockpile_Items)] = new("Workshop", "Extra Steam inventory item DefIDs associated with this map. One item is selected at random for the credits panel. Format: 123, 456."),
        [nameof(LevelInfoConfigData.Feedback)] = new("Workshop", "Feedback/discussion URL for the map. Workshop maps fall back to their Steam discussions when this is empty."),
        [nameof(LevelInfoConfigData.Asset)] = new("Workshop", "LevelAsset GUID used for map-level behavior such as static tags, background/loadout overrides, and other asset-driven settings. Empty uses the default LevelAsset."),
        [nameof(LevelInfoConfigData.RequiredWorkshopFileIds)] = new("Workshop", "Required Steam Workshop dependency file IDs. The play menu checks these and warns when dependencies are missing. Format: 1234567890, 9876543210."),

        [nameof(LevelInfoConfigData.Trains)] = new("Trains", "Train vehicle spawns. VehicleID must be unique on the map, RoadIndex is the road path index, and placements are normalized 0 to 1 along the path. Format: VehicleID, RoadIndex, MinPlacement, MaxPlacement; ..."),

        [nameof(LevelInfoConfigData.Mode_Config_Overrides)] = new("Overrides", "Global gameplay mode config overrides applied when the map runs. Keys are ModeConfigData field paths, for example Gameplay.Allow_Shoulder_Camera. Values can be bool, float, or uint. Server config overrides take priority."),
        [nameof(LevelInfoConfigData.EasyDifficulty_Config_Overrides)] = new("Overrides", "Gameplay mode config overrides applied only in Easy after the global map overrides. JSON object with ModeConfigData field paths."),
        [nameof(LevelInfoConfigData.NormalDifficulty_Config_Overrides)] = new("Overrides", "Gameplay mode config overrides applied only in Normal after the global map overrides. JSON object with ModeConfigData field paths."),
        [nameof(LevelInfoConfigData.HardDifficulty_Config_Overrides)] = new("Overrides", "Gameplay mode config overrides applied only in Hard after the global map overrides. JSON object with ModeConfigData field paths."),

        [nameof(LevelInfoConfigData.Allow_Underwater_Features)] = new("World", "Allows underwater feature logic on maps using legacy water below normal sea level."),
        [nameof(LevelInfoConfigData.Terrain_Snow_Sparkle)] = new("World", "Enables glitter/sparkle rendering on snowy terrain when the client graphics setting allows it."),
        [nameof(LevelInfoConfigData.Use_Legacy_Clip_Borders)] = new("World", "When true, safe-position checks use the old rectangular level border. When false, player clip volumes define invalid areas."),
        [nameof(LevelInfoConfigData.Use_Legacy_Ground)] = new("World", "Keeps old ground/terrain behavior for compatibility. Disable only after verifying the map with newer ground handling."),
        [nameof(LevelInfoConfigData.Use_Legacy_Water)] = new("World", "Keeps old sea-level water behavior. Disable for newer water/lighting behavior after verifying the map."),
        [nameof(LevelInfoConfigData.Use_Vanilla_Bubbles)] = new("World", "Controls whether default underwater bubble particle effects are active."),
        [nameof(LevelInfoConfigData.Use_Underground_Whitelist)] = new("World", "When enabled, underground positions are clamped above ground unless allowed by underground volumes."),
        [nameof(LevelInfoConfigData.Use_Legacy_Snow_Height)] = new("World", "Uses legacy snow-height checks instead of snow volumes."),
        [nameof(LevelInfoConfigData.Use_Legacy_Fog_Height)] = new("World", "Legacy field kept for compatibility. No current runtime reads were found in the latest client source."),
        [nameof(LevelInfoConfigData.Use_Legacy_Oxygen_Height)] = new("World", "Uses legacy oxygen-height checks for breathing/oxygen behavior."),
        [nameof(LevelInfoConfigData.Use_Rain_Volumes)] = new("World", "Uses rain volumes for weather exposure instead of only global rain state."),
        [nameof(LevelInfoConfigData.Use_Snow_Volumes)] = new("World", "Uses snow volumes for player/vehicle snow state and lighting weather checks."),
        [nameof(LevelInfoConfigData.Is_Aurora_Borealis_Visible)] = new("World", "Shows the aurora borealis sky effect on this map."),
        [nameof(LevelInfoConfigData.Snow_Affects_Temperature)] = new("World", "When true, being in snow affects player temperature."),
        [nameof(LevelInfoConfigData.Weather_Override)] = new("World", "Forces a constant weather mode for the map: NONE, RAIN, or SNOW."),
        [nameof(LevelInfoConfigData.Has_Atmosphere)] = new("World", "Controls atmosphere/sky rendering. Disable for maps that should not render normal atmospheric sky effects."),
        [nameof(LevelInfoConfigData.Allow_Crafting)] = new("World", "Controls whether players can open and use crafting on this map."),
        [nameof(LevelInfoConfigData.Allow_Skills)] = new("World", "Controls whether players can use skills and the skills dashboard on this map."),
        [nameof(LevelInfoConfigData.Allow_Information)] = new("World", "Controls whether map/quests/players information screens can open on this map."),
        [nameof(LevelInfoConfigData.Allow_Holiday_Redirects)] = new("World", "Allows active-holiday object redirects while playing the map. Ignored in the editor."),
        [nameof(LevelInfoConfigData.Has_Global_Electricity)] = new("World", "Electric objects are always powered and generators have no effect."),
        [nameof(LevelInfoConfigData.Gravity)] = new("World", "Y-axis physics gravity applied when the level loads. Default is -9.81."),
        [nameof(LevelInfoConfigData.Blimp_Altitude)] = new("World", "Surface elevation used by blimp buoyancy. Default is 150."),
        [nameof(LevelInfoConfigData.Max_Walkable_Slope)] = new("World", "Overrides player walkable slope when greater than -0.5. Default -1 uses normal movement settings."),
        [nameof(LevelInfoConfigData.Prevent_Building_Near_Spawnpoint_Radius)] = new("World", "Radius around player spawnpoints where building is prevented. Default is 16."),

        [nameof(LevelInfoConfigData.Category)] = new("General", "Singleplayer play-menu category for the map: OFFICIAL, CURATED, WORKSHOP, MISC, ALL, or EDITABLE."),
        [nameof(LevelInfoConfigData.Version)] = new("General", "Display version shown in menus/loading text. Expected format is a.b.c.d."),
        [nameof(LevelInfoConfigData.Tips)] = new("General", "Number of custom loading tips in the map localization file. Keys are Tip_0 through Tip_(Tips - 1)."),
        [nameof(LevelInfoConfigData.Batching_Version)] = new("General", "Enables LevelBatching outside editor when greater than 1. Only raise after verifying batching works on the map."),
        [nameof(LevelInfoConfigData.Batching_Max_Texture_Size)] = new("General", "Maximum source texture size included in the LevelBatching atlas. Keep the combined atlas at a reasonable size."),
        [nameof(LevelInfoConfigData.Enable_Clutter_Option)] = new("General", "Allows the client clutter graphics option to skip clutter instantiation after the creator verifies it works."),
        [nameof(LevelInfoConfigData.Enable_Static_Volumes)] = new("General", "Enables static volume initialization outside the editor. Use only when volumes are placed in the level editor, not Unity prefabs."),
        [nameof(LevelInfoConfigData.Use_Arena_Compactor)] = new("General", "Arena mode uses ArenaCompactorVolume data when enabled. Disable to use the full world radius instead."),

        [nameof(LevelInfoConfigData.PlayerUI_HealthVisible)] = new("Player UI", "Shows or hides the health HUD element on this map."),
        [nameof(LevelInfoConfigData.PlayerUI_FoodVisible)] = new("Player UI", "Shows or hides the food HUD element on this map."),
        [nameof(LevelInfoConfigData.PlayerUI_WaterVisible)] = new("Player UI", "Shows or hides the water HUD element on this map."),
        [nameof(LevelInfoConfigData.PlayerUI_VirusVisible)] = new("Player UI", "Shows or hides the immunity/virus HUD element on this map."),
        [nameof(LevelInfoConfigData.PlayerUI_StaminaVisible)] = new("Player UI", "Shows or hides the stamina HUD element on this map."),
        [nameof(LevelInfoConfigData.PlayerUI_OxygenVisible)] = new("Player UI", "Shows or hides the oxygen HUD element on this map."),
        [nameof(LevelInfoConfigData.PlayerUI_GunVisible)] = new("Player UI", "Shows or hides the gun/ammo HUD element on this map."),

        [nameof(LevelInfoConfigData.Arena_Loadouts)] = new("Loadouts", "Arena item spawn table grants. Each entry resolves Table_ID as an item spawn table and grants Amount rolls to arena players. Format: TableID, Amount; ..."),
        [nameof(LevelInfoConfigData.Spawn_Loadouts)] = new("Loadouts", "Item spawn table grants for players when they spawn. Each entry resolves Table_ID as an item spawn table and grants Amount rolls. Format: TableID, Amount; ...")
    };

    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;

    private readonly ISleekButton _openButton;
    private readonly SleekFullscreenBox _root;
    private readonly SleekFullscreenBox _panel;
    private readonly ISleekBox _background;
    private readonly ISleekBox _headerBox;
    private readonly ISleekLabel _pathLabel;
    private readonly ISleekLabel _disclaimerLabel;
    private readonly ISleekLabel _statusLabel;
    private readonly ISleekScrollView _scrollView;
    private readonly ISleekButton _saveButton;
    private readonly ISleekButton _reloadButton;
    private readonly ISleekButton _defaultsButton;
    private readonly ISleekButton _closeButton;

    private readonly List<ISleekElement> _sectionButtons = [];
    private readonly List<ISleekElement> _fieldRows = [];
    private readonly Dictionary<ISleekElement, bool> _hiddenElements = new();

    private LevelInfoConfigData _configData = new();
    private string _selectedSection = "General";
    private bool _active;
    private bool _isDirty;
    private bool _isRebuilding;
    private LevelInfo? _targetLevel;

    public MapConfigExtension()
    {
        UIBuilder builder = new(200f, 30f);

        builder.SetAnchorHorizontal(0.5f)
            .SetOffsetHorizontal(-305f)
            .SetOffsetVertical(570f)
            .SetText("Map Config");
        _openButton = builder.BuildButton("Edit the selected map's Config.json");

        builder.ResetProperties()
            .SetScaleHorizontal(1f)
            .SetScaleVertical(1f);
        _root = builder.BuildFullscreenBox();

        builder.ResetProperties()
            .SetOffsetHorizontal(10f)
            .SetOffsetVertical(10f)
            .SetSizeHorizontal(-20f)
            .SetSizeVertical(-20f)
            .SetScaleHorizontal(1f)
            .SetScaleVertical(1f);
        _panel = builder.BuildFullscreenBox();

        builder.ResetProperties()
            .SetScaleHorizontal(1f)
            .SetScaleVertical(1f);
        _background = builder.BuildColoredBox(new SleekColor(ESleekTint.BACKGROUND, 0.88f));

        builder.ResetProperties()
            .SetSizeVertical(HeaderHeight)
            .SetScaleHorizontal(1f)
            .SetText("Map Config");
        _headerBox = builder.BuildBox();

        builder.ResetProperties()
            .SetOffsetHorizontal(14f)
            .SetOffsetVertical(42f)
            .SetSizeHorizontal(-28f)
            .SetSizeVertical(24f)
            .SetScaleHorizontal(1f);
        _pathLabel = builder.BuildLabel(TextAnchor.MiddleLeft, ESleekFontSize.Small);

        builder.ResetProperties()
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(14f)
            .SetOffsetVertical(-92f)
            .SetSizeHorizontal(-28f)
            .SetSizeVertical(WarningHeight)
            .SetScaleHorizontal(1f)
            .SetText("Warning: internal Unturned map options. Change at your own responsibility; invalid values can break loading or gameplay.");
        _disclaimerLabel = builder.BuildLabel(TextAnchor.MiddleCenter, ESleekFontSize.Small);

        builder.ResetProperties()
            .SetOffsetHorizontal(14f)
            .SetOffsetVertical(112f)
            .SetSizeHorizontal(-28f)
            .SetSizeVertical(-218f)
            .SetScaleHorizontal(1f)
            .SetScaleVertical(1f);
        _scrollView = builder.BuildScrollView(scaleContentToWidth: true);

        builder.ResetProperties()
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(14f)
            .SetOffsetVertical(BottomButtonY)
            .SetSizeHorizontal(130f)
            .SetSizeVertical(40f)
            .SetText("Save");
        _saveButton = builder.BuildButton("Write changes to the current map's Config.json", ESleekFontSize.Medium);

        builder.ResetProperties()
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(154f)
            .SetOffsetVertical(BottomButtonY)
            .SetSizeHorizontal(130f)
            .SetSizeVertical(40f)
            .SetText("Reload");
        _reloadButton = builder.BuildButton("Reload values from Config.json", ESleekFontSize.Medium);

        builder.ResetProperties()
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(294f)
            .SetOffsetVertical(BottomButtonY)
            .SetSizeHorizontal(160f)
            .SetSizeVertical(40f)
            .SetText("Reset Section");
        _defaultsButton = builder.BuildButton("Reset the current section to Unturned defaults", ESleekFontSize.Medium);

        builder.ResetProperties()
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(474f)
            .SetOffsetVertical(BottomButtonY)
            .SetSizeHorizontal(520f)
            .SetSizeVertical(40f);
        _statusLabel = builder.BuildLabel(TextAnchor.MiddleLeft, ESleekFontSize.Small);

        builder.ResetProperties()
            .SetAnchorHorizontal(1f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(-170f)
            .SetOffsetVertical(BottomButtonY)
            .SetSizeHorizontal(160f)
            .SetSizeVertical(40f)
            .SetText("Back");
        _closeButton = builder.BuildButton("Close map config editor", ESleekFontSize.Medium);

        Initialize();
    }

    public void Initialize()
    {
        if (_container == null)
        {
            return;
        }

        _container.AddChild(_openButton);
        MenuUI.container.AddChild(_root);
        _root.AddChild(_panel);
        _panel.AddChild(_background);
        _panel.AddChild(_headerBox);
        _panel.AddChild(_pathLabel);
        _panel.AddChild(_disclaimerLabel);
        _panel.AddChild(_scrollView);
        _panel.AddChild(_saveButton);
        _panel.AddChild(_reloadButton);
        _panel.AddChild(_defaultsButton);
        _panel.AddChild(_statusLabel);
        _panel.AddChild(_closeButton);

        CreateSectionButtons();
        _root.AnimateOutOfView(1f, 0f);

        _openButton.OnClicked += OnOpenClicked;
        _saveButton.OnClicked += OnSaveClicked;
        _reloadButton.OnClicked += OnReloadClicked;
        _defaultsButton.OnClicked += OnDefaultsClicked;
        _closeButton.OnClicked += OnCloseClicked;
        MenuUIPatches.OnEscapePressedBefore += OnEscapePressedBefore;
    }

    private void Open()
    {
        if (_active)
        {
            return;
        }

        _targetLevel = GetSelectedMenuLevel();
        if (_targetLevel == null || !_targetLevel.isEditable)
        {
            SetStatus("Select an editable map first.");
            return;
        }

        _active = true;
        LoadConfig();
        Rebuild();
        HideOtherMenuUI();
        _root.AnimateIntoView();
    }

    private void Close()
    {
        if (!_active)
        {
            return;
        }

        _active = false;
        _root.AnimateOutOfView(1f, 0f);
        RestoreOtherMenuUI();
    }

    private void LoadConfig()
    {
        _configData = CloneConfig(_targetLevel?.configData) ?? new LevelInfoConfigData();
        _isDirty = false;

        try
        {
            string path = ConfigPath;
            if (File.Exists(path))
            {
                JsonConvert.PopulateObject(File.ReadAllText(path), _configData, JsonSettings);
                SetStatus("Loaded Config.json.");
            }
            else
            {
                SetStatus("Config.json will be created on save.");
            }
        }
        catch (Exception ex)
        {
            UnturnedLog.exception(ex, "[EditorHelper2] Failed to load map Config.json:");
            SetStatus("Failed to load Config.json. Defaults are shown.");
        }

        _pathLabel.Text = ConfigPath;
        UpdateSaveButtonText();
    }

    private void SaveConfig()
    {
        try
        {
            string path = ConfigPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? CurrentMapPath);
            File.WriteAllText(path, JsonConvert.SerializeObject(_configData, Formatting.Indented));
            ApplyToCurrentLevelInfo();
            _isDirty = false;
            UpdateSaveButtonText();
            SetStatus("Saved Config.json.");
        }
        catch (Exception ex)
        {
            UnturnedLog.exception(ex, "[EditorHelper2] Failed to save map Config.json:");
            SetStatus("Failed to save Config.json.");
        }
    }

    private void ApplyToCurrentLevelInfo()
    {
        ApplyConfigToLevelInfo(_targetLevel);

        if (SDG.Unturned.Level.info != null
            && _targetLevel != null
            && string.Equals(SDG.Unturned.Level.info.path, _targetLevel.path, StringComparison.OrdinalIgnoreCase))
        {
            ApplyConfigToLevelInfo(SDG.Unturned.Level.info);
        }
    }

    private void ApplyConfigToLevelInfo(LevelInfo? levelInfo)
    {
        if (levelInfo == null)
        {
            return;
        }

        try
        {
            PropertyInfo? property = typeof(LevelInfo)
                .GetProperty(nameof(LevelInfo.configData), BindingFlags.Instance | BindingFlags.Public);
            property?.GetSetMethod(nonPublic: true)?.Invoke(levelInfo, [_configData]);
        }
        catch (Exception ex)
        {
            UnturnedLog.exception(ex, "[EditorHelper2] Failed to apply map config to current level info:");
        }
    }

    private void ResetSelectedSectionToDefaults()
    {
        LevelInfoConfigData defaults = new();

        foreach (FieldInfo field in GetSectionFields(_selectedSection))
        {
            field.SetValue(_configData, CloneValue(field.GetValue(defaults)));
        }

        SetStatus($"Reset {_selectedSection} to defaults.");
    }

    private void Rebuild()
    {
        _isRebuilding = true;
        _scrollView.RemoveAllChildren();
        _fieldRows.Clear();
        RebuildSectionButtons();

        float offsetY = 0f;
        foreach (FieldInfo field in GetSectionFields(_selectedSection))
        {
            AddFieldRow(field, offsetY);
            offsetY += RowHeight;
        }

        _scrollView.ContentSizeOffset = new Vector2(0f, offsetY);
        _isRebuilding = false;
    }

    private void AddFieldRow(FieldInfo field, float offsetY)
    {
        UIBuilder builder = new(0f, RowHeight);
        builder.SetOffsetHorizontal(8f)
            .SetOffsetVertical(offsetY)
            .SetSizeHorizontal(-360f)
            .SetScaleHorizontal(1f)
            .SetText(Humanize(field.Name));

        ISleekLabel label = builder.BuildLabel(TextAnchor.MiddleLeft);
        _scrollView.AddChild(label);
        _fieldRows.Add(label);

        object? value = field.GetValue(_configData);
        object? defaultValue = field.GetValue(new LevelInfoConfigData());

        if (field.FieldType == typeof(bool))
        {
            AddToggleField(field, value is true, defaultValue, offsetY);
            return;
        }

        if (field.FieldType.IsEnum)
        {
            AddEnumField(field, value, defaultValue, offsetY);
            return;
        }

        AddTextField(field, value, defaultValue, offsetY);
    }

    private void AddToggleField(FieldInfo field, bool value, object? defaultValue, float offsetY)
    {
        ISleekToggle toggle = Glazier.Get().CreateToggle();
        toggle.PositionOffset_X = -340f;
        toggle.PositionOffset_Y = offsetY - 3f;
        toggle.PositionScale_X = 1f;
        toggle.SizeOffset_X = 40f;
        toggle.SizeOffset_Y = 40f;
        toggle.Value = value;
        toggle.ForegroundColor = ESleekTint.FOREGROUND;
        toggle.TooltipText = BuildFieldTooltip(field, defaultValue);
        toggle.OnValueChanged += (_, state) =>
        {
            if (_isRebuilding)
            {
                return;
            }

            field.SetValue(_configData, state);
            MarkDirty();
        };

        _scrollView.AddChild(toggle);
        _fieldRows.Add(toggle);
    }

    private void AddEnumField(FieldInfo field, object? value, object? defaultValue, float offsetY)
    {
        Array enumValues = Enum.GetValues(field.FieldType);
        GUIContent[] states = new GUIContent[enumValues.Length];
        for (int i = 0; i < enumValues.Length; i++)
        {
            object enumValue = enumValues.GetValue(i);
            states[i] = new GUIContent(Enum.GetName(field.FieldType, enumValue) ?? enumValue.ToString());
        }

        SleekButtonState stateButton = new(states);
        stateButton.PositionOffset_X = -340f;
        stateButton.PositionOffset_Y = offsetY + 2f;
        stateButton.PositionScale_X = 1f;
        stateButton.SizeOffset_X = 330f;
        stateButton.SizeOffset_Y = 28f;
        stateButton.tooltip = BuildFieldTooltip(field, defaultValue);
        stateButton.state = GetEnumStateIndex(enumValues, value);
        stateButton.onSwappedState += (_, state) =>
        {
            if (_isRebuilding)
            {
                return;
            }

            field.SetValue(_configData, enumValues.GetValue(state));
            MarkDirty();
        };

        _scrollView.AddChild(stateButton);
        _fieldRows.Add(stateButton);
    }

    private void AddTextField(FieldInfo field, object? value, object? defaultValue, float offsetY)
    {
        UIBuilder builder = new(0f, RowHeight);
        builder.SetAnchorHorizontal(1f)
            .SetOffsetHorizontal(-340f)
            .SetOffsetVertical(offsetY + 2f)
            .SetSizeHorizontal(330f)
            .SetSizeVertical(28f)
            .SetText(FormatValue(field.FieldType, value));

        ISleekField fieldElement = builder.BuildStringField();
        fieldElement.Text = FormatValue(field.FieldType, value);
        fieldElement.PlaceholderText = FormatValue(field.FieldType, defaultValue);
        fieldElement.TooltipText = BuildFieldTooltip(field, defaultValue);
        fieldElement.OnTextChanged += (_, text) =>
        {
            if (_isRebuilding)
            {
                return;
            }

            if (!TryParseValue(field.FieldType, text, out object? parsed))
            {
                fieldElement.BackgroundColor = SleekColor.BackgroundIfLight(Color.red);
                SetStatus($"Invalid value for {field.Name}.");
                return;
            }

            fieldElement.BackgroundColor = SleekColor.BackgroundIfLight(Color.black);
            field.SetValue(_configData, parsed);
            MarkDirty();
        };

        _scrollView.AddChild(fieldElement);
        _fieldRows.Add(fieldElement);
    }

    private static int GetEnumStateIndex(Array enumValues, object? value)
    {
        if (value == null)
        {
            return 0;
        }

        for (int i = 0; i < enumValues.Length; i++)
        {
            object? enumValue = enumValues.GetValue(i);
            if (Equals(enumValue, value))
            {
                return i;
            }
        }

        return 0;
    }

    private void CreateSectionButtons()
    {
        for (int i = 0; i < Sections.Length; i++)
        {
            string section = Sections[i];
            ISleekButton button = Glazier.Get().CreateButton();
            button.PositionOffset_X = 14f + i * (SectionButtonWidth + 6f);
            button.PositionOffset_Y = 74f;
            button.SizeOffset_X = SectionButtonWidth;
            button.SizeOffset_Y = 30f;
            button.Text = section;
            button.TooltipText = WrapTooltipText(BuildSectionTooltip(section));
            button.OnClicked += _ => SelectSection(section);
            _sectionButtons.Add(button);
            _panel.AddChild(button);
        }
    }

    private void RebuildSectionButtons()
    {
        for (int i = 0; i < _sectionButtons.Count; i++)
        {
            if (_sectionButtons[i] is not ISleekButton button)
            {
                continue;
            }

            string section = Sections[i];
            button.Text = section == _selectedSection ? $"> {section}" : section;
        }
    }

    private void SelectSection(string section)
    {
        if (section == _selectedSection)
        {
            return;
        }

        _selectedSection = section;
        Rebuild();
    }

    private static IEnumerable<FieldInfo> GetSectionFields(string section)
    {
        return typeof(LevelInfoConfigData)
            .GetFields(BindingFlags.Instance | BindingFlags.Public)
            .Where(field => !IgnoredFields.Contains(field.Name))
            .Where(field => field.GetCustomAttribute<JsonIgnoreAttribute>() == null)
            .Where(field => BelongsToSection(field, section));
    }

    private static bool BelongsToSection(FieldInfo field, string section)
    {
        if (FieldMetadataByName.TryGetValue(field.Name, out FieldMetadata metadata))
        {
            return metadata.Section == section;
        }

        string name = field.Name;
        if (SpecialFields.Contains(name) || name.StartsWith("PlayerUI_", StringComparison.Ordinal))
        {
            return false;
        }

        return section == ((field.FieldType == typeof(bool) || field.FieldType == typeof(float)) ? "World" : "General");
    }

    private static bool TryParseValue(Type targetType, string text, out object? value)
    {
        Type effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        string input = text.Trim();

        try
        {
            if (effectiveType == typeof(string))
            {
                value = string.IsNullOrWhiteSpace(text) ? null : text;
                return true;
            }

            if (effectiveType == typeof(string[]))
            {
                value = ParseStringArray(input);
                return true;
            }

            if (effectiveType == typeof(int[]))
            {
                value = ParseDelimited<int>(input, int.Parse);
                return true;
            }

            if (effectiveType == typeof(ulong[]))
            {
                value = ParseDelimited<ulong>(input, ulong.Parse);
                return true;
            }

            if (effectiveType == typeof(bool))
            {
                return TryParseBool(input, out value);
            }

            if (effectiveType.IsEnum)
            {
                value = Enum.Parse(effectiveType, input, ignoreCase: true);
                return true;
            }

            if (effectiveType == typeof(AssetReference<LevelAsset>))
            {
                value = string.IsNullOrWhiteSpace(input)
                    ? AssetReference<LevelAsset>.invalid
                    : new AssetReference<LevelAsset>(Guid.Parse(input));
                return true;
            }

            if (effectiveType == typeof(List<LevelTrainAssociation>))
            {
                value = ParseTrains(input);
                return true;
            }

            if (effectiveType == typeof(List<ArenaLoadout>))
            {
                value = ParseLoadouts(input);
                return true;
            }

            if (effectiveType == typeof(Dictionary<string, string[]>))
            {
                value = ParseCustomCredits(input);
                return true;
            }

            if (typeof(System.Collections.IDictionary).IsAssignableFrom(effectiveType))
            {
                value = string.IsNullOrWhiteSpace(input)
                    ? Activator.CreateInstance(effectiveType)
                    : JsonConvert.DeserializeObject(input, effectiveType);
                return value != null;
            }

            if (typeof(System.Collections.IList).IsAssignableFrom(effectiveType) && effectiveType != typeof(string))
            {
                value = string.IsNullOrWhiteSpace(input)
                    ? Activator.CreateInstance(effectiveType)
                    : JsonConvert.DeserializeObject(input, effectiveType);
                return value != null;
            }

            value = Convert.ChangeType(input, effectiveType, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            value = null;
            return false;
        }
    }

    private static bool TryParseBool(string input, out object? value)
    {
        if (bool.TryParse(input, out bool boolValue))
        {
            value = boolValue;
            return true;
        }

        if (input == "1" || input.Equals("yes", StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }

        if (input == "0" || input.Equals("no", StringComparison.OrdinalIgnoreCase))
        {
            value = false;
            return true;
        }

        value = null;
        return false;
    }

    private static T[] ParseDelimited<T>(string input, Func<string, IFormatProvider, T> parse)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        return input
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(part => parse(part.Trim(), CultureInfo.InvariantCulture))
            .ToArray();
    }

    private static string[] ParseStringArray(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        return input
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .ToArray();
    }

    private static Dictionary<string, string[]> ParseCustomCredits(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new Dictionary<string, string[]>();
        }

        if (input.TrimStart().StartsWith("{", StringComparison.Ordinal))
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string[]>>(input)
                   ?? new Dictionary<string, string[]>();
        }

        Dictionary<string, string[]> result = new(StringComparer.Ordinal);
        foreach (string entry in input.Split(';'))
        {
            int separatorIndex = entry.IndexOf(':');
            if (separatorIndex < 1)
            {
                continue;
            }

            string group = entry[..separatorIndex].Trim();
            string[] names = ParseStringArray(entry[(separatorIndex + 1)..]);
            if (group.Length > 0)
            {
                result[group] = names;
            }
        }

        return result;
    }

    private static List<LevelTrainAssociation> ParseTrains(string input)
    {
        List<LevelTrainAssociation> trains = [];
        if (string.IsNullOrWhiteSpace(input))
        {
            return trains;
        }

        if (input.TrimStart().StartsWith("[", StringComparison.Ordinal))
        {
            return JsonConvert.DeserializeObject<List<LevelTrainAssociation>>(input) ?? trains;
        }

        foreach (string entry in input.Split(';'))
        {
            string[] parts = entry.Split(',').Select(part => part.Trim()).ToArray();
            if (parts.Length < 2)
            {
                continue;
            }

            trains.Add(new LevelTrainAssociation
            {
                VehicleID = ushort.Parse(parts[0], CultureInfo.InvariantCulture),
                RoadIndex = ushort.Parse(parts[1], CultureInfo.InvariantCulture),
                Min_Spawn_Placement = parts.Length > 2 ? float.Parse(parts[2], CultureInfo.InvariantCulture) : 0.1f,
                Max_Spawn_Placement = parts.Length > 3 ? float.Parse(parts[3], CultureInfo.InvariantCulture) : 0.9f
            });
        }

        return trains;
    }

    private static List<ArenaLoadout> ParseLoadouts(string input)
    {
        List<ArenaLoadout> loadouts = [];
        if (string.IsNullOrWhiteSpace(input))
        {
            return loadouts;
        }

        if (input.TrimStart().StartsWith("[", StringComparison.Ordinal))
        {
            return JsonConvert.DeserializeObject<List<ArenaLoadout>>(input) ?? loadouts;
        }

        foreach (string entry in input.Split(';'))
        {
            string[] parts = entry.Split(',').Select(part => part.Trim()).ToArray();
            if (parts.Length < 2)
            {
                continue;
            }

            loadouts.Add(new ArenaLoadout
            {
                Table_ID = ushort.Parse(parts[0], CultureInfo.InvariantCulture),
                Amount = ushort.Parse(parts[1], CultureInfo.InvariantCulture)
            });
        }

        return loadouts;
    }

    private static string FormatValue(Type fieldType, object? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        Type effectiveType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;
        if (effectiveType == typeof(string[]))
        {
            return string.Join(", ", (string[])value);
        }

        if (effectiveType == typeof(int[]))
        {
            return string.Join(", ", (int[])value);
        }

        if (effectiveType == typeof(ulong[]))
        {
            return string.Join(", ", (ulong[])value);
        }

        if (effectiveType == typeof(Dictionary<string, string[]>))
        {
            Dictionary<string, string[]> credits = (Dictionary<string, string[]>)value;
            return string.Join("; ", credits.Select(pair => $"{pair.Key}: {string.Join(", ", pair.Value)}"));
        }

        if (effectiveType == typeof(List<LevelTrainAssociation>))
        {
            List<LevelTrainAssociation> trains = (List<LevelTrainAssociation>)value;
            return string.Join("; ", trains.Select(train =>
                $"{train.VehicleID}, {train.RoadIndex}, {FormatFloat(train.Min_Spawn_Placement)}, {FormatFloat(train.Max_Spawn_Placement)}"));
        }

        if (effectiveType == typeof(List<ArenaLoadout>))
        {
            List<ArenaLoadout> loadouts = (List<ArenaLoadout>)value;
            return string.Join("; ", loadouts.Select(loadout => $"{loadout.Table_ID}, {loadout.Amount}"));
        }

        if (effectiveType == typeof(AssetReference<LevelAsset>))
        {
            return ((AssetReference<LevelAsset>)value).GUID.ToString("N");
        }

        if (typeof(System.Collections.IDictionary).IsAssignableFrom(effectiveType) ||
            typeof(System.Collections.IList).IsAssignableFrom(effectiveType) && effectiveType != typeof(string))
        {
            return JsonConvert.SerializeObject(value, Formatting.None);
        }

        return value switch
        {
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string FormatFloat(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string BuildFieldTooltip(FieldInfo field, object? defaultValue = null)
    {
        string tooltip = $"{Humanize(field.Name)}\nPath: Config.json/{field.Name}\nType: {FormatTypeName(field.FieldType)}";
        if (defaultValue != null)
        {
            tooltip += $"\nDefault: {FormatValue(field.FieldType, defaultValue)}";
        }

        string hint = BuildMeaningHint(field);
        if (!string.IsNullOrWhiteSpace(hint))
        {
            tooltip += $"\n{hint}";
        }

        if (field.FieldType.IsEnum)
        {
            tooltip += $"\nOptions: {string.Join(", ", Enum.GetNames(field.FieldType))}";
        }

        return WrapTooltipText(tooltip);
    }

    private static string WrapTooltipText(string text)
    {
        int maxCharacters = CalculateTooltipMaxCharacters();
        StringBuilder builder = new();
        string[] lines = text.Replace("\r\n", "\n").Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                builder.Append('\n');
            }

            AppendWrappedLine(builder, lines[i], maxCharacters);
        }

        return builder.ToString();
    }

    private static int CalculateTooltipMaxCharacters()
    {
        int layoutWidth = Mathf.Max(640, ScreenEx.GetWidthForLayout());
        float maxTooltipWidth = Mathf.Clamp(layoutWidth * 0.44f, 300f, 680f);
        return Mathf.Clamp(Mathf.FloorToInt(maxTooltipWidth / 8.5f), 36, 80);
    }

    private static void AppendWrappedLine(StringBuilder builder, string line, int maxCharacters)
    {
        if (line.Length <= maxCharacters)
        {
            builder.Append(line);
            return;
        }

        int current = 0;
        foreach (string word in line.Split(' '))
        {
            if (word.Length == 0)
            {
                continue;
            }

            if (current == 0)
            {
                AppendLongWord(builder, word, maxCharacters, ref current);
                continue;
            }

            if (current + 1 + word.Length > maxCharacters)
            {
                builder.Append('\n');
                current = 0;
                AppendLongWord(builder, word, maxCharacters, ref current);
            }
            else
            {
                builder.Append(' ');
                builder.Append(word);
                current += 1 + word.Length;
            }
        }
    }

    private static void AppendLongWord(StringBuilder builder, string word, int maxCharacters, ref int current)
    {
        if (word.Length <= maxCharacters)
        {
            builder.Append(word);
            current = word.Length;
            return;
        }

        int index = 0;
        while (index < word.Length)
        {
            int length = Math.Min(maxCharacters, word.Length - index);
            if (current > 0)
            {
                builder.Append('\n');
            }

            builder.Append(word, index, length);
            current = length;
            index += length;
        }
    }

    private static string BuildMeaningHint(FieldInfo field)
    {
        return FieldMetadataByName.TryGetValue(field.Name, out FieldMetadata metadata)
            ? metadata.Hint
            : "Latest-source field with no custom legend yet. It is still saved with Config.json.";
    }

    private static string BuildSectionTooltip(string section)
    {
        return section switch
        {
            "General" => "Map identity, version, category, physics, and batching settings.",
            "World" => "Weather, legacy compatibility, crafting, skills, water, snow, and world-rule toggles.",
            "Player UI" => "Map-specific HUD visibility toggles.",
            "Credits" => "Creators, collaborators, thanks, and custom credit groups.",
            "Trains" => "Train vehicle IDs and their road-path spawn placement.",
            "Overrides" => "Per-map and per-difficulty gameplay config overrides as JSON.",
            "Workshop" => "Workshop dependencies, stockpile item links, feedback URL, and level asset GUID.",
            "Loadouts" => "Arena and spawn loadout arrays edited as JSON.",
            _ => "Config.json settings."
        };
    }

    private static string FormatTypeName(Type type)
    {
        Type effectiveType = Nullable.GetUnderlyingType(type) ?? type;
        if (effectiveType == typeof(string[]))
        {
            return "comma-separated text list";
        }

        if (effectiveType == typeof(int[]) || effectiveType == typeof(ulong[]))
        {
            return "comma-separated number list";
        }

        if (effectiveType == typeof(Dictionary<string, string[]>))
        {
            return "credit groups";
        }

        if (effectiveType == typeof(List<LevelTrainAssociation>))
        {
            return "train list";
        }

        if (effectiveType == typeof(List<ArenaLoadout>))
        {
            return "loadout list";
        }

        if (typeof(System.Collections.IDictionary).IsAssignableFrom(effectiveType) ||
            typeof(System.Collections.IList).IsAssignableFrom(effectiveType) && effectiveType != typeof(string))
        {
            return "JSON";
        }

        return effectiveType.Name;
    }

    private static string Humanize(string value)
    {
        return value.Replace('_', ' ');
    }

    private static object? CloneValue(object? value)
    {
        if (value == null)
        {
            return null;
        }

        return JToken.FromObject(value).ToObject(value.GetType());
    }

    private void MarkDirty()
    {
        _isDirty = true;
        UpdateSaveButtonText();
        SetStatus("Unsaved changes.");
    }

    private void UpdateSaveButtonText()
    {
        _saveButton.Text = _isDirty ? "Save *" : "Save";
    }

    private void SetStatus(string text)
    {
        _statusLabel.Text = text;
    }

    private string CurrentMapPath => _targetLevel?.path ?? SDG.Unturned.Level.info.path;

    private string ConfigPath => Path.Combine(CurrentMapPath, "Config.json");

    private static LevelInfo? GetSelectedMenuLevel()
    {
        return MenuSelectedLevelField?.GetValue(null) as LevelInfo;
    }

    private static LevelInfoConfigData? CloneConfig(LevelInfoConfigData? configData)
    {
        if (configData == null)
        {
            return null;
        }

        return JsonConvert.DeserializeObject<LevelInfoConfigData>(
            JsonConvert.SerializeObject(configData, Formatting.None),
            JsonSettings);
    }

    private void HideOtherMenuUI()
    {
        _hiddenElements.Clear();
        for (int i = 0; i < MenuUI.container.GetChildCount(); i++)
        {
            HideElement(MenuUI.container.GetChildAtIndexEx(i));
        }
    }

    private void HideElement(ISleekElement? element)
    {
        if (element == null || ReferenceEquals(element, _root) || _hiddenElements.ContainsKey(element))
        {
            return;
        }

        _hiddenElements[element] = element.IsVisible;
        element.IsVisible = false;
    }

    private void RestoreOtherMenuUI()
    {
        foreach (KeyValuePair<ISleekElement, bool> pair in _hiddenElements)
        {
            pair.Key.IsVisible = pair.Value;
        }

        _hiddenElements.Clear();
    }

    private void OnOpenClicked(ISleekElement button) => Open();

    private void OnCloseClicked(ISleekElement button) => Close();

    private bool OnEscapePressedBefore()
    {
        if (!_active)
        {
            return false;
        }

        Close();
        return true;
    }

    private void OnSaveClicked(ISleekElement button) => SaveConfig();

    private void OnReloadClicked(ISleekElement button)
    {
        LoadConfig();
        Rebuild();
    }

    private void OnDefaultsClicked(ISleekElement button)
    {
        ResetSelectedSectionToDefaults();
        MarkDirty();
        Rebuild();
    }

    public void Dispose()
    {
        if (_container == null)
        {
            return;
        }

        if (_active)
        {
            _active = false;
            RestoreOtherMenuUI();
        }

        _openButton.OnClicked -= OnOpenClicked;
        _saveButton.OnClicked -= OnSaveClicked;
        _reloadButton.OnClicked -= OnReloadClicked;
        _defaultsButton.OnClicked -= OnDefaultsClicked;
        _closeButton.OnClicked -= OnCloseClicked;
        MenuUIPatches.OnEscapePressedBefore -= OnEscapePressedBefore;

        _container.RemoveChild(_openButton);
        MenuUI.container.RemoveChild(_root);
    }
}
