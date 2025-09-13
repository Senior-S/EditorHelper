using System.Collections.Generic;
using System.IO;
using EditorHelper2.Assets;
using EditorHelper2.common.Helpers;
using EditorHelper2.UI.Builders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SDG.Unturned;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EditorHelper2.UI.Elements
{
    public class MenuBarnsUI
    {
        private static SleekFullscreenBox? _container;

        public static bool Active;
        private readonly ISleekScrollView _menuScrollBox;
        private readonly SleekButtonIcon _backButton;

        // Path to save the selected menu
        private static readonly string SelectedMenuPath = Path.Combine(UnturnedPaths.RootDirectory.FullName, "SelectedMenu.json");
        private const string SelectedMenuKey = "Menu";

        private static Local? _localization;

        public MenuBarnsUI()
        {
            _localization = Localization.read("/Menu/Play/MenuPlaySingleplayer.dat");
            
            Bundle? icons = Bundles.getBundle("/Bundles/Textures/Menu/Icons/MenuDashboard/MenuDashboard.unity3d");
            _container = new SleekFullscreenBox();
            UIBuilder builder = new(-20f, -20f);
            builder.SetOffsetHorizontal(10f)
                .SetOffsetVertical(10f)
                .SetAnchorHorizontal(1f)
                .SetSizeHorizontal(1f)
                .SetScaleVertical(1f);
            builder.FormatElement(ref _container);
            MenuUI.container.AddChild(_container);
            Active = false;

            builder.ResetProperties()
                .SetOffsetVertical(-50f)
                .SetAnchorVertical(1f)
                .SetSizeHorizontal(200)
                .SetSizeVertical(50)
                .SetText("Back");
            
            _backButton = builder.BuildButtonIcon("Back to the main menu", icons.load<Texture2D>("Exit"));
            _backButton.onClickedButton += OnBackButtonClicked;
            _container.AddChild(_backButton);

            // Menu scroll box

            builder.ResetProperties()
                .SetOffsetHorizontal(-200f)
                .SetOffsetVertical(100f)
                .SetAnchorHorizontal(0.5f)
                .SetSizeHorizontal(400f)
                .SetSizeVertical(-200f)
                .SetScaleVertical(1f);

            _menuScrollBox = builder.BuildScrollView(scaleContentToWidth: true);
            _container.AddChild(_menuScrollBox);

            PopulateMenuList();
        }

        public static void Open()
        {
            if (Active) return;
            
            Active = true;
            _container?.AnimateIntoView();
        }

        public static void Close()
        {
            if (!Active) return;
            
            Active = false;
            _container?.AnimateOutOfView(0f, -1f);
        }

        private void OnBackButtonClicked(ISleekElement button)
        {
            Close();
            MenuDashboardUI.open();
            MenuTitleUI.open();
        }

        private void PopulateMenuList()
        {
            // Fetch all scene assets
            List<BarnAsset> barnAssets = [];
            SDG.Unturned.Assets.find(barnAssets);
            UnturnedLog.info($"[MenuBarnsUI] Found {barnAssets.Count} BarnAssets.");

            if (barnAssets.Count == 0)
            {
                UnturnedLog.info("[MenuBarnsUI] No BarnAssets found. Returning.");
                return;
            }

            // Populate the scroll box with SleekBarnItems
            int offsetY = 0;
            foreach (BarnAsset barnAsset in barnAssets)
            {
                UnturnedLog.info($"[MenuBarnsUI] Adding BarnAsset: {barnAsset.BarnName} (GUID: {barnAsset.GUID})");
                SleekBarn barn = new(barnAsset)
                {
                    PositionOffset_Y = offsetY
                };

                if (barnAsset.BarnIcon != null)
                    barn.SetIconTexture(barnAsset.BarnIcon);

                barn.onClickedItem += (_) => OnMenuItemClicked(barnAsset);
                _menuScrollBox.AddChild(barn);

                offsetY += 110;  // Adjust spacing between items
            }

            // Manage Subscriptions button
            UIBuilder builder = new(400f, 30f);
            builder.SetOffsetVertical(offsetY)
                .SetText(_localization?.format("Manage_Workshop_Label") ?? "");
            ISleekButton sleekButton = builder.BuildButton(_localization?.format("Manage_Workshop_Tooltip") ?? "");
            sleekButton.OnClicked += OnClickedManageSubscriptionsButton;
            _menuScrollBox.AddChild(sleekButton);
            offsetY += 40;

            // Browse Workshop button
            builder.ResetProperties()
                .SetOffsetVertical(offsetY)
                .SetSizeHorizontal(400f)
                .SetSizeVertical(30f)
                .SetText("Browse Workshop");
            
            ISleekButton browseButton = builder.BuildButton("Find more barns on the workshop.");
            browseButton.OnClicked += OnClickedBrowseButton;
            _menuScrollBox.AddChild(browseButton);
            offsetY += 40;

            // Reset Barn button
            builder.ResetProperties()
                .SetOffsetVertical(offsetY)
                .SetSizeHorizontal(400f)
                .SetSizeVertical(30f)
                .SetText("Reset Barn");
            ISleekButton resetButton = builder.BuildButton("Use the default barn");
            resetButton.OnClicked += OnClickedResetButton;
            _menuScrollBox.AddChild(resetButton);

            // Update scroll content size
            _menuScrollBox.ContentSizeOffset = new Vector2(0f, offsetY - 10f);
        }

        private void OnMenuItemClicked(BarnAsset barnAsset)
        {
            SaveSelectedMenu(barnAsset);

            // Notify the BarnAssetManager to reload the scene
            BarnAssetManager.LoadSceneFromSavedData();
        }

        private static void SaveSelectedMenu(BarnAsset barnAsset)
        {
            try
            {
                // Create JSON object
                JObject jsonData = new()
                {
                    [SelectedMenuKey] = barnAsset.GUID.ToString()
                };

                // Save to file
                File.WriteAllText(SelectedMenuPath, JsonConvert.SerializeObject(jsonData, Formatting.Indented));
            }
            catch (IOException e)
            {
                UnturnedLog.info($"[MenuBarnsUI] Failed to save selected menu: {e.Message}");
            }
        }
        
        private static void OnClickedManageSubscriptionsButton(ISleekElement button)
        {
            MenuUI.closeAll();
            Close();
            MenuWorkshopSubscriptionsUI.instance.open();
        }
        
        private static void OnClickedResetButton(ISleekElement button)
        {
            try
            {
                if (File.Exists(SelectedMenuPath))
                {
                    File.Delete(SelectedMenuPath);
                }
                else
                {
                    UnturnedLog.info("[MenuBarnsUI] No SelectedMenu.json found to delete.");
                }
            }
            catch (IOException ex)
            {
                UnturnedLog.info($"[MenuBarnsUI] Failed to reset: {ex.Message}");
            }
            SceneManager.LoadScene("Menu");
        }
        
        private static void OnClickedBrowseButton(ISleekElement button)
        {
            Provider.provider.browserService.open("https://steamcommunity.com/workshop/browse/?appid=304930&requiredtags%5B%5D=barn");
        }
    }
}
