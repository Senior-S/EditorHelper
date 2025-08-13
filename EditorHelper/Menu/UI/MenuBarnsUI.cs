using SDG.Unturned;
using System.Collections.Generic;
using System.IO;
using Breakdown.Module.Editor.UI;
using Newtonsoft.Json.Linq;
using EditorHelper.CustomAssets;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EditorHelper.Menu.UI
{
    public class MenuBarnsUI
    {
        private static SleekFullscreenBox container;
        public static Bundle icons;

        public static bool active;
        private ISleekScrollView menuScrollBox;
        private SleekButtonIcon backButton;
        private SleekBarn[] menuItems;

        // Path to save the selected menu
        private static readonly string SelectedMenuPath = Path.Combine(UnturnedPaths.RootDirectory.FullName, "SelectedMenu.json");
        private const string SelectedMenuKey = "Menu";
        
        public static Local localization;

        public MenuBarnsUI()
        {
            localization = Localization.read("/Menu/Play/MenuPlaySingleplayer.dat");
            container = new SleekFullscreenBox();
            icons = Bundles.getBundle("/Bundles/Textures/Menu/Icons/MenuDashboard/MenuDashboard.unity3d");

            container.PositionOffset_X = 10f;
            container.PositionOffset_Y = 10f;
            container.PositionScale_Y = 1f;
            container.SizeOffset_X = -20f;
            container.SizeOffset_Y = -20f;
            container.SizeScale_X = 1f;
            container.SizeScale_Y = 1f;
            MenuUI.container.AddChild(container);
            active = false;

            backButton = new SleekButtonIcon(icons.load<Texture2D>("Exit"));
            backButton.PositionOffset_Y = -50f;
            backButton.PositionScale_Y = 1f;
            backButton.SizeOffset_X = 200;
            backButton.SizeOffset_Y = 50;
            backButton.text = "Back";
            backButton.onClickedButton += OnBackButtonClicked;
            container.AddChild(backButton);

            // Menu scroll box
            menuScrollBox = Glazier.Get().CreateScrollView();
            menuScrollBox.PositionOffset_X = -200f;
            menuScrollBox.PositionOffset_Y = 100f;
            menuScrollBox.PositionScale_X = 0.5f;
            menuScrollBox.SizeOffset_X = 400f;
            menuScrollBox.SizeOffset_Y = -200f;
            menuScrollBox.SizeScale_Y = 1f;
            menuScrollBox.ScaleContentToWidth = true;
            container.AddChild(menuScrollBox);

            PopulateMenuList();
        }

        public static void open()
        {
            if (!active)
            {
                active = true;
                container.AnimateIntoView();
            }
        }

        public static void close()
        {
            if (active)
            {
                active = false;
                container.AnimateOutOfView(0f, -1f);
            }
        }

        private void OnBackButtonClicked(ISleekElement button)
        {
            close();
            MenuDashboardUI.open();
            MenuTitleUI.open();
        }

        private void PopulateMenuList()
        {
            // Fetch all scene assets
            List<SceneAsset> sceneAssets = new List<SceneAsset>();
            Assets.find(sceneAssets);

            if (sceneAssets.Count == 0)
            {
                return;
            }

            // Populate the scroll box with SleekBarnItems
            int offsetY = 0;
            foreach (SceneAsset sceneAsset in sceneAssets)
            {
                SleekBarn barn = new SleekBarn(sceneAsset);
                barn.PositionOffset_Y = offsetY;
                
                barn.SetIconTexture(sceneAsset.BarnIcon);
                
                barn.onClickedItem += (item) => OnMenuItemClicked(sceneAsset);
                menuScrollBox.AddChild(barn);

                offsetY += 110;  // Adjust spacing between items
            }
            
            ISleekButton sleekButton = Glazier.Get().CreateButton();
            sleekButton.PositionOffset_Y = offsetY;
            sleekButton.SizeOffset_X = 400f;
            sleekButton.SizeOffset_Y = 30f;
            sleekButton.Text = localization.format("Manage_Workshop_Label");
            sleekButton.TooltipText = localization.format("Manage_Workshop_Tooltip");
            sleekButton.OnClicked += onClickedManageSubscriptionsButton;
            menuScrollBox.AddChild(sleekButton);
            offsetY += 40;
            
            ISleekButton resetButton = Glazier.Get().CreateButton();
            resetButton.PositionOffset_Y = offsetY;
            resetButton.SizeOffset_X = 400f;
            resetButton.SizeOffset_Y = 30f;
            resetButton.Text = "Reset Barn";
            resetButton.TooltipText = "Use the default barn.";
            resetButton.OnClicked += onClickedResetButton;
            menuScrollBox.AddChild(resetButton);

            // Update scroll content size
            menuScrollBox.ContentSizeOffset = new Vector2(0f, offsetY - 10f);
        }

        private void OnMenuItemClicked(SceneAsset sceneAsset)
        {
            UnturnedLog.info($"Menu '{sceneAsset.BarnName}' selected!");
            SaveSelectedMenu(sceneAsset);

            // Notify the BarnAssetManager to reload the scene
            BarnAssetManager.LoadSceneFromSavedData();
        }

        private static void SaveSelectedMenu(SceneAsset sceneAsset)
        {
            try
            {
                // Create JSON object
                var jsonData = new JObject
                {
                    [SelectedMenuKey] = sceneAsset.GUID.ToString()
                };

                // Save to file
                File.WriteAllText(SelectedMenuPath, jsonData.ToString(Formatting.Indented));
                UnturnedLog.info($"Saved selected menu: {sceneAsset.BarnName}");
            }
            catch (IOException e)
            {
                UnturnedLog.info($"Failed to save selected menu: {e.Message}");
            }
        }
        
        private static void onClickedManageSubscriptionsButton(ISleekElement button)
        {
            MenuUI.closeAll();
            close();
            MenuWorkshopSubscriptionsUI.instance.open();
        }
        
        private static void onClickedResetButton(ISleekElement button)
        {
            File.WriteAllText(SelectedMenuPath, "");
            SceneManager.LoadScene("Menu");
        }
    }
}
