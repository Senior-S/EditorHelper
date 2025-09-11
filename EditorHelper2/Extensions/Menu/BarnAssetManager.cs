using Newtonsoft.Json;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using EditorHelper2.Assets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorHelper2.Extensions.Menu
{
    public class BarnAssetManager
    {
        private static readonly string SelectedMenuPath = Path.Combine(UnturnedPaths.RootDirectory.FullName, "SelectedMenu.json");

        // Track instantiated scene objects for cleanup
        private static List<GameObject>? _instantiatedObjects;
        private static AudioClip? _originalMusic;

        public BarnAssetManager()
        {
            UnturnedLog.info("[BarnAssetManager] Constructor called.");
            _instantiatedObjects = new List<GameObject>();

            var menu = GameObject.Find("Menu");
            if (menu != null && menu.TryGetComponent<AudioSource>(out var audioSource))
            {
                _originalMusic = audioSource.clip;
                UnturnedLog.info("[BarnAssetManager] Original menu music cached.");
            }
            else
            {
                _originalMusic = null;
                UnturnedLog.info("[BarnAssetManager] Menu GameObject or AudioSource not found during initialization.");
            }

            Setup();
        }

        public static void Setup()
        {
            UnturnedLog.info("[BarnAssetManager] Setup() called.");
            LoadSceneFromSavedData();
        }

        public static Guid ReadSelectedMenu()
        {
            UnturnedLog.info("[BarnAssetManager] ReadSelectedMenu() called.");

            if (!File.Exists(SelectedMenuPath))
            {
                UnturnedLog.info("[BarnAssetManager] SelectedMenu.json not found.");
                return Guid.Empty;
            }

            try
            {
                string dataText = File.ReadAllText(SelectedMenuPath);
                UnturnedLog.info($"[BarnAssetManager] SelectedMenu.json content: {dataText}");

                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(dataText);

                if (data != null && data.ContainsKey("Menu"))
                {
                    if (Guid.TryParse(data["Menu"], out Guid menuGuid))
                    {
                        UnturnedLog.info($"[BarnAssetManager] Parsed GUID: {menuGuid}");
                        return menuGuid;
                    }
                    else
                    {
                        UnturnedLog.info("[BarnAssetManager] Invalid GUID format in SelectedMenu.json.");
                        return Guid.Empty;
                    }
                }
                else
                {
                    UnturnedLog.info("[BarnAssetManager] No 'Menu' key found in SelectedMenu.json.");
                    return Guid.Empty;
                }
            }
            catch (Exception ex)
            {
                UnturnedLog.info($"[BarnAssetManager] Error reading SelectedMenu.json: {ex.Message}");
                return Guid.Empty;
            }
        }

        public static void SetupCustomScene(BarnAsset barnAsset)
        {
            if (barnAsset == null)
            {
                UnturnedLog.info("[BarnAssetManager] SetupCustomScene called with NULL barnAsset!");
                return;
            }

            UnturnedLog.info($"[BarnAssetManager] Setting up scene: {barnAsset.BarnName}");

            // Clear the previous scene
            CleanupPreviousScene(barnAsset);

            // Instantiate the scene GameObject if available and track it
            if (barnAsset.BarnPropsGameObject != null)
            {
                GameObject instantiatedObject = Object.Instantiate(barnAsset.BarnPropsGameObject);
                _instantiatedObjects?.Add(instantiatedObject);
                UnturnedLog.info($"[BarnAssetManager] Instantiated scene object: {instantiatedObject.name}");
            }
            else
            {
                UnturnedLog.info($"[BarnAssetManager] Scene prefab is null for: {barnAsset.BarnName}");
            }

            // Set up the skybox if provided
            if (barnAsset.BarnSkyboxMaterial != null)
            {
                RenderSettings.skybox = barnAsset.BarnSkyboxMaterial;
                UnturnedLog.info("[BarnAssetManager] Applied custom skybox.");
            }
            else
            {
                UnturnedLog.info($"[BarnAssetManager] Scene skybox is null for: {barnAsset.BarnName}");
            }
        }

        public static void LoadSceneFromSavedData()
        {
            UnturnedLog.info("[BarnAssetManager] LoadSceneFromSavedData() called.");

            Guid guid = ReadSelectedMenu();
            UnturnedLog.info($"[BarnAssetManager] GUID read: {guid}");

            if (guid == Guid.Empty)
            {
                UnturnedLog.info("[BarnAssetManager] No valid menu GUID found to load.");
                return;
            }

            BarnAsset sceneAsset = SDG.Unturned.Assets.find<BarnAsset>(guid);

            if (sceneAsset != null)
            {
                UnturnedLog.info($"[BarnAssetManager] Found BarnAsset: {sceneAsset.BarnName}");
                SetupCustomScene(sceneAsset);
            }
            else
            {
                UnturnedLog.info($"[BarnAssetManager] SceneAsset with GUID {guid} not found.");
            }
        }

        private static void CleanupPreviousScene(BarnAsset sceneAsset)
        {
            UnturnedLog.info("[BarnAssetManager] Cleaning up previous scene...");

            // Destroy all tracked instantiated objects
            if (_instantiatedObjects != null)
            {
                foreach (var obj in _instantiatedObjects)
                {
                    if (obj != null)
                    {
                        UnturnedLog.info($"[BarnAssetManager] Destroying object: {obj.name}");
                        Object.Destroy(obj);
                    }
                }
                _instantiatedObjects.Clear();
            }

            // Remove additional objects that may not be tracked
            for (int i = 0; i < 20; i++)
            {
                GameObject resource = GameObject.Find(i == 0 ? "Resource" : $"Resource ({i})");
                if (resource != null)
                {
                    UnturnedLog.info($"[BarnAssetManager] Removing resource: {resource.name}");
                    Object.Destroy(resource);
                }
            }

            GameObject flag = GameObject.Find("Ukranian Flag (solo)");
            if (flag != null)
            {
                UnturnedLog.info("[BarnAssetManager] Removing Ukrainian flag.");
                Object.Destroy(flag);
            }

            GameObject props = GameObject.Find("Props");
            if (props != null)
            {
                UnturnedLog.info("[BarnAssetManager] Removing Props GameObject.");
                Object.Destroy(props);
            }

            GameObject menu = GameObject.Find("Menu");
            if (menu == null)
            {
                UnturnedLog.info("[BarnAssetManager] Menu GameObject not found during cleanup.");
                return;
            }

            if (!menu.TryGetComponent<AudioSource>(out var menuMusic))
            {
                UnturnedLog.info("[BarnAssetManager] Menu AudioSource not found.");
                return;
            }

            if (sceneAsset.BarnAudioClip != null)
            {
                UnturnedLog.info("[BarnAssetManager] Replacing menu music with BarnAudioClip.");
                menuMusic.clip = sceneAsset.BarnAudioClip;
                menuMusic.Play();
            }
            else if (_originalMusic != null)
            {
                UnturnedLog.info("[BarnAssetManager] Restoring original menu music.");
                menuMusic.clip = _originalMusic;
                menuMusic.Play();
            }
            else
            {
                UnturnedLog.info("[BarnAssetManager] No audio clip available to play.");
            }

            UnturnedLog.info("[BarnAssetManager] Scene cleanup complete.");
        }
    }
}
