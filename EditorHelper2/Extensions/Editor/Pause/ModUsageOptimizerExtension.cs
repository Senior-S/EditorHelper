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
    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;

    private readonly ISleekBox _panel;
    private readonly ISleekField _outputPathField;
    private readonly ISleekButton _optimizeButton;
    private readonly ISleekBox _statusBox;

    private bool _isRunning;
    private string? _latestProgressStatus;

    public ModUsageOptimizerExtension()
    {
        UIBuilder builder = new(520f, 185f);
        builder.SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(0.5f)
            .SetOffsetHorizontal(-260f)
            .SetOffsetVertical(305f);

        _panel = builder.BuildBox();

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(10f)
            .SetOffsetVertical(10f)
            .SetSizeHorizontal(500f)
            .SetSizeVertical(20f)
            .SetText("Optimized mod output folder");
        ISleekLabel titleLabel = builder.BuildLabel(TextAnchor.MiddleLeft);
        _panel.AddChild(titleLabel);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(10f)
            .SetOffsetVertical(35f)
            .SetSizeHorizontal(500f)
            .SetSizeVertical(30f)
            .SetText(@"E:\Mods\MyOptimizedMap");
        _outputPathField = builder.BuildStringField();
        _outputPathField.TooltipText = "Absolute folder path where the optimized mod should be written";
        _panel.AddChild(_outputPathField);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(10f)
            .SetOffsetVertical(75f)
            .SetSizeHorizontal(500f)
            .SetSizeVertical(30f)
            .SetText("Optimize Mod Usage");
        _optimizeButton = builder.BuildButton("Create a new optimized mod and patch the saved level GUIDs");
        _panel.AddChild(_optimizeButton);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(10f)
            .SetOffsetVertical(110f)
            .SetSizeHorizontal(500f)
            .SetSizeVertical(60f)
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
        _container.AddChild(_panel);
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

        DiscordRichPresence? richPresence = EditorHelper.GetRichPresence();
        if (richPresence == null)
        {
            SetStatus("Unable to start the optimizer because the coroutine host is missing.");
            return;
        }

        richPresence.StartCoroutine(RunOptimizationRoutine(normalizedOutputPath));
    }

    private IEnumerator RunOptimizationRoutine(string outputPath)
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
            plan = MapModOptimizationPlanner.CreatePlan(outputPath);
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

        string scanSummary =
            $"Step 2/4 - Saving level. Found {plan.RootObjectAssetCount} root objects and {plan.RootResourceAssetCount} root resources from installed mods.";
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
    }

    public void Dispose()
    {
        if (_container == null)
        {
            return;
        }

        _optimizeButton.OnClicked -= OnOptimizeClicked;
        _container.RemoveChild(_panel);
    }
}