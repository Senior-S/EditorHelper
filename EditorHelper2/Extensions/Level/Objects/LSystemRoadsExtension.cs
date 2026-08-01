using System;
using System.Collections.Generic;
using System.Linq;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Helpers.LSystem;
using EditorHelper2.Helpers;
using EditorHelper2.Extensions.Editor.Dashboard;
using EditorHelper2.Loader;
using EditorHelper2.Patches.Editor.UI;
using EditorHelper2.UI.Builders;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Level.Objects;

[UIExtension(typeof(EditorLevelObjectsUI))]
[EHExtension("L-System Roads Extension", "Senior S")]
public class LSystemRoadsExtension : UIExtension, IExtension
{
    private const string ConfigSection = "LSystemRoads";
    private const int HardSegmentLimit = 200;
    private static LSystemRoadsExtension? _instance;

    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;

    private readonly ISleekButton _roadsPanelButton;
    private readonly ISleekBox _settingsContainer;
    private readonly SleekButtonState _sizeButton;
    private readonly ISleekInt32Field _seedField;
    private readonly ISleekInt32Field _segmentLimitField;
    private readonly ISleekInt32Field _initialLengthField;
    private readonly ISleekFloat32Field _branchChanceField;
    private readonly ISleekFloat32Field _turnChanceField;
    private readonly ISleekFloat32Field _radiusField;
    private readonly ISleekButton _straightRoadButton;
    private readonly ISleekButton _teeRoadButton;
    private readonly ISleekButton _quadRoadButton;
    private readonly ISleekButton _cornerRoadButton;
    private readonly ISleekButton _endRoadButton;
    private readonly ISleekLabel _straightRoadLabel;
    private readonly ISleekLabel _teeRoadLabel;
    private readonly ISleekLabel _quadRoadLabel;
    private readonly ISleekLabel _cornerRoadLabel;
    private readonly ISleekLabel _endRoadLabel;
    private readonly ISleekButton _resetRoadsButton;
    private readonly ISleekButton _clearGeneratedRoadsButton;
    private readonly ISleekButton _generateRoadsButton;
    private readonly ISleekLabel _statusLabel;
    private readonly List<List<Transform>> _generatedRoadBatches = new();

    private Guid _straightRoadGuid = LSystemRoadSettings.DefaultStraightRoadGuid;
    private Guid _teeRoadGuid = LSystemRoadSettings.DefaultTeeRoadGuid;
    private Guid _quadRoadGuid = LSystemRoadSettings.DefaultQuadRoadGuid;
    private Guid _cornerRoadGuid = LSystemRoadSettings.DefaultCornerRoadGuid;
    private Guid _endRoadGuid = LSystemRoadSettings.DefaultEndRoadGuid;

    public LSystemRoadsExtension()
    {
        _instance = this;

        UIBuilder builder = new(200f, 30f);
        builder.SetAnchorHorizontal(1f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(-230f)
            .SetOffsetVertical(EditorLevelObjectsUI.assetsScrollBox.SizeOffset_Y + 200f)
            .SetText("Road city");

        _roadsPanelButton = builder.BuildButton("Open L-System city road generation");

        builder.ResetProperties()
            .SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(0.5f)
            .SetOffsetHorizontal(-160f)
            .SetOffsetVertical(-335f)
            .SetSizeHorizontal(320f)
            .SetSizeVertical(670f)
            .SetText("");

        _settingsContainer = builder.BuildBox();
        _settingsContainer.IsVisible = false;

        builder.ResetProperties()
            .SetOffsetHorizontal(130f)
            .SetOffsetVertical(25f)
            .SetSizeHorizontal(160f)
            .SetSizeVertical(30f);

        _sizeButton = builder.BuildButtonState(
            new GUIContent("Small", "Small city road network"),
            new GUIContent("Medium", "Medium city road network"),
            new GUIContent("Large", "Large city road network")
        );
        _sizeButton.AddLabel("Size", ESleekSide.LEFT);
        _sizeButton.state = 1;

        builder.SetOffsetVertical(65f)
            .SetText("Seed");
        _seedField = builder.BuildInt32Field("Repeatable random seed");

        builder.SetOffsetVertical(105f)
            .SetText("Segments");
        _segmentLimitField = builder.BuildInt32Field($"Maximum accepted segments, capped at {HardSegmentLimit}");

        builder.SetOffsetVertical(145f)
            .SetText("Block Length");
        _initialLengthField = builder.BuildInt32Field("Initial segment length in road-piece units");

        builder.SetOffsetVertical(185f)
            .SetText("Branch %");
        _branchChanceField = builder.BuildFloatInput();

        builder.SetOffsetVertical(225f)
            .SetText("Turn %");
        _turnChanceField = builder.BuildFloatInput();

        builder.SetOffsetVertical(265f)
            .SetText("Radius");
        _radiusField = builder.BuildFloatInput();

        builder.SetOffsetHorizontal(30f)
            .SetOffsetVertical(305f)
            .SetSizeHorizontal(120f)
            .SetText("Use Straight");
        _straightRoadButton = builder.BuildButton("Assign the currently selected object asset as the straight road piece");

        builder.SetOffsetHorizontal(160f)
            .SetOffsetVertical(305f)
            .SetSizeHorizontal(130f)
            .SetText("Default");
        _straightRoadLabel = builder.BuildLabel(TextAnchor.MiddleLeft);

        builder.SetOffsetHorizontal(30f)
            .SetOffsetVertical(345f)
            .SetSizeHorizontal(120f)
            .SetText("Use Tee");
        _teeRoadButton = builder.BuildButton("Assign the currently selected object asset as the three-way junction piece");

        builder.SetOffsetHorizontal(160f)
            .SetOffsetVertical(345f)
            .SetSizeHorizontal(130f)
            .SetText("Default");
        _teeRoadLabel = builder.BuildLabel(TextAnchor.MiddleLeft);

        builder.SetOffsetHorizontal(30f)
            .SetOffsetVertical(385f)
            .SetSizeHorizontal(120f)
            .SetText("Use Quad");
        _quadRoadButton = builder.BuildButton("Assign the currently selected object asset as the four-way junction piece");

        builder.SetOffsetHorizontal(160f)
            .SetOffsetVertical(385f)
            .SetSizeHorizontal(130f)
            .SetText("Default");
        _quadRoadLabel = builder.BuildLabel(TextAnchor.MiddleLeft);

        builder.SetOffsetHorizontal(30f)
            .SetOffsetVertical(425f)
            .SetSizeHorizontal(120f)
            .SetText("Use Corner");
        _cornerRoadButton = builder.BuildButton("Assign the currently selected object asset as the corner road piece");

        builder.SetOffsetHorizontal(160f)
            .SetOffsetVertical(425f)
            .SetSizeHorizontal(130f)
            .SetText("Default");
        _cornerRoadLabel = builder.BuildLabel(TextAnchor.MiddleLeft);

        builder.SetOffsetHorizontal(30f)
            .SetOffsetVertical(465f)
            .SetSizeHorizontal(120f)
            .SetText("Use End");
        _endRoadButton = builder.BuildButton("Assign the currently selected object asset as the dead-end road piece");

        builder.SetOffsetHorizontal(160f)
            .SetOffsetVertical(465f)
            .SetSizeHorizontal(130f)
            .SetText("Default");
        _endRoadLabel = builder.BuildLabel(TextAnchor.MiddleLeft);

        builder.SetOffsetHorizontal(30f)
            .SetOffsetVertical(510f)
            .SetSizeHorizontal(260f)
            .SetText("Reset road pieces");
        _resetRoadsButton = builder.BuildButton("Reset road pieces to EditorHelper defaults");

        builder.SetOffsetHorizontal(30f)
            .SetOffsetVertical(550f)
            .SetSizeHorizontal(260f)
            .SetText("Clear generated roads");
        _clearGeneratedRoadsButton = builder.BuildButton("Remove all roads generated by Road city in this editor session");

        builder.SetOffsetHorizontal(30f)
            .SetOffsetVertical(590f)
            .SetSizeHorizontal(260f)
            .SetText("Generate city roads");
        _generateRoadsButton = builder.BuildButton("Generate a bounded road network from the selected road/object");

        builder.SetOffsetVertical(625f)
            .SetText("Ready");
        _statusLabel = builder.BuildLabel(TextAnchor.MiddleLeft);
        _statusLabel.TextContrastContext = ETextContrastContext.ColorfulBackdrop;

        ResetRoadAssets();
        _seedField.Value = 4148;
        ApplyPreset(1);
        Initialize();
        MapEditorConfigHelper.RegisterExtensionSettings(ConfigSection, CaptureSettings, ApplySettings);
    }

    public void Initialize()
    {
        if (_container == null) return;

        EditorLevelObjectsUI.assetsScrollBox.SizeOffset_Y -= 40f;
        _container.AddChild(_roadsPanelButton);
        _container.AddChild(_settingsContainer);
        _settingsContainer.AddChild(_sizeButton);
        _settingsContainer.AddChild(_seedField);
        _settingsContainer.AddChild(_segmentLimitField);
        _settingsContainer.AddChild(_initialLengthField);
        _settingsContainer.AddChild(_branchChanceField);
        _settingsContainer.AddChild(_turnChanceField);
        _settingsContainer.AddChild(_radiusField);
        _settingsContainer.AddChild(_straightRoadButton);
        _settingsContainer.AddChild(_teeRoadButton);
        _settingsContainer.AddChild(_quadRoadButton);
        _settingsContainer.AddChild(_cornerRoadButton);
        _settingsContainer.AddChild(_endRoadButton);
        _settingsContainer.AddChild(_straightRoadLabel);
        _settingsContainer.AddChild(_teeRoadLabel);
        _settingsContainer.AddChild(_quadRoadLabel);
        _settingsContainer.AddChild(_cornerRoadLabel);
        _settingsContainer.AddChild(_endRoadLabel);
        _settingsContainer.AddChild(_resetRoadsButton);
        _settingsContainer.AddChild(_clearGeneratedRoadsButton);
        _settingsContainer.AddChild(_generateRoadsButton);
        _settingsContainer.AddChild(_statusLabel);

        _roadsPanelButton.OnClicked += OnRoadsPanelClicked;
        _sizeButton.onSwappedState += OnSizeChanged;
        _straightRoadButton.OnClicked += OnStraightRoadClicked;
        _teeRoadButton.OnClicked += OnTeeRoadClicked;
        _quadRoadButton.OnClicked += OnQuadRoadClicked;
        _cornerRoadButton.OnClicked += OnCornerRoadClicked;
        _endRoadButton.OnClicked += OnEndRoadClicked;
        _resetRoadsButton.OnClicked += OnResetRoadsClicked;
        _clearGeneratedRoadsButton.OnClicked += OnClearGeneratedRoadsClicked;
        _generateRoadsButton.OnClicked += OnGenerateRoadsClicked;
    }

    private void OnRoadsPanelClicked(ISleekElement button)
    {
        _settingsContainer.IsVisible = !_settingsContainer.IsVisible;
    }

    public static bool IsOpen => _instance?._settingsContainer.IsVisible == true;

    public static void CloseIfOpen()
    {
        if (_instance != null) _instance._settingsContainer.IsVisible = false;
    }

    private void OnSizeChanged(SleekButtonState button, int index)
    {
        ApplyPreset(index);
    }

    private void OnStraightRoadClicked(ISleekElement button)
    {
        AssignSelectedRoadAsset(ref _straightRoadGuid, _straightRoadLabel);
    }

    private void OnTeeRoadClicked(ISleekElement button)
    {
        AssignSelectedRoadAsset(ref _teeRoadGuid, _teeRoadLabel);
    }

    private void OnQuadRoadClicked(ISleekElement button)
    {
        AssignSelectedRoadAsset(ref _quadRoadGuid, _quadRoadLabel);
    }

    private void OnCornerRoadClicked(ISleekElement button)
    {
        AssignSelectedRoadAsset(ref _cornerRoadGuid, _cornerRoadLabel);
    }

    private void OnEndRoadClicked(ISleekElement button)
    {
        AssignSelectedRoadAsset(ref _endRoadGuid, _endRoadLabel);
    }

    private void OnResetRoadsClicked(ISleekElement button)
    {
        ResetRoadAssets();
    }

    private void OnClearGeneratedRoadsClicked(ISleekElement button)
    {
        List<Transform> roads = _generatedRoadBatches.SelectMany(batch => batch).Where(transform => transform != null).ToList();
        _generatedRoadBatches.Clear();

        if (roads.Count == 0)
        {
            _statusLabel.TextColor = Color.white;
            _statusLabel.Text = "No generated roads to clear";
            return;
        }

        EditorObjects.clearSelection();
        LevelObjects.step++;
        foreach (Transform road in roads)
        {
            LevelObjects.registerRemoveObject(road);
        }

        _statusLabel.TextColor = Color.white;
        _statusLabel.Text = $"Cleared {roads.Count} generated roads";
    }

    private void OnGenerateRoadsClicked(ISleekElement button)
    {
        if (EditorObjects.selection == null || EditorObjects.selection.Count != 1)
        {
            ShowPanelMessage("Select one road/object to use as the L-System road start.");
            return;
        }

        Transform selectedObject = EditorObjects.selection.First().transform;
        LevelObject? levelObject = LevelObjects.FindLevelObject(selectedObject.gameObject);
        if (levelObject == null)
        {
            ShowPanelMessage("Selected object could not be resolved.");
            return;
        }

        ObjectAsset? previousObjectAsset = EditorObjects.selectedObjectAsset;
        ItemAsset? previousItemAsset = EditorObjects.selectedItemAsset;

        try
        {
            EditorObjects.clearSelection();

            if (!LSystemRoadGenerator.TryGenerate(levelObject, BuildSettings(), out List<Transform> generatedRoads, out string errorMessage))
            {
                ShowPanelMessage(errorMessage);
                return;
            }

            if (generatedRoads.Count > 0)
            {
                _generatedRoadBatches.Add(generatedRoads);
            }

            _statusLabel.TextColor = Color.white;
            _statusLabel.Text = $"Generated {generatedRoads.Count} roads";
        }
        finally
        {
            EditorLevelObjectsUIPatches.SetSelectedObjectAsset(previousObjectAsset ?? (Asset?)previousItemAsset);
        }
    }

    private LSystemRoadSettings BuildSettings()
    {
        return new LSystemRoadSettings
        {
            Seed = _seedField.Value,
            SegmentLimit = Mathf.Clamp(_segmentLimitField.Value, 1, HardSegmentLimit),
            InitialLength = Mathf.Clamp(_initialLengthField.Value, 2, 24),
            MinLength = 2,
            BranchChance = Mathf.Clamp01(_branchChanceField.Value / 100f),
            TurnChance = Mathf.Clamp01(_turnChanceField.Value / 100f),
            BranchAngles = new[] { -90f, 90f },
            MaxRadiusFromStart = Mathf.Clamp(_radiusField.Value, 100f, 2000f),
            SnapDistance = 10f,
            CollisionDistance = 18f,
            StraightRoadGuid = _straightRoadGuid,
            TeeRoadGuid = _teeRoadGuid,
            QuadRoadGuid = _quadRoadGuid,
            CornerRoadGuid = _cornerRoadGuid,
            EndRoadGuid = _endRoadGuid
        };
    }

    private void ApplyPreset(int index)
    {
        switch (index)
        {
            case 0:
                _segmentLimitField.Value = 32;
                _initialLengthField.Value = 6;
                _branchChanceField.Value = 25f;
                _turnChanceField.Value = 15f;
                _radiusField.Value = 350f;
                break;
            case 2:
                _segmentLimitField.Value = 128;
                _initialLengthField.Value = 10;
                _branchChanceField.Value = 45f;
                _turnChanceField.Value = 30f;
                _radiusField.Value = 950f;
                break;
            default:
                _segmentLimitField.Value = 64;
                _initialLengthField.Value = 8;
                _branchChanceField.Value = 35f;
                _turnChanceField.Value = 25f;
                _radiusField.Value = 650f;
                break;
        }

        _statusLabel.TextColor = Color.white;
        _statusLabel.Text = $"Preset: {(index == 0 ? "Small" : index == 2 ? "Large" : "Medium")}";
    }

    private void AssignSelectedRoadAsset(ref Guid targetGuid, ISleekLabel label)
    {
        ObjectAsset? selectedAsset = EditorObjects.selectedObjectAsset;
        if (selectedAsset == null)
        {
            ShowPanelMessage("Select an object asset from the object browser first.");
            return;
        }

        targetGuid = selectedAsset.GUID;
        label.Text = selectedAsset.FriendlyName;
    }

    private void ResetRoadAssets()
    {
        _straightRoadGuid = LSystemRoadSettings.DefaultStraightRoadGuid;
        _teeRoadGuid = LSystemRoadSettings.DefaultTeeRoadGuid;
        _quadRoadGuid = LSystemRoadSettings.DefaultQuadRoadGuid;
        _cornerRoadGuid = LSystemRoadSettings.DefaultCornerRoadGuid;
        _endRoadGuid = LSystemRoadSettings.DefaultEndRoadGuid;
        _straightRoadLabel.Text = "Default";
        _teeRoadLabel.Text = "Default";
        _quadRoadLabel.Text = "Default";
        _cornerRoadLabel.Text = "Default";
        _endRoadLabel.Text = "Default";
    }

    private static void ShowMessage(string message)
    {
        if (ExtensionManager.TryGetInstance(out PromptsExtension? promptsExtension) && promptsExtension != null)
        {
            promptsExtension.DisplayAlert(message);
            return;
        }

        CommandWindow.LogError("[EditorHelper2] " + message);
    }

    private void ShowPanelMessage(string message)
    {
        _settingsContainer.IsVisible = false;
        ShowMessage(message);
    }

    public void Dispose()
    {
        MapEditorConfigHelper.UnregisterExtensionSettings(ConfigSection);
        if (_container == null) return;

        EditorLevelObjectsUI.assetsScrollBox.SizeOffset_Y += 40f;
        _roadsPanelButton.OnClicked -= OnRoadsPanelClicked;
        _sizeButton.onSwappedState -= OnSizeChanged;
        _straightRoadButton.OnClicked -= OnStraightRoadClicked;
        _teeRoadButton.OnClicked -= OnTeeRoadClicked;
        _quadRoadButton.OnClicked -= OnQuadRoadClicked;
        _cornerRoadButton.OnClicked -= OnCornerRoadClicked;
        _endRoadButton.OnClicked -= OnEndRoadClicked;
        _resetRoadsButton.OnClicked -= OnResetRoadsClicked;
        _clearGeneratedRoadsButton.OnClicked -= OnClearGeneratedRoadsClicked;
        _generateRoadsButton.OnClicked -= OnGenerateRoadsClicked;
        _container.RemoveChild(_roadsPanelButton);
        _container.RemoveChild(_settingsContainer);
        if (_instance == this) _instance = null;
    }

    private Settings CaptureSettings() => new()
    {
        SizePreset = _sizeButton.state,
        Seed = _seedField.Value,
        SegmentLimit = _segmentLimitField.Value,
        InitialLength = _initialLengthField.Value,
        BranchChance = _branchChanceField.Value,
        TurnChance = _turnChanceField.Value,
        Radius = _radiusField.Value,
        StraightRoadGuid = _straightRoadGuid,
        TeeRoadGuid = _teeRoadGuid,
        QuadRoadGuid = _quadRoadGuid,
        CornerRoadGuid = _cornerRoadGuid,
        EndRoadGuid = _endRoadGuid
    };

    private void ApplySettings(Settings settings)
    {
        _sizeButton.state = Mathf.Clamp(settings.SizePreset, 0, 2);
        _seedField.Value = settings.Seed;
        _segmentLimitField.Value = settings.SegmentLimit;
        _initialLengthField.Value = settings.InitialLength;
        _branchChanceField.Value = settings.BranchChance;
        _turnChanceField.Value = settings.TurnChance;
        _radiusField.Value = settings.Radius;
        _straightRoadGuid = settings.StraightRoadGuid;
        _teeRoadGuid = settings.TeeRoadGuid;
        _quadRoadGuid = settings.QuadRoadGuid;
        _cornerRoadGuid = settings.CornerRoadGuid;
        _endRoadGuid = settings.EndRoadGuid;
        SetRoadAssetLabel(_straightRoadGuid, LSystemRoadSettings.DefaultStraightRoadGuid, _straightRoadLabel);
        SetRoadAssetLabel(_teeRoadGuid, LSystemRoadSettings.DefaultTeeRoadGuid, _teeRoadLabel);
        SetRoadAssetLabel(_quadRoadGuid, LSystemRoadSettings.DefaultQuadRoadGuid, _quadRoadLabel);
        SetRoadAssetLabel(_cornerRoadGuid, LSystemRoadSettings.DefaultCornerRoadGuid, _cornerRoadLabel);
        SetRoadAssetLabel(_endRoadGuid, LSystemRoadSettings.DefaultEndRoadGuid, _endRoadLabel);
    }

    private static void SetRoadAssetLabel(Guid guid, Guid defaultGuid, ISleekLabel label)
    {
        label.Text = guid == defaultGuid
            ? "Default"
            : (SDG.Unturned.Assets.find(guid) as ObjectAsset)?.FriendlyName ?? guid.ToString("N");
    }

    private sealed class Settings
    {
        public int SizePreset { get; set; } = 1;
        public int Seed { get; set; } = 4148;
        public int SegmentLimit { get; set; } = 64;
        public int InitialLength { get; set; } = 8;
        public float BranchChance { get; set; } = 35f;
        public float TurnChance { get; set; } = 25f;
        public float Radius { get; set; } = 650f;
        public Guid StraightRoadGuid { get; set; } = LSystemRoadSettings.DefaultStraightRoadGuid;
        public Guid TeeRoadGuid { get; set; } = LSystemRoadSettings.DefaultTeeRoadGuid;
        public Guid QuadRoadGuid { get; set; } = LSystemRoadSettings.DefaultQuadRoadGuid;
        public Guid CornerRoadGuid { get; set; } = LSystemRoadSettings.DefaultCornerRoadGuid;
        public Guid EndRoadGuid { get; set; } = LSystemRoadSettings.DefaultEndRoadGuid;
    }
}
