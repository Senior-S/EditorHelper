using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using EditorHelper.Builders;
using EditorHelper.Extras;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace EditorHelper.Patches.Menu.UI;

[HarmonyPatch]
public class MenuDashboardUIPatches
{
    [HarmonyPatch(typeof(MenuDashboardUI), MethodType.Constructor)]
    [HarmonyPrefix]
    [UsedImplicitly]
    private static bool Constructor(MenuDashboardUI __instance)
    {
        if (MenuDashboardUI.icons != null)
        {
            MenuDashboardUI.icons.unload();
        }

        MenuDashboardUI.localization = Localization.read("/Menu/MenuDashboard.dat");
        TransportBase.OnGetMessage = MenuDashboardUI.localization.format;
        MenuDashboardUI.icons = Bundles.getBundle("/Bundles/Textures/Menu/Icons/MenuDashboard/MenuDashboard.unity3d");
        MenuUI.copyNotificationButton.icon = MenuDashboardUI.icons.load<Texture2D>("Clipboard");
        MenuUI.copyNotificationButton.text = MenuDashboardUI.localization.format("Copy_Notification_Label");
        MenuUI.copyNotificationButton.tooltip = MenuDashboardUI.localization.format("Copy_Notification_Tooltip");
        MenuDashboardUI.newAnnouncement = null;
        MenuDashboardUI.workshopBox = null;
        MenuDashboardUI.itemStoreSaleNews = null;
        if (SteamUser.BLoggedOn())
        {
            MenuUI.instance.StartCoroutine(MenuUI.instance.CheckForUpdates(MenuDashboardUI.OnUpdateDetected));
            if (MenuDashboardUI.steamUGCQueryCompletedPopular == null)
            {
                MenuDashboardUI.steamUGCQueryCompletedPopular = CallResult<SteamUGCQueryCompleted_t>.Create(MenuDashboardUI.onSteamUGCQueryCompleted);
            }

            if (MenuDashboardUI.steamUGCQueryCompletedFeatured == null)
            {
                MenuDashboardUI.steamUGCQueryCompletedFeatured =
                    CallResult<SteamUGCQueryCompleted_t>.Create(MenuDashboardUI.onSteamUGCQueryCompleted);
            }

            if (MenuDashboardUI.popularWorkshopHandle != UGCQueryHandle_t.Invalid)
            {
                SteamUGC.ReleaseQueryUGCRequest(MenuDashboardUI.popularWorkshopHandle);
                MenuDashboardUI.popularWorkshopHandle = UGCQueryHandle_t.Invalid;
            }
        }

        MenuDashboardUI.container = new SleekFullscreenBox();
        MenuDashboardUI.container.PositionOffset_X = 10f;
        MenuDashboardUI.container.PositionOffset_Y = 10f;
        MenuDashboardUI.container.SizeOffset_X = -20f;
        MenuDashboardUI.container.SizeOffset_Y = -20f;
        MenuDashboardUI.container.SizeScale_X = 1f;
        MenuDashboardUI.container.SizeScale_Y = 1f;
        MenuUI.container.AddChild(MenuDashboardUI.container);
        MenuDashboardUI.active = true;
        MenuDashboardUI.playButton = new SleekButtonIcon(MenuDashboardUI.icons.load<Texture2D>("Play"));
        MenuDashboardUI.playButton.PositionOffset_Y = 170f;
        MenuDashboardUI.playButton.SizeOffset_X = 200f;
        MenuDashboardUI.playButton.SizeOffset_Y = 50f;
        MenuDashboardUI.playButton.text = MenuDashboardUI.localization.format("PlayButtonText");
        MenuDashboardUI.playButton.tooltip = MenuDashboardUI.localization.format("PlayButtonTooltip");
        MenuDashboardUI.playButton.onClickedButton += MenuDashboardUI.onClickedPlayButton;
        MenuDashboardUI.playButton.fontSize = ESleekFontSize.Medium;
        MenuDashboardUI.playButton.iconColor = ESleekTint.FOREGROUND;
        MenuDashboardUI.container.AddChild(MenuDashboardUI.playButton);
        MenuDashboardUI.survivorsButton = new SleekButtonIcon(MenuDashboardUI.icons.load<Texture2D>("Survivors"));
        MenuDashboardUI.survivorsButton.PositionOffset_Y = 230f;
        MenuDashboardUI.survivorsButton.SizeOffset_X = 200f;
        MenuDashboardUI.survivorsButton.SizeOffset_Y = 50f;
        MenuDashboardUI.survivorsButton.text = MenuDashboardUI.localization.format("SurvivorsButtonText");
        MenuDashboardUI.survivorsButton.tooltip = MenuDashboardUI.localization.format("SurvivorsButtonTooltip");
        MenuDashboardUI.survivorsButton.onClickedButton += MenuDashboardUI.onClickedSurvivorsButton;
        MenuDashboardUI.survivorsButton.fontSize = ESleekFontSize.Medium;
        MenuDashboardUI.survivorsButton.iconColor = ESleekTint.FOREGROUND;
        MenuDashboardUI.container.AddChild(MenuDashboardUI.survivorsButton);
        MenuDashboardUI.configurationButton = new SleekButtonIcon(MenuDashboardUI.icons.load<Texture2D>("Configuration"));
        MenuDashboardUI.configurationButton.PositionOffset_Y = 290f;
        MenuDashboardUI.configurationButton.SizeOffset_X = 200f;
        MenuDashboardUI.configurationButton.SizeOffset_Y = 50f;
        MenuDashboardUI.configurationButton.text = MenuDashboardUI.localization.format("ConfigurationButtonText");
        MenuDashboardUI.configurationButton.tooltip = MenuDashboardUI.localization.format("ConfigurationButtonTooltip");
        MenuDashboardUI.configurationButton.onClickedButton += MenuDashboardUI.onClickedConfigurationButton;
        MenuDashboardUI.configurationButton.fontSize = ESleekFontSize.Medium;
        MenuDashboardUI.configurationButton.iconColor = ESleekTint.FOREGROUND;
        MenuDashboardUI.container.AddChild(MenuDashboardUI.configurationButton);
        MenuDashboardUI.workshopButton = new SleekButtonIcon(MenuDashboardUI.icons.load<Texture2D>("Workshop"));
        MenuDashboardUI.workshopButton.PositionOffset_Y = 350f;
        MenuDashboardUI.workshopButton.SizeOffset_X = 200f;
        MenuDashboardUI.workshopButton.SizeOffset_Y = 50f;
        MenuDashboardUI.workshopButton.text = MenuDashboardUI.localization.format("WorkshopButtonText");
        MenuDashboardUI.workshopButton.tooltip = MenuDashboardUI.localization.format("WorkshopButtonTooltip");
        MenuDashboardUI.workshopButton.onClickedButton += MenuDashboardUI.onClickedWorkshopButton;
        MenuDashboardUI.workshopButton.fontSize = ESleekFontSize.Medium;
        MenuDashboardUI.workshopButton.iconColor = ESleekTint.FOREGROUND;
        MenuDashboardUI.container.AddChild(MenuDashboardUI.workshopButton);
        MenuDashboardUI.exitButton = new SleekButtonIcon(MenuDashboardUI.icons.load<Texture2D>("Exit"));
        MenuDashboardUI.exitButton.PositionOffset_Y = -50f;
        MenuDashboardUI.exitButton.PositionScale_Y = 1f;
        MenuDashboardUI.exitButton.SizeOffset_X = 200f;
        MenuDashboardUI.exitButton.SizeOffset_Y = 50f;
        MenuDashboardUI.exitButton.text = MenuDashboardUI.localization.format("ExitButtonText");
        MenuDashboardUI.exitButton.tooltip = MenuDashboardUI.localization.format("ExitButtonTooltip");
        MenuDashboardUI.exitButton.onClickedButton += MenuDashboardUI.onClickedExitButton;
        MenuDashboardUI.exitButton.fontSize = ESleekFontSize.Medium;
        MenuDashboardUI.exitButton.iconColor = ESleekTint.FOREGROUND;
        MenuDashboardUI.container.AddChild(MenuDashboardUI.exitButton);
        MenuDashboardUI.mainScrollView = Glazier.Get().CreateScrollView();
        MenuDashboardUI.mainScrollView.PositionOffset_X = 210f;
        MenuDashboardUI.mainScrollView.PositionOffset_Y = 170f;
        MenuDashboardUI.mainScrollView.SizeScale_X = 1f;
        MenuDashboardUI.mainScrollView.SizeScale_Y = 1f;
        MenuDashboardUI.mainScrollView.SizeOffset_X = -210f;
        MenuDashboardUI.mainScrollView.SizeOffset_Y = -170f;
        MenuDashboardUI.mainScrollView.ScaleContentToWidth = true;
        MenuDashboardUI.container.AddChild(MenuDashboardUI.mainScrollView);
        if (!Glazier.Get().SupportsAutomaticLayout)
        {
            ISleekLabel sleekLabel = Glazier.Get().CreateLabel();
            sleekLabel.Text = "Featured workshop file and news feed are no longer supported when using the -Glazier=IMGUI launch option.";
            sleekLabel.FontSize = ESleekFontSize.Large;
            sleekLabel.SizeScale_X = 1f;
            sleekLabel.SizeOffset_Y = 100f;
            MenuDashboardUI.mainScrollView.ContentSizeOffset = new Vector2(0f, 100f);
            MenuDashboardUI.mainScrollView.AddChild(sleekLabel);
        }
        else
        {
            MenuDashboardUI.mainScrollView.ContentUseManualLayout = false;
        }

        if (!Provider.isPro)
        {
            MenuDashboardUI.proButton = Glazier.Get().CreateButton();
            MenuDashboardUI.proButton.PositionOffset_X = 210f;
            MenuDashboardUI.proButton.PositionOffset_Y = -100f;
            MenuDashboardUI.proButton.PositionScale_Y = 1f;
            MenuDashboardUI.proButton.SizeOffset_Y = 100f;
            MenuDashboardUI.proButton.SizeOffset_X = -210f;
            MenuDashboardUI.proButton.SizeScale_X = 1f;
            MenuDashboardUI.proButton.TooltipText = MenuDashboardUI.localization.format("Pro_Button_Tooltip");
            MenuDashboardUI.proButton.BackgroundColor = SleekColor.BackgroundIfLight(Palette.PRO);
            MenuDashboardUI.proButton.TextColor = Palette.PRO;
            MenuDashboardUI.proButton.OnClicked += MenuDashboardUI.onClickedProButton;
            MenuDashboardUI.container.AddChild(MenuDashboardUI.proButton);
            MenuDashboardUI.proLabel = Glazier.Get().CreateLabel();
            MenuDashboardUI.proLabel.SizeScale_X = 1f;
            MenuDashboardUI.proLabel.SizeOffset_Y = 50f;
            MenuDashboardUI.proLabel.Text = MenuDashboardUI.localization.format("Pro_Title");
            MenuDashboardUI.proLabel.TextColor = Palette.PRO;
            MenuDashboardUI.proLabel.FontSize = ESleekFontSize.Large;
            MenuDashboardUI.proButton.AddChild(MenuDashboardUI.proLabel);
            MenuDashboardUI.featureLabel = Glazier.Get().CreateLabel();
            MenuDashboardUI.featureLabel.PositionOffset_Y = 50f;
            MenuDashboardUI.featureLabel.SizeOffset_Y = -50f;
            MenuDashboardUI.featureLabel.SizeScale_X = 1f;
            MenuDashboardUI.featureLabel.SizeScale_Y = 1f;
            MenuDashboardUI.featureLabel.Text = MenuDashboardUI.localization.format("Pro_Button");
            MenuDashboardUI.featureLabel.TextColor = Palette.PRO;
            MenuDashboardUI.proButton.AddChild(MenuDashboardUI.featureLabel);
            MenuDashboardUI.mainScrollView.SizeOffset_Y -= 110f;
        }

        MenuDashboardUI.mainHeaderOffset = 170f;
        MenuDashboardUI.alertBox = null;
        MenuDashboardUI.hasCreatedItemStoreButton = false;
        if (SteamApps.GetCurrentBetaName(out string pchName, 64) && string.Equals(pchName, "preview", StringComparison.InvariantCultureIgnoreCase))
        {
            __instance.CreatePreviewBranchChangelogButton();
        }

        if (!Dedicator.hasBattlEye)
        {
            Bundle icons = Bundles.getBundle("/Bundles/Textures/Menu/Icons/MenuPause/MenuPause.unity3d");
            MenuDashboardUI.battlEyeButton = Glazier.Get().CreateButton();
            MenuDashboardUI.battlEyeButton.PositionOffset_X = 210f;
            MenuDashboardUI.battlEyeButton.PositionOffset_Y = MenuDashboardUI.mainHeaderOffset;
            MenuDashboardUI.battlEyeButton.SizeOffset_Y = 60f;
            MenuDashboardUI.battlEyeButton.SizeOffset_X = -210f;
            MenuDashboardUI.battlEyeButton.SizeScale_X = 1f;
            MenuDashboardUI.battlEyeButton.OnClicked += __instance.OnClickedBattlEyeButton;
            MenuDashboardUI.container.AddChild(MenuDashboardUI.battlEyeButton);
            MenuDashboardUI.battlEyeIcon = Glazier.Get().CreateImage();
            MenuDashboardUI.battlEyeIcon.PositionOffset_X = 10f;
            MenuDashboardUI.battlEyeIcon.PositionOffset_Y = 10f;
            MenuDashboardUI.battlEyeIcon.SizeOffset_X = 40f;
            MenuDashboardUI.battlEyeIcon.SizeOffset_Y = 40f;
            MenuDashboardUI.battlEyeIcon.TintColor = ESleekTint.FOREGROUND;
            MenuDashboardUI.battlEyeIcon.Texture = icons.load<Texture2D>("Steam");
            MenuDashboardUI.battlEyeButton.AddChild(MenuDashboardUI.battlEyeIcon);
            MenuDashboardUI.battlEyeHeaderLabel = Glazier.Get().CreateLabel();
            MenuDashboardUI.battlEyeHeaderLabel.PositionOffset_X = 60f;
            MenuDashboardUI.battlEyeHeaderLabel.SizeScale_X = 1f;
            MenuDashboardUI.battlEyeHeaderLabel.SizeOffset_X = -60f;
            MenuDashboardUI.battlEyeHeaderLabel.SizeOffset_Y = 30f;

            #region Discord Alert

            MenuDashboardUI.battlEyeHeaderLabel.Text = $"You're running Editor Helper v{EditorHelper.Instance.GetType().Assembly.GetName().Version}";
            MenuDashboardUI.battlEyeHeaderLabel.FontSize = ESleekFontSize.Medium;
            MenuDashboardUI.battlEyeButton.AddChild(MenuDashboardUI.battlEyeHeaderLabel);
            MenuDashboardUI.battlEyeBodyLabel = Glazier.Get().CreateLabel();
            MenuDashboardUI.battlEyeBodyLabel.PositionOffset_X = 60f;
            MenuDashboardUI.battlEyeBodyLabel.PositionOffset_Y = 20f;
            MenuDashboardUI.battlEyeBodyLabel.SizeOffset_X = -60f;
            MenuDashboardUI.battlEyeBodyLabel.SizeOffset_Y = -20f;
            MenuDashboardUI.battlEyeBodyLabel.SizeScale_X = 1f;
            MenuDashboardUI.battlEyeBodyLabel.SizeScale_Y = 1f;

            string updateAlert = UpdaterCore.IsOutDated ? "<size=+2><b>Outdated version, please download the latest version to get the latest features!</b></size>" : "You're using the latest version!";

            // I'll love to don't require this but lately ppl have been reported bugs of outdated version due they don't see the top announce
            // So to assure the best experience for now it will be required to have the latest version.
            if (UpdaterCore.IsOutDated)
            {
                UIBuilder builder = new();

                builder.SetPositionScaleX(0.5f)
                    .SetPositionScaleY(0.5f)
                    .SetSizeOffsetX(400f)
                    .SetSizeOffsetY(150f)
                    .SetPositionOffsetX(-200f)
                    .SetPositionOffsetY(-75f)
                    .SetText($"You're not using the latest version of the module! This version may contain bugs and issues that can make you lost several hours of your time.\nPlease update to the version {UpdaterCore.LatestVersion} to enjoy the best experience the module have to offer!");

                ISleekBox box = builder.BuildBox();

                builder.SetPositionScaleY(1f)
                    .SetOneTimeSpacing(0f)
                    .SetPositionOffsetX(-100f)
                    .SetPositionOffsetY(5f)
                    .SetSizeOffsetX(200f)
                    .SetSizeOffsetY(30f)
                    .SetText("Update");

                SleekButtonIcon updateButton = builder.BuildButton("Update your module right now");
                updateButton.onClickedButton += (_) =>
                {
                    OpenUrl("https://editorhelper.sshost.club/Download");
                    
                    Provider.QuitGame("EditorHelper required an update!");
                };
                
                box.AddChild(updateButton);
                MenuUI.container.AddChild(box);
            }
            
            MenuDashboardUI.battlEyeBodyLabel.AllowRichText = true;
            MenuDashboardUI.battlEyeBodyLabel.Text = $"{updateAlert}\nGet the latest news, releases, and previews of future updates on my discord, click on this alert to join!";
            #endregion

            MenuDashboardUI.battlEyeButton.AddChild(MenuDashboardUI.battlEyeBodyLabel);
            MenuDashboardUI.mainHeaderOffset += MenuDashboardUI.battlEyeButton.SizeOffset_Y + 10f;
            MenuDashboardUI.mainScrollView.PositionOffset_Y += MenuDashboardUI.battlEyeButton.SizeOffset_Y + 10f;
            MenuDashboardUI.mainScrollView.SizeOffset_Y -= MenuDashboardUI.battlEyeButton.SizeOffset_Y + 10f;
            icons.unload();
        }

        ItemStore.Get().OnPricesReceived += MenuDashboardUI.OnPricesReceived;
        MenuDashboardUI.hasBegunQueryingLiveConfigWorkshop = false;
        LiveConfig.OnRefreshed += MenuDashboardUI.OnLiveConfigRefreshed;
        MenuDashboardUI.OnLiveConfigRefreshed();
        __instance.pauseUI = new MenuPauseUI();
        __instance.creditsUI = new MenuCreditsUI();
        __instance.titleUI = new MenuTitleUI();
        __instance.playUI = new MenuPlayUI();
        __instance.survivorsUI = new MenuSurvivorsUI();
        __instance.configUI = new MenuConfigurationUI();
        __instance.workshopUI = new MenuWorkshopUI();
        if (Provider.connectionFailureInfo != 0)
        {
            ESteamConnectionFailureInfo eSteamConnectionFailureInfo = Provider.connectionFailureInfo;
            string connectionFailureReason = Provider.connectionFailureReason;
            uint connectionFailureDuration = Provider.connectionFailureDuration;
            int serverInvalidItemsCount = Provider.provider.workshopService.serverInvalidItemsCount;
            Provider.resetConnectionFailure();
            Provider.provider.workshopService.resetServerInvalidItems();
            if (serverInvalidItemsCount > 0 &&
                ((eSteamConnectionFailureInfo == ESteamConnectionFailureInfo.MAP ||
                  eSteamConnectionFailureInfo == ESteamConnectionFailureInfo.HASH_LEVEL || (uint)(eSteamConnectionFailureInfo - 43) <= 2u)
                    ? true
                    : false))
            {
                UnturnedLog.info(
                    "Connection failure {0} is asset related and therefore probably caused by the {1} download-restricted workshop item(s)",
                    eSteamConnectionFailureInfo, serverInvalidItemsCount);
                eSteamConnectionFailureInfo = ESteamConnectionFailureInfo.WORKSHOP_DOWNLOAD_RESTRICTION;
            }

            string text = null;
            text = eSteamConnectionFailureInfo switch
            {
                ESteamConnectionFailureInfo.BANNED => MenuDashboardUI.localization.format("Banned", connectionFailureDuration,
                    connectionFailureReason),
                ESteamConnectionFailureInfo.KICKED => MenuDashboardUI.localization.format("Kicked", connectionFailureReason),
                ESteamConnectionFailureInfo.WHITELISTED => MenuDashboardUI.localization.format("Whitelisted"),
                ESteamConnectionFailureInfo.PASSWORD => MenuDashboardUI.localization.format("Password"),
                ESteamConnectionFailureInfo.FULL => MenuDashboardUI.localization.format("Full"),
                ESteamConnectionFailureInfo.HASH_LEVEL => MenuDashboardUI.localization.format("Hash_Level"),
                ESteamConnectionFailureInfo.HASH_ASSEMBLY => MenuDashboardUI.localization.format("Hash_Assembly"),
                ESteamConnectionFailureInfo.VERSION => MenuDashboardUI.localization.format("Version", connectionFailureReason, Provider.APP_VERSION),
                ESteamConnectionFailureInfo.PRO_SERVER => MenuDashboardUI.localization.format("Pro_Server"),
                ESteamConnectionFailureInfo.PRO_CHARACTER => MenuDashboardUI.localization.format("Pro_Character"),
                ESteamConnectionFailureInfo.PRO_DESYNC => MenuDashboardUI.localization.format("Pro_Desync"),
                ESteamConnectionFailureInfo.PRO_APPEARANCE => MenuDashboardUI.localization.format("Pro_Appearance"),
                ESteamConnectionFailureInfo.AUTH_VERIFICATION => MenuDashboardUI.localization.format("Auth_Verification"),
                ESteamConnectionFailureInfo.AUTH_NO_STEAM => MenuDashboardUI.localization.format("Auth_No_Steam"),
                ESteamConnectionFailureInfo.AUTH_LICENSE_EXPIRED => MenuDashboardUI.localization.format("Auth_License_Expired"),
                ESteamConnectionFailureInfo.AUTH_VAC_BAN => MenuDashboardUI.localization.format("Auth_VAC_Ban"),
                ESteamConnectionFailureInfo.AUTH_ELSEWHERE => MenuDashboardUI.localization.format("Auth_Elsewhere"),
                ESteamConnectionFailureInfo.AUTH_TIMED_OUT => MenuDashboardUI.localization.format("Auth_Timed_Out"),
                ESteamConnectionFailureInfo.AUTH_USED => MenuDashboardUI.localization.format("Auth_Used"),
                ESteamConnectionFailureInfo.AUTH_NO_USER => MenuDashboardUI.localization.format("Auth_No_User"),
                ESteamConnectionFailureInfo.AUTH_PUB_BAN => MenuDashboardUI.localization.format("Auth_Pub_Ban"),
                ESteamConnectionFailureInfo.AUTH_NETWORK_IDENTITY_FAILURE => MenuDashboardUI.localization.format("Auth_Network_Identity_Failure"),
                ESteamConnectionFailureInfo.AUTH_ECON_SERIALIZE => MenuDashboardUI.localization.format("Auth_Econ_Serialize"),
                ESteamConnectionFailureInfo.AUTH_ECON_DESERIALIZE => MenuDashboardUI.localization.format("Auth_Econ_Deserialize"),
                ESteamConnectionFailureInfo.AUTH_ECON_VERIFY => MenuDashboardUI.localization.format("Auth_Econ_Verify"),
                ESteamConnectionFailureInfo.AUTH_EMPTY => MenuDashboardUI.localization.format("Auth_Empty"),
                ESteamConnectionFailureInfo.ALREADY_CONNECTED => MenuDashboardUI.localization.format("Already_Connected"),
                ESteamConnectionFailureInfo.ALREADY_PENDING => MenuDashboardUI.localization.format("Already_Pending"),
                ESteamConnectionFailureInfo.LATE_PENDING => MenuDashboardUI.localization.format("Late_Pending"),
                ESteamConnectionFailureInfo.NOT_PENDING => MenuDashboardUI.localization.format("Not_Pending"),
                ESteamConnectionFailureInfo.NAME_PLAYER_SHORT => MenuDashboardUI.localization.format("Name_Player_Short"),
                ESteamConnectionFailureInfo.NAME_PLAYER_LONG => MenuDashboardUI.localization.format("Name_Player_Long"),
                ESteamConnectionFailureInfo.NAME_PLAYER_INVALID => MenuDashboardUI.localization.format("Name_Player_Invalid"),
                ESteamConnectionFailureInfo.NAME_PLAYER_NUMBER => MenuDashboardUI.localization.format("Name_Player_Number"),
                ESteamConnectionFailureInfo.NAME_CHARACTER_SHORT => MenuDashboardUI.localization.format("Name_Character_Short"),
                ESteamConnectionFailureInfo.NAME_CHARACTER_LONG => MenuDashboardUI.localization.format("Name_Character_Long"),
                ESteamConnectionFailureInfo.NAME_CHARACTER_INVALID => MenuDashboardUI.localization.format("Name_Character_Invalid"),
                ESteamConnectionFailureInfo.NAME_CHARACTER_NUMBER => MenuDashboardUI.localization.format("Name_Character_Number"),
                ESteamConnectionFailureInfo.TIMED_OUT => MenuDashboardUI.localization.format("Timed_Out"),
                ESteamConnectionFailureInfo.TIMED_OUT_LOGIN => MenuDashboardUI.localization.format("Timed_Out_Login"),
                ESteamConnectionFailureInfo.MAP => MenuDashboardUI.localization.format("Map"),
                ESteamConnectionFailureInfo.SHUTDOWN => string.IsNullOrEmpty(connectionFailureReason)
                    ? MenuDashboardUI.localization.format("Shutdown")
                    : MenuDashboardUI.localization.format("Shutdown_Reason", connectionFailureReason),
                ESteamConnectionFailureInfo.PING => connectionFailureReason,
                ESteamConnectionFailureInfo.PLUGIN => string.IsNullOrEmpty(connectionFailureReason)
                    ? MenuDashboardUI.localization.format("Plugin")
                    : MenuDashboardUI.localization.format("Plugin_Reason", connectionFailureReason),
                ESteamConnectionFailureInfo.BARRICADE => MenuDashboardUI.localization.format("Barricade", connectionFailureReason),
                ESteamConnectionFailureInfo.STRUCTURE => MenuDashboardUI.localization.format("Structure", connectionFailureReason),
                ESteamConnectionFailureInfo.VEHICLE => MenuDashboardUI.localization.format("Vehicle", connectionFailureReason),
                ESteamConnectionFailureInfo.CLIENT_MODULE_DESYNC => MenuDashboardUI.localization.format("Client_Module_Desync"),
                ESteamConnectionFailureInfo.SERVER_MODULE_DESYNC => MenuDashboardUI.localization.format("Server_Module_Desync"),
                ESteamConnectionFailureInfo.BATTLEYE_BROKEN => MenuDashboardUI.localization.format("BattlEye_Broken"),
                ESteamConnectionFailureInfo.BATTLEYE_UPDATE => MenuDashboardUI.localization.format("BattlEye_Update"),
                ESteamConnectionFailureInfo.BATTLEYE_UNKNOWN => MenuDashboardUI.localization.format("BattlEye_Unknown"),
                ESteamConnectionFailureInfo.LEVEL_VERSION => connectionFailureReason,
                ESteamConnectionFailureInfo.ECON_HASH => MenuDashboardUI.localization.format("Econ_Hash"),
                ESteamConnectionFailureInfo.HASH_MASTER_BUNDLE => MenuDashboardUI.localization.format("Master_Bundle_Hash", connectionFailureReason),
                ESteamConnectionFailureInfo.REJECT_UNKNOWN => MenuDashboardUI.localization.format("Reject_Unknown", connectionFailureReason),
                ESteamConnectionFailureInfo.WORKSHOP_DOWNLOAD_RESTRICTION => MenuDashboardUI.localization.format("Workshop_Download_Restriction",
                    serverInvalidItemsCount),
                ESteamConnectionFailureInfo.WORKSHOP_ADVERTISEMENT_MISMATCH => MenuDashboardUI.localization.format("Workshop_Advertisement_Mismatch"),
                ESteamConnectionFailureInfo.CUSTOM => connectionFailureReason,
                ESteamConnectionFailureInfo.LATE_PENDING_STEAM_AUTH => MenuDashboardUI.localization.format("Late_Pending_Steam_Auth"),
                ESteamConnectionFailureInfo.LATE_PENDING_STEAM_ECON => MenuDashboardUI.localization.format("Late_Pending_Steam_Econ"),
                ESteamConnectionFailureInfo.LATE_PENDING_STEAM_GROUPS => MenuDashboardUI.localization.format("Late_Pending_Steam_Groups"),
                ESteamConnectionFailureInfo.NAME_PRIVATE_LONG => MenuDashboardUI.localization.format("Name_Private_Long"),
                ESteamConnectionFailureInfo.NAME_PRIVATE_INVALID => MenuDashboardUI.localization.format("Name_Private_Invalid"),
                ESteamConnectionFailureInfo.NAME_PRIVATE_NUMBER => MenuDashboardUI.localization.format("Name_Private_Number"),
                ESteamConnectionFailureInfo.HASH_RESOURCES => MenuDashboardUI.localization.format("Hash_Resources"),
                ESteamConnectionFailureInfo.SKIN_COLOR_WITHIN_THRESHOLD_OF_TERRAIN_COLOR => MenuDashboardUI.localization.format(
                    "SkinColorWithinThresholdOfTerrainColor"),
                ESteamConnectionFailureInfo.STEAM_ID_MISMATCH => MenuDashboardUI.localization.format("Steam_ID_Mismatch"),
                ESteamConnectionFailureInfo.CONNECT_RATE_LIMITING => MenuDashboardUI.localization.format("Connect_Rate_Limiting"),
                ESteamConnectionFailureInfo.BAD_PACKET_RATE_LIMITING => MenuDashboardUI.localization.format("Bad_Packet_Rate_Limiting"),
                ESteamConnectionFailureInfo.SERVER_MAP_ADVERTISEMENT_MISMATCH => MenuDashboardUI.localization.format(
                    "Server_Map_Advertisement_Mismatch"),
                ESteamConnectionFailureInfo.SERVER_VAC_ADVERTISEMENT_MISMATCH => MenuDashboardUI.localization.format(
                    "Server_VAC_Advertisement_Mismatch"),
                ESteamConnectionFailureInfo.SERVER_BATTLEYE_ADVERTISEMENT_MISMATCH => MenuDashboardUI.localization.format(
                    "Server_BattlEye_Advertisement_Mismatch"),
                ESteamConnectionFailureInfo.SERVER_MAXPLAYERS_ADVERTISEMENT_MISMATCH => MenuDashboardUI.localization.format(
                    "Server_MaxPlayers_Advertisement_Mismatch"),
                ESteamConnectionFailureInfo.SERVER_CAMERAMODE_ADVERTISEMENT_MISMATCH => MenuDashboardUI.localization.format(
                    "Server_CameraMode_Advertisement_Mismatch"),
                ESteamConnectionFailureInfo.SERVER_PVP_ADVERTISEMENT_MISMATCH => MenuDashboardUI.localization.format(
                    "Server_PvP_Advertisement_Mismatch"),
                ESteamConnectionFailureInfo.HWID_MODIFIED => MenuDashboardUI.localization.format("HWID_Modified"),
                _ => MenuDashboardUI.localization.format("Failure_Unknown", eSteamConnectionFailureInfo, connectionFailureReason),
            };
            if (string.IsNullOrEmpty(text))
            {
                text = $"Error: {eSteamConnectionFailureInfo} Reason: {connectionFailureReason}";
            }

            MenuUI.alert(text);
            UnturnedLog.info(text);
        }

        #region Update

        if (UpdaterCore.TryGetTexts(out string updateMessage, out string title, out string subtitle))
        {
            ISleekBox sleekBox = Glazier.Get().CreateBox();
            sleekBox.SizeScale_X = 1f;
            sleekBox.UseManualLayout = false;
            sleekBox.UseChildAutoLayout = ESleekChildLayout.Vertical;
            sleekBox.ChildAutoLayoutPadding = 5f;
            ISleekLabel sleekLabel1 = Glazier.Get().CreateLabel();
            sleekLabel1.Text = title;
            sleekLabel1.UseManualLayout = false;
            sleekLabel1.TextAlignment = TextAnchor.UpperLeft;
            sleekLabel1.FontSize = ESleekFontSize.Large;
            sleekBox.AddChild(sleekLabel1);
            ISleekLabel sleekLabel2 = Glazier.Get().CreateLabel();
            sleekLabel2.Text = subtitle;
            sleekLabel2.UseManualLayout = false;
            sleekLabel2.TextAlignment = TextAnchor.UpperLeft;
            sleekLabel2.FontSize = ESleekFontSize.Tiny;
            sleekLabel2.TextColor = new SleekColor(ESleekTint.FONT, 0.5f);
            sleekBox.AddChild(sleekLabel2);

            ISleekLabel sleekLabel3 = Glazier.Get().CreateLabel();
            sleekLabel3.Text = updateMessage;
            sleekLabel3.UseManualLayout = false;
            sleekLabel3.AllowRichText = true;
            sleekLabel3.TextAlignment = TextAnchor.UpperLeft;
            sleekBox.AddChild(sleekLabel3);

            
            SleekWebLinkButton discordButton = new();
            discordButton.Text = "EditorHelper discord";
            discordButton.Url = "https://discord.gg/Y3jD5K2Q8C";
            discordButton.UseManualLayout = false;
            discordButton.UseChildAutoLayout = ESleekChildLayout.Vertical;
            discordButton.UseHeightLayoutOverride = true;
            discordButton.ExpandChildren = true;
            discordButton.SizeOffset_Y = 30f;
            sleekBox.AddChild(discordButton);
            SleekWebLinkButton youtubeButton = new();
            youtubeButton.Text = "SSPlugins Youtube Channel";
            youtubeButton.Url = "https://www.youtube.com/@ssplugins4783/featured";
            youtubeButton.UseManualLayout = false;
            youtubeButton.UseChildAutoLayout = ESleekChildLayout.Vertical;
            youtubeButton.UseHeightLayoutOverride = true;
            youtubeButton.ExpandChildren = true;
            youtubeButton.SizeOffset_Y = 30f;
            sleekBox.AddChild(youtubeButton);
            
            MenuDashboardUI.mainScrollView.AddChild(sleekBox);
            MenuDashboardUI.newAnnouncement = sleekBox;
            MenuDashboardUI.ReviseNewsOrder();
        }

        #endregion
        
        if (UpdaterCore.IsOutDated)
        {
            MenuUI.instance.escapeMenu();
            MenuPauseUI.close();
        }
        return false;
    }

    [HarmonyPatch(typeof(MenuDashboardUI), "OnClickedBattlEyeButton")]
    [HarmonyPostfix]
    [HarmonyPrefix]
    private static bool OnClickedBattlEyeButton(ISleekElement element)
    {
        Provider.provider.browserService.open("https://discord.gg/Y3jD5K2Q8C");

        return false;
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
}