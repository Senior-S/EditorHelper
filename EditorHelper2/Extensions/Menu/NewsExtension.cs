using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Helpers;
using EditorHelper2.UI.Builders;
using SDG.Unturned;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

namespace EditorHelper2.Extensions.Menu;

[UIExtension(typeof(MenuDashboardUI))]
[EHExtension("NewsExtension extension", "Senior S", true)]
public class NewsExtension : UIExtension, IExtension
{
    [ExistingMember("battlEyeHeaderLabel")]
    private readonly ISleekLabel? _battlEyeHeaderLabel;

    [ExistingMember("battlEyeBodyLabel")] 
    private readonly ISleekLabel? _battlEyeBodyLabel;

    [ExistingMember("battlEyeIcon")]
    private readonly ISleekImage? _battlEyeIcon;

    private readonly ISleekBox _updateRequiredBox;

    private readonly ISleekBox _updateBox;
    private readonly ISleekLabel _updateTitle;
    private readonly ISleekLabel _updateSubtitle;
    private readonly ISleekLabel _updateMessage;

    public NewsExtension()
    {
        Bundle icons = Bundles.getBundle("/Bundles/Textures/Menu/Icons/MenuPause/MenuPause.unity3d");
        _battlEyeIcon!.Texture = icons.load<Texture2D>("Steam");
        _battlEyeIcon.TintColor = ESleekTint.FOREGROUND;
        icons.unload();

        _battlEyeHeaderLabel!.Text = $"You're running EditorHelper2 v{GetType().Assembly.GetName().Version}";
        _battlEyeHeaderLabel.FontSize = ESleekFontSize.Medium;
        _battlEyeHeaderLabel.PositionOffset_X = 15f;
        
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

        _battlEyeBodyLabel!.AllowRichText = true;
        _battlEyeBodyLabel.Text = GetUpdateAlert(EVersionStatus.Loading);
        _battlEyeBodyLabel.PositionOffset_X = 15f;

        builder.ResetProperties()
                .SetScaleHorizontal(1f);

        _updateBox = builder.BuildBox();
        _updateBox.UseManualLayout = false;
        _updateBox.UseChildAutoLayout = ESleekChildLayout.Vertical;
        _updateBox.ChildAutoLayoutPadding = 5f;
        _updateBox.IsVisible = false;
        builder.ResetProperties()
            .SetText("Title");
        _updateTitle = builder.BuildLabel(TextAnchor.UpperLeft, ESleekFontSize.Large);
        _updateTitle.UseManualLayout = false;
        _updateBox.AddChild(_updateTitle);

        builder.ResetProperties()
            .SetText("Subtitle");
        _updateSubtitle = builder.BuildLabel(TextAnchor.UpperLeft, ESleekFontSize.Tiny);
        _updateSubtitle.UseManualLayout = false;
        _updateSubtitle.TextColor = new SleekColor(ESleekTint.FONT, 0.5f);
        _updateBox.AddChild(_updateSubtitle);

        builder.ResetProperties()
            .SetText("Update Message");
        _updateMessage = builder.BuildLabel(TextAnchor.UpperLeft);
        _updateMessage.UseManualLayout = false;
        _updateBox.AddChild(_updateMessage);

        SleekWebLinkButton discordButton = new()
        {
            Text = "EditorHelper2 discord",
            Url = "https://discord.gg/Y3jD5K2Q8C",
            UseManualLayout = false,
            UseChildAutoLayout = ESleekChildLayout.Vertical,
            UseHeightLayoutOverride = true,
            ExpandChildren = true,
            SizeOffset_Y = 30f
        };
        _updateBox.AddChild(discordButton);
        SleekWebLinkButton youtubeButton = new()
        {
            Text = "Youtube Channel",
            Url = "https://www.youtube.com/@ssplugins4783/featured",
            UseManualLayout = false,
            UseChildAutoLayout = ESleekChildLayout.Vertical,
            UseHeightLayoutOverride = true,
            ExpandChildren = true,
            SizeOffset_Y = 30f
        };
        _updateBox.AddChild(youtubeButton);

        Initialize();
    }

    public void Initialize()
    {
        MenuUI.container.AddChild(_updateRequiredBox);
        MenuDashboardUI.mainScrollView.AddChild(_updateBox);

        if (UpdaterCore.ConfigLoaded) VersionStatusReady();
        UpdaterCore.OnConfigLoaded += VersionStatusReady; // Allow for Realtime Updating
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

        _battlEyeBodyLabel!.Text = GetUpdateAlert(versionStatus);

        if (UpdaterCore.TryGetTexts(out string? updateMessage, out string? title, out string? subtitle))
        {
            _updateTitle.Text = title;
            _updateSubtitle.Text = subtitle;
            _updateMessage.Text = updateMessage;

            _updateBox.IsVisible = true;
            MenuDashboardUI.newAnnouncement = _updateBox;
            MenuDashboardUI.ReviseNewsOrder();
        }
        else
        {
            _updateBox.IsVisible = false;
        }
    }
    #endregion

    #region Extension Functions

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

    #endregion Extension Functions

    public void Dispose()
    {
        MenuUI.container.RemoveChild(_updateRequiredBox);

        MenuDashboardUI.mainScrollView.RemoveChild(_updateBox);
        MenuDashboardUI.newAnnouncement = null;
        MenuDashboardUI.ReviseNewsOrder();

        UpdaterCore.OnConfigLoaded -= VersionStatusReady;
    }
}