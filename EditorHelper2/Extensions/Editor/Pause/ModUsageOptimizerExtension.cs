using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Helpers;
using EditorHelper2.Extensions.Editor.Dashboard;
using EditorHelper2.Loader;
using EditorHelper2.Optimization;
using EditorHelper2.UI.Builders;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Editor.Pause;

/// <summary>
/// Exports only the mod assets used by the current map into a new output mod and patches the saved level files
/// to the regenerated GUIDs.
/// </summary>
[UIExtension(typeof(EditorPauseUI))]
[EHExtension("Mod Usage Optimizer Extension", "Senior S")]
public class ModUsageOptimizerExtension : UIExtension, IExtension
{
    private const float PanelWidth = 460f;
    private const float CompactPanelHeight = 220f;
    private const float ExpandedPanelHeight = 305f;
    private const float PanelPadding = 10f;
    private const float ContentWidth = PanelWidth - PanelPadding * 2f;
    private const float SidePanelMinimumScreenWidth = 1120f;

    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;

    private readonly ISleekBox _panel;
    private readonly ISleekField _outputPathField;
    private readonly ISleekField _parallelBundleJobsField;
    private readonly ISleekToggle _metadataOnlyTrimToggle;
    private readonly ISleekToggle _saveItemsAndVehiclesToggle;
    private readonly ISleekToggle _keepAllModItemsToggle;
    private readonly ISleekButton _optimizeButton;
    private readonly ISleekBox _statusBox;

    private bool _isRunning;
    private string? _latestProgressStatus;

    public ModUsageOptimizerExtension()
    {
        UIBuilder builder = new(PanelWidth, CompactPanelHeight);
        builder.SetAnchorHorizontal(0f)
            .SetAnchorVertical(1f);

        _panel = builder.BuildBox();
        UpdatePanelPlacement();

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(PanelPadding)
            .SetOffsetVertical(PanelPadding)
            .SetSizeHorizontal(ContentWidth)
            .SetSizeVertical(20f)
            .SetText("Optimized mod output folder");
        ISleekLabel titleLabel = builder.BuildLabel(TextAnchor.MiddleLeft);
        _panel.AddChild(titleLabel);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(PanelPadding)
            .SetOffsetVertical(35f)
            .SetSizeHorizontal(ContentWidth)
            .SetSizeVertical(30f)
            .SetText(@"C:\Mods\MyOptimizedMap");
        _outputPathField = builder.BuildStringField();
        _outputPathField.TooltipText = "Absolute folder path where the optimized mod should be written";
        _panel.AddChild(_outputPathField);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(PanelPadding)
            .SetOffsetVertical(75f)
            .SetSizeHorizontal(250f)
            .SetSizeVertical(20f)
            .SetText("Parallel bundle jobs");
        ISleekLabel parallelLabel = builder.BuildLabel(TextAnchor.MiddleLeft);
        _panel.AddChild(parallelLabel);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(PanelPadding + 260f)
            .SetOffsetVertical(70f)
            .SetSizeHorizontal(80f)
            .SetSizeVertical(30f)
            .SetText("1");
        _parallelBundleJobsField = builder.BuildStringField();
        _parallelBundleJobsField.Text = "1";
        _parallelBundleJobsField.TooltipText = "Number of master bundles trimmed at the same time";
        _panel.AddChild(_parallelBundleJobsField);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(PanelPadding)
            .SetOffsetVertical(110f)
            .SetSizeHorizontal(30f)
            .SetSizeVertical(30f)
            .SetText("Metadata-only trim");
        _metadataOnlyTrimToggle = builder.BuildToggle("Skip streamed payload compaction for faster, larger bundle output", ESleekSide.RIGHT);
        _metadataOnlyTrimToggle.Value = false;
        _panel.AddChild(_metadataOnlyTrimToggle);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(PanelPadding + 220f)
            .SetOffsetVertical(110f)
            .SetSizeHorizontal(30f)
            .SetSizeVertical(30f)
            .SetText("Keep required items");
        _saveItemsAndVehiclesToggle = builder.BuildToggle("Export item and vehicle assets used by spawn tables and NPCs", ESleekSide.RIGHT);
        _saveItemsAndVehiclesToggle.Value = true;
        _panel.AddChild(_saveItemsAndVehiclesToggle);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(PanelPadding)
            .SetOffsetVertical(145f)
            .SetSizeHorizontal(30f)
            .SetSizeVertical(30f)
            .SetText("Keep all mod items");
        _keepAllModItemsToggle = builder.BuildToggle("Export every item asset from each mod used by the map", ESleekSide.RIGHT);
        _keepAllModItemsToggle.Value = false;
        _panel.AddChild(_keepAllModItemsToggle);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(PanelPadding)
            .SetOffsetVertical(180f)
            .SetSizeHorizontal(ContentWidth)
            .SetSizeVertical(30f)
            .SetText("Optimize Mod Usage");
        _optimizeButton = builder.BuildButton("Create a new optimized mod and patch the saved level GUIDs");
        _panel.AddChild(_optimizeButton);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(PanelPadding)
            .SetOffsetVertical(220f)
            .SetSizeHorizontal(ContentWidth)
            .SetSizeVertical(70f)
            .SetText(string.Empty);
        _statusBox = builder.BuildBox(TextAnchor.UpperLeft);
        _statusBox.IsVisible = false;
        _panel.AddChild(_statusBox);

        Initialize();
    }

    public void Initialize()
    {
        if (_container == null)
        {
            return;
        }

        _optimizeButton.OnClicked += OnOptimizeClicked;
        _saveItemsAndVehiclesToggle.OnValueChanged += OnSaveItemsAndVehiclesChanged;
        _keepAllModItemsToggle.OnValueChanged += OnKeepAllModItemsChanged;
        _container.AddChild(_panel);
    }

    protected override void Opened()
    {
        UpdatePanelPlacement();
    }

    private void UpdatePanelPlacement()
    {
        if (Screen.width < SidePanelMinimumScreenWidth)
        {
            _panel.PositionScale_X = 0.5f;
            _panel.PositionScale_Y = 0f;
            _panel.PositionOffset_X = -PanelWidth / 2f;
            _panel.PositionOffset_Y = PanelPadding;
            return;
        }

        _panel.PositionScale_X = 0f;
        _panel.PositionScale_Y = 1f;
        _panel.PositionOffset_X = PanelPadding;
        _panel.PositionOffset_Y = -(_panel.SizeOffset_Y + PanelPadding);
    }

    private void OnOptimizeClicked(ISleekElement button)
    {
        if (_isRunning)
        {
            return;
        }

        string outputPathInput = _outputPathField.Text?.Trim() ?? string.Empty;
        string normalizedOutputPath;

        try
        {
            normalizedOutputPath = Path.GetFullPath(outputPathInput);
        }
        catch (Exception)
        {
            SetStatus("Enter a valid absolute output folder path.");
            return;
        }

        if (!Path.IsPathRooted(normalizedOutputPath))
        {
            SetStatus("Enter a valid absolute output folder path.");
            return;
        }

        string parallelBundleJobsInput = _parallelBundleJobsField.Text?.Trim() ?? string.Empty;
        if (!int.TryParse(parallelBundleJobsInput, out int parallelBundleJobs) || parallelBundleJobs < 1)
        {
            SetStatus("Enter a valid parallel bundle job count of 1 or higher.");
            return;
        }

        bool useMetadataOnlyBundleTrim = _metadataOnlyTrimToggle.Value;
        bool saveItemsAndVehicles = _saveItemsAndVehiclesToggle.Value;
        bool keepAllModItems = _keepAllModItemsToggle.Value;

        DiscordRichPresence? richPresence = EditorHelper.GetRichPresence();
        if (richPresence == null)
        {
            SetStatus("Unable to start the optimizer because the coroutine host is missing.");
            return;
        }

        void StartOptimization()
        {
            richPresence.StartCoroutine(RunOptimizationRoutine(
                normalizedOutputPath,
                parallelBundleJobs,
                useMetadataOnlyBundleTrim,
                saveItemsAndVehicles,
                keepAllModItems));
        }

        if (parallelBundleJobs > 2)
        {
            if (!ExtensionManager.TryGetInstance(out PromptsExtension? promptsExtension))
            {
                SetStatus("Unable to confirm high parallelism because the prompt system is missing.");
                return;
            }

            promptsExtension.DisplayQuestion(
                $"Use {parallelBundleJobs} parallel bundle jobs? This can use high memory and disk bandwidth.",
                StartOptimization,
                noAction: () => SetStatus("Optimization aborted."));
            return;
        }

        StartOptimization();
    }

    private IEnumerator RunOptimizationRoutine(
        string outputPath,
        int parallelBundleJobs,
        bool useMetadataOnlyBundleTrim,
        bool saveItemsAndVehicles,
        bool keepAllModItems)
    {
        _isRunning = true;
        _optimizeButton.IsClickable = false;
        _latestProgressStatus = null;
        SetStatus("Step 1/4 - Scanning map assets and checking missing references...");

        yield return null;

        ModOptimizationPlan? plan = null;
        Exception? error = null;

        try
        {
            plan = MapModOptimizationPlanner.CreatePlan(outputPath, saveItemsAndVehicles, keepAllModItems);
        }
        catch (Exception ex)
        {
            error = ex;
        }

        if (error != null || plan == null)
        {
            FinishWithError(error);
            yield break;
        }

        plan.MaxParallelMasterBundleExports = parallelBundleJobs;
        plan.UseMetadataOnlyBundleTrim = useMetadataOnlyBundleTrim;

        string scanSummary =
            $"Step 2/4 - Saving level. Found {plan.RootObjectAssetCount} root objects, {plan.RootResourceAssetCount} root resources, {plan.RootItemSpawnAssetCount} item spawn assets, and {plan.RootVehicleSpawnAssetCount} vehicle spawn assets from installed mods.";
        if (plan.Warnings.Count > 0)
        {
            scanSummary += $" {plan.Warnings.Count} warning(s) were recorded during scanning.";
        }

        SetStatus(scanSummary);
        SDG.Unturned.Level.save();

        yield return null;

        SetStatus("Step 3/4 - Exporting optimized files...");

        ModOptimizationResult? result = null;
        string? lastShownBackgroundStatus = null;
        Task exportTask = Task.Run(() =>
        {
            try
            {
                result = MapModOptimizationExecutor.Execute(plan, message => _latestProgressStatus = message);
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });

        while (!exportTask.IsCompleted)
        {
            if (!string.IsNullOrWhiteSpace(_latestProgressStatus) && !string.Equals(lastShownBackgroundStatus, _latestProgressStatus, StringComparison.Ordinal))
            {
                lastShownBackgroundStatus = _latestProgressStatus;
                SetStatus($"Step 3/4 - {_latestProgressStatus}");
            }

            yield return new WaitForSeconds(0.1f);
        }

        if (error != null || result == null)
        {
            FinishWithError(error);
            yield break;
        }

        string finalStatus =
            $"Step 4/4 - Done. Exported {result.ExportedAssetCount} assets across {result.MasterBundleCount} master bundles. " +
            $"Patched {result.PatchedObjectCount} objects and {result.PatchedResourceCount} resources.";
        SetStatus(finalStatus);

        if (ExtensionManager.TryGetInstance(out PromptsExtension? promptsExtension))
        {
            if (result.Warnings.Count > 0)
            {
                promptsExtension.DisplayAlert($"Optimization finished with {result.Warnings.Count} warning(s). Report: {result.ReportPath}");
            }
            else
            {
                promptsExtension.DisplayAlert($"Optimization finished successfully. Report: {result.ReportPath}");
            }
        }

        _optimizeButton.IsClickable = true;
        _isRunning = false;
    }

    private void FinishWithError(Exception? error)
    {
        if (error != null)
        {
            UnturnedLog.error(error);
        }

        string message = error == null ? "Optimization failed." : $"Optimization failed: {error.Message}";
        SetStatus(message);

        if (error != null && ExtensionManager.TryGetInstance(out PromptsExtension? promptsExtension))
        {
            promptsExtension.DisplayAlert(message);
        }

        _optimizeButton.IsClickable = true;
        _isRunning = false;
    }

    private void SetStatus(string text)
    {
        _statusBox.Text = text;
        _statusBox.IsVisible = true;
        _panel.SizeOffset_Y = ExpandedPanelHeight;
        UpdatePanelPlacement();
    }

    private void OnSaveItemsAndVehiclesChanged(ISleekToggle toggle, bool state)
    {
        if (state)
        {
            _keepAllModItemsToggle.Value = false;
        }
    }

    private void OnKeepAllModItemsChanged(ISleekToggle toggle, bool state)
    {
        if (state)
        {
            _saveItemsAndVehiclesToggle.Value = false;
        }
    }

    public void Dispose()
    {
        if (_container == null)
        {
            return;
        }

        _optimizeButton.OnClicked -= OnOptimizeClicked;
        _saveItemsAndVehiclesToggle.OnValueChanged -= OnSaveItemsAndVehiclesChanged;
        _keepAllModItemsToggle.OnValueChanged -= OnKeepAllModItemsChanged;
        _container.RemoveChild(_panel);
    }
}
