using System.Diagnostics;
using System.Runtime.InteropServices;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Helpers;
using EditorHelper2.UI.Builders;
using SDG.Unturned;
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

    public NewsExtension()
    {
        Bundle icons = Bundles.getBundle("/Bundles/Textures/Menu/Icons/MenuPause/MenuPause.unity3d");
        _battlEyeIcon!.Texture = icons.load<Texture2D>("Steam");
        _battlEyeIcon.TintColor = ESleekTint.FOREGROUND;
        icons.unload();
        
        _battlEyeHeaderLabel!.Text = $"You're running EditorHelper2 v{GetType().Assembly.GetName().Version}";
        _battlEyeHeaderLabel.FontSize = ESleekFontSize.Medium;

        string updateAlert = UpdaterCore.IsOutDated
            ? "<size=+2><b>Outdated version, please download the latest version to get the latest features!</b></size>"
            : "You're using the latest version!";
        
        UIBuilder builder = new(0f, 0f);

        // I'll love to don't require this but lately ppl have been reported bugs of outdated version due they don't see the top announce
        // So to assure the best experience for now it will be required to have the latest version.
        if (UpdaterCore.IsOutDated)
        {
            builder.ResetProperties()
                .SetSizeHorizontal(400f)
                .SetSizeVertical(150f)
                .SetAnchorHorizontal(0.5f)
                .SetAnchorVertical(0.5f)
                .SetOffsetHorizontal(-200f)
                .SetOffsetVertical(-75f)
                .SetText(
                    $"You're not using the latest version of the module! This version may contain bugs and issues that can make you lost several hours of your time.\nPlease update to the version {UpdaterCore.LatestVersion} to enjoy the best experience the module have to offer!");

            ISleekBox box = builder.BuildBox();

            builder.ResetProperties()
                .SetAnchorHorizontal(0.5f)
                .SetAnchorVertical(1f)
                .SetOffsetHorizontal(-100f)
                .SetOffsetVertical(5f)
                .SetSizeHorizontal(200f)
                .SetSizeVertical(30f)
                .SetText("Update");

            ISleekButton updateButton = builder.BuildButton("Update your module right now");
            updateButton.OnClicked += (_) =>
            {
                OpenUrl("https://editorhelper.sshost.club/Download");

                Provider.QuitGame("EditorHelper requires an update!");
            };

            box.AddChild(updateButton);
            MenuUI.container.AddChild(box);
        }

        _battlEyeBodyLabel!.AllowRichText = true;
        _battlEyeBodyLabel.Text = $"{updateAlert}\nGet the latest news, releases, and previews of future updates in our discord, click on this alert to join!";
        
        if (UpdaterCore.TryGetTexts(out string updateMessage, out string title, out string subtitle))
        {
            builder.ResetProperties()
                .SetScaleHorizontal(1f);
            
            ISleekBox sleekBox = builder.BuildBox();
            sleekBox.UseManualLayout = false;
            sleekBox.UseChildAutoLayout = ESleekChildLayout.Vertical;
            sleekBox.ChildAutoLayoutPadding = 5f;
            builder.ResetProperties()
                .SetText(title);
            ISleekLabel sleekLabel1 = builder.BuildLabel(TextAnchor.UpperLeft, ESleekFontSize.Large);
            sleekLabel1.UseManualLayout = false;
            sleekBox.AddChild(sleekLabel1);
            builder.ResetProperties()
                .SetText(subtitle);
            ISleekLabel sleekLabel2 = builder.BuildLabel(TextAnchor.UpperLeft, ESleekFontSize.Tiny);
            sleekLabel2.UseManualLayout = false;
            sleekLabel2.TextColor = new SleekColor(ESleekTint.FONT, 0.5f);
            sleekBox.AddChild(sleekLabel2);

            builder.ResetProperties()
                .SetText(updateMessage);
            ISleekLabel sleekLabel3 = builder.BuildLabel(TextAnchor.UpperLeft);
            sleekLabel3.UseManualLayout = false;
            sleekBox.AddChild(sleekLabel3);
            
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
            sleekBox.AddChild(discordButton);
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
            sleekBox.AddChild(youtubeButton);
            
            MenuDashboardUI.mainScrollView.AddChild(sleekBox);
            MenuDashboardUI.newAnnouncement = sleekBox;
            MenuDashboardUI.ReviseNewsOrder();
        }

        if (!UpdaterCore.IsOutDated) return;
        
        MenuUI.instance.escapeMenu();
        MenuPauseUI.close();
    }

    public void Initialize()
    {
    }

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

    #endregion Extension Functions

    public void Dispose()
    {
    }
}