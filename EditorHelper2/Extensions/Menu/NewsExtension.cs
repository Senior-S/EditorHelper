using DanielWillett.UITools.API.Extensions;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Helpers;
using EditorHelper2.UI.Builders;
using SDG.Unturned;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace EditorHelper2.Extensions.Menu;

[UIExtension(typeof(MenuDashboardUI))]
[EHExtension("NewsExtension extension", "Senior S", true)]
public class NewsExtension : UIExtension, IExtension
{
    private const float AlertHeight = 60f;
    private const float AlertSpacing = 10f;

    private readonly ISleekBox _updateRequiredBox;
    private readonly Texture2D _statusIconTexture;

    private ISleekButton? _statusAlertButton;
    private ISleekImage? _statusAlertIcon;
    private ISleekLabel? _statusAlertHeaderLabel;
    private ISleekLabel? _statusAlertBodyLabel;
    private ISleekElement? _replacedDashboardAlert;
    private bool _createdHeaderSpace;
    private bool _statusAlertClickedHooked;

    public NewsExtension()
    {
        Bundle icons = Bundles.getBundle("/Bundles/Textures/Menu/Icons/MenuPause/MenuPause.unity3d");
        _statusIconTexture = icons.load<Texture2D>("Steam");
        icons.unload();

        UIBuilder builder = new(0f, 0f);

        // I'll love to don't require this but lately ppl have been reported bugs of outdated version due they don't see the top announce
        // So to assure the best experience for now it will be required to have the latest version.
        #region UpdateRequiredBox
        builder.ResetProperties()
                .SetSizeHorizontal(400f)
                .SetSizeVertical(150f)
                .SetAnchorHorizontal(0.5f)
                .SetAnchorVertical(0.5f)
                .SetOffsetHorizontal(-200f)
                .SetOffsetVertical(-75f)
                .SetText("You're not using the latest version of the module!");

        _updateRequiredBox = builder.BuildBox();
        _updateRequiredBox.IsVisible = false;

        builder.ResetProperties()
            .SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(-100f)
            .SetOffsetVertical(5f)
            .SetSizeHorizontal(200f)
            .SetSizeVertical(30f)
            .SetText("Update & Quit");

        ISleekButton updateButton = builder.BuildButton("Update your module right now");
        updateButton.OnClicked += (_) =>
        {
            OpenUrl("https://editorhelper.sshost.club/Download");

            Provider.QuitGame("EditorHelper requires an update!");
        };

        _updateRequiredBox.AddChild(updateButton);
        #endregion

        Initialize();
    }

    public void Initialize()
    {
        MenuUI.container.AddChild(_updateRequiredBox);
        EnsureStatusAlert();
        UpdateStatusAlert(EVersionStatus.Loading);

        if (UpdaterCore.ConfigLoaded()) VersionStatusReady();
        UpdaterCore.OnConfigLoaded += VersionStatusReady; // Allow for Realtime Updating
        LiveConfig.OnRefreshed += OnLiveConfigRefreshed;
    }

    #region Extensions Event Handlers
    private void VersionStatusReady()
    {
        EVersionStatus versionStatus = UpdaterCore.GetVersionStatus();

        if (versionStatus == EVersionStatus.Outdated)
        {
            _updateRequiredBox.Text = $"You're not using the latest version of the module! This version may contain bugs and issues that can make you lose several hours of your time.\nPlease update to the latest version {UpdaterCore.LatestVersion} to enjoy the best experience the module have to offer!";
            _updateRequiredBox.IsVisible = true;

            MenuUI.instance.escapeMenu();
            MenuPauseUI.close();
        }

        UpdateStatusAlert(versionStatus);
    }

    private void OnLiveConfigRefreshed()
    {
        ISleekElement? dashboardAlert = GetDashboardField<ISleekElement>("alertBox");
        if (dashboardAlert != null && !ReferenceEquals(dashboardAlert, _replacedDashboardAlert))
        {
            _replacedDashboardAlert = dashboardAlert;
            dashboardAlert.IsVisible = false;

            if (_statusAlertButton != null)
            {
                _statusAlertButton.PositionOffset_Y = dashboardAlert.PositionOffset_Y;
            }

            if (_createdHeaderSpace)
            {
                ReleaseCreatedHeaderSpace();
            }
        }

        UpdateStatusAlert(UpdaterCore.GetVersionStatus());
    }
    #endregion

    #region Extension Functions
    private void EnsureStatusAlert()
    {
        if (_statusAlertButton != null)
        {
            return;
        }

        SleekFullscreenBox? container = GetDashboardField<SleekFullscreenBox>("container");
        if (container == null)
        {
            return;
        }

        ISleekElement? dashboardAlert = GetDashboardField<ISleekElement>("alertBox");
        _replacedDashboardAlert = dashboardAlert;

        _statusAlertButton = Glazier.Get().CreateButton();
        _statusAlertButton.PositionOffset_X = 210f;
        _statusAlertButton.SizeOffset_X = -210f;
        _statusAlertButton.SizeScale_X = 1f;
        _statusAlertButton.SizeOffset_Y = AlertHeight;
        _statusAlertButton.TooltipText = "Join the EditorHelper2 Discord";

        if (dashboardAlert != null)
        {
            _statusAlertButton.PositionOffset_Y = dashboardAlert.PositionOffset_Y;
            dashboardAlert.IsVisible = false;
        }
        else
        {
            float mainHeaderOffset = GetDashboardField<float>("mainHeaderOffset");
            _statusAlertButton.PositionOffset_Y = mainHeaderOffset;
            SetDashboardField("mainHeaderOffset", mainHeaderOffset + AlertHeight + AlertSpacing);

            ISleekScrollView? mainScrollView = GetDashboardField<ISleekScrollView>("mainScrollView");
            if (mainScrollView != null)
            {
                mainScrollView.PositionOffset_Y += AlertHeight + AlertSpacing;
                mainScrollView.SizeOffset_Y -= AlertHeight + AlertSpacing;
                _createdHeaderSpace = true;
            }
        }

        _statusAlertButton.OnClicked += OnStatusButtonClicked;
        _statusAlertClickedHooked = true;
        container.AddChild(_statusAlertButton);

        _statusAlertIcon = Glazier.Get().CreateImage();
        _statusAlertIcon.PositionOffset_X = 10f;
        _statusAlertIcon.PositionOffset_Y = 10f;
        _statusAlertIcon.SizeOffset_X = 40f;
        _statusAlertIcon.SizeOffset_Y = 40f;
        _statusAlertIcon.Texture = _statusIconTexture;
        _statusAlertIcon.TintColor = ESleekTint.FOREGROUND;
        _statusAlertButton.AddChild(_statusAlertIcon);

        _statusAlertHeaderLabel = Glazier.Get().CreateLabel();
        _statusAlertHeaderLabel.PositionOffset_X = 60f;
        _statusAlertHeaderLabel.SizeScale_X = 1f;
        _statusAlertHeaderLabel.SizeOffset_X = -70f;
        _statusAlertHeaderLabel.SizeOffset_Y = 30f;
        _statusAlertHeaderLabel.TextAlignment = TextAnchor.MiddleLeft;
        _statusAlertHeaderLabel.FontSize = ESleekFontSize.Medium;
        _statusAlertHeaderLabel.TextContrastContext = ETextContrastContext.ColorfulBackdrop;
        _statusAlertButton.AddChild(_statusAlertHeaderLabel);

        _statusAlertBodyLabel = Glazier.Get().CreateLabel();
        _statusAlertBodyLabel.PositionOffset_X = 60f;
        _statusAlertBodyLabel.PositionOffset_Y = 20f;
        _statusAlertBodyLabel.SizeScale_X = 1f;
        _statusAlertBodyLabel.SizeScale_Y = 1f;
        _statusAlertBodyLabel.SizeOffset_X = -70f;
        _statusAlertBodyLabel.SizeOffset_Y = -20f;
        _statusAlertBodyLabel.TextAlignment = TextAnchor.UpperLeft;
        _statusAlertBodyLabel.TextColor = ESleekTint.RICH_TEXT_DEFAULT;
        _statusAlertBodyLabel.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
        _statusAlertBodyLabel.AllowRichText = true;
        _statusAlertButton.AddChild(_statusAlertBodyLabel);
    }

    private void UpdateStatusAlert(EVersionStatus versionStatus)
    {
        EnsureStatusAlert();
        if (_statusAlertButton == null || _statusAlertHeaderLabel == null || _statusAlertBodyLabel == null)
        {
            return;
        }

        string header = $"EditorHelper2 v{GetType().Assembly.GetName().Version}";
        string body = GetUpdateAlert(versionStatus);
        string tooltip = "Join the EditorHelper2 Discord";

        if (UpdaterCore.TryGetTexts(out string? updateMessage, out string? title, out string? subtitle))
        {
            header = string.IsNullOrWhiteSpace(title) ? header : title;
            string status = GetUpdateStatusLine(versionStatus);
            body = string.IsNullOrWhiteSpace(subtitle) ? status : $"{subtitle}\n{status}";
            tooltip = string.IsNullOrWhiteSpace(updateMessage)
                ? tooltip
                : $"{updateMessage}\n\nClick to join the EditorHelper2 Discord.";
        }

        _statusAlertHeaderLabel.Text = header;
        _statusAlertBodyLabel.Text = body;
        _statusAlertButton.TooltipText = tooltip;
        _statusAlertButton.IsVisible = true;
    }

    private static void OnStatusButtonClicked(ISleekElement button)
    {
        OpenUrl("https://discord.gg/Y3jD5K2Q8C");
    }

    private static T? GetDashboardField<T>(string name)
    {
        FieldInfo? field = typeof(MenuDashboardUI).GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null)
        {
            return default;
        }

        object? value = field.GetValue(null);
        return value is T typedValue ? typedValue : default;
    }

    private static void SetDashboardField<T>(string name, T value)
    {
        typeof(MenuDashboardUI)
            .GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(null, value);
    }

    // https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/
    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw;
            }
        }
    }

    private static string GetUpdateAlert(EVersionStatus versionStatus)
    {
        return versionStatus switch
        {
            EVersionStatus.Loading => "Loading latest version..",
            EVersionStatus.Unknown => "Failed to get latest version.",
            EVersionStatus.Outdated => "<size=+2><b>Outdated version, please download the latest version to get the latest features!</b></size>",
            EVersionStatus.Latest => "You're using the latest version!",
            _ => versionStatus.ToString()
        } + "\nGet the latest news, releases, and previews of future updates in our discord, click on this alert to join!";
    }

    private static string GetUpdateStatusLine(EVersionStatus versionStatus)
    {
        return versionStatus switch
        {
            EVersionStatus.Loading => "Loading latest version..",
            EVersionStatus.Unknown => "Failed to get latest version.",
            EVersionStatus.Outdated => $"Update required. Latest version: {UpdaterCore.LatestVersion}",
            EVersionStatus.Latest => "You're using the latest version.",
            _ => versionStatus.ToString()
        };
    }

    #endregion Extension Functions

    public void Dispose()
    {
        MenuUI.container.RemoveChild(_updateRequiredBox);

        if (_statusAlertButton != null)
        {
            if (_statusAlertClickedHooked)
            {
                _statusAlertButton.OnClicked -= OnStatusButtonClicked;
            }

            GetDashboardField<SleekFullscreenBox>("container")?.RemoveChild(_statusAlertButton);
            _statusAlertButton = null;
        }

        if (_replacedDashboardAlert != null)
        {
            _replacedDashboardAlert.IsVisible = true;
            _replacedDashboardAlert = null;
        }

        if (_createdHeaderSpace)
        {
            ReleaseCreatedHeaderSpace();
        }

        LiveConfig.OnRefreshed -= OnLiveConfigRefreshed;
        UpdaterCore.OnConfigLoaded -= VersionStatusReady;
    }

    private void ReleaseCreatedHeaderSpace()
    {
        float mainHeaderOffset = GetDashboardField<float>("mainHeaderOffset");
        SetDashboardField("mainHeaderOffset", mainHeaderOffset - AlertHeight - AlertSpacing);

        ISleekScrollView? mainScrollView = GetDashboardField<ISleekScrollView>("mainScrollView");
        if (mainScrollView != null)
        {
            mainScrollView.PositionOffset_Y -= AlertHeight + AlertSpacing;
            mainScrollView.SizeOffset_Y += AlertHeight + AlertSpacing;
        }

        _createdHeaderSpace = false;
    }
}