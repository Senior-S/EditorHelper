using System;
using System.Collections;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Helpers;
using EditorHelper2.UI.Builders;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Editor.Pause;

[UIExtension(typeof(EditorPauseUI))]
[EHExtension("Excel Export Extension", "Senior S")]
public class ExcelExportExtension : UIExtension, IExtension
{
    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;

    private readonly ISleekButton _exportButton;
    private readonly ISleekBox _statusBox;

    public ExcelExportExtension()
    {
        UIBuilder builder = new(200f, 30f);

        builder.SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(0.5f)
            .SetOffsetHorizontal(-100f)
            .SetOffsetVertical(225f)
            .SetText("Export Map Data");

        _exportButton = builder.BuildButton("Export map assets to Excel");

        builder.SetOffsetVertical(265f)
            .SetText("");

        _statusBox = builder.BuildBox();
        _statusBox.IsVisible = false;

        Initialize();
    }

    public void Initialize()
    {
        if (_container == null) return;

        _exportButton.OnClicked += OnExportClicked;
        _container.AddChild(_exportButton);
        _container.AddChild(_statusBox);
    }

    private void OnExportClicked(ISleekElement button)
    {
        DiscordRichPresence? drp = EditorHelper.GetRichPresence();
        if (drp != null)
        {
            drp.StartCoroutine(ExportRoutine());
        }
        else
        {
            UnturnedLog.warn("EditorHelper2: Could not find DiscordRichPresence to start coroutine. Running synchronously.");
            ExportSynchronous();
        }
    }

    private void ExportSynchronous()
    {
        _statusBox.Text = "Exporting...";
        _statusBox.IsVisible = true;

        try
        {
            ExcelExporter.Export();
            _statusBox.Text = "Exported to Desktop!";
        }
        catch (Exception ex)
        {
            UnturnedLog.error(ex);
            _statusBox.Text = "Export Failed!";
        }
    }

    private IEnumerator ExportRoutine()
    {
        _statusBox.Text = "Exporting...";
        _statusBox.IsVisible = true;
        _exportButton.IsClickable = false;

        yield return null;
        yield return new WaitForEndOfFrame();

        Exception? error = null;

        try
        {
             ExcelExporter.Export();
        }
        catch (Exception ex)
        {
            error = ex;
            UnturnedLog.error(ex);
        }

        _exportButton.IsClickable = true;

        _statusBox.Text = error != null ? "Export Failed!" : "Exported to Desktop!";

        yield return new WaitForSeconds(3f);
        _statusBox.IsVisible = false;
    }

    public void Dispose()
    {
        if (_container == null) return;

        _exportButton.OnClicked -= OnExportClicked;
        _container.RemoveChild(_exportButton);
        _container.RemoveChild(_statusBox);
    }
}
