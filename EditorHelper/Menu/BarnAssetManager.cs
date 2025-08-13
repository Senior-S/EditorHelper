using Newtonsoft.Json;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using EditorHelper.CustomAssets;
using UnityEngine;

namespace EditorHelper.Menu
{
    public class BarnAssetManager
    {
        private static readonly string SelectedMenuPath = Path.Combine(UnturnedPaths.RootDirectory.FullName, "SelectedMenu.json");

        // Track instantiated scene objects for cleanup
        private static List<GameObject>? _instantiatedObjects;
        private static AudioClip? _originalMusic;

        public BarnAssetManager()
        {
            UnturnedLog.info("Barn Asset Manager initialized.");
            _instantiatedObjects = new List<GameObject>();
            _originalMusic = GameObject.Find("Menu").GetComponent<AudioSource>().clip;
            Setup();
        }
        public static void Setup()
        {
            LoadSceneFromSavedData();
        }

        public static Guid ReadSelectedMenu()
        {
            if (File.Exists(SelectedMenuPath))
            {
                try
                {
                    string dataText = File.ReadAllText(SelectedMenuPath);
                    var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(dataText);

                    if (data != null && data.ContainsKey("Menu"))
                    {
                        if (Guid.TryParse(data["Menu"], out Guid menuGuid))
                            return menuGuid;
                        else
                            UnturnedLog.info("Invalid GUID format in SelectedMenu.json.");
                    }
                    else
                        UnturnedLog.info("No 'Menu' key found in SelectedMenu.json.");
                }
                catch (Exception ex)
                {
                    UnturnedLog.info($"Error reading SelectedMenu.json: {ex.Message}");
                }
            }
            else
                UnturnedLog.info("SelectedMenu.json not found.");

            return Guid.Empty;
        }

        public static void SetupCustomScene(SceneAsset barnAsset)
        {
            UnturnedLog.info($"Setting up scene: {barnAsset.BarnName}");

            // Clear the previous scene
            CleanupPreviousScene(barnAsset);

            // Instantiate the scene GameObject if available and track it
            if (barnAsset.BarnPropsGameObject != null)
            {
                GameObject instantiatedObject = GameObject.Instantiate(barnAsset.BarnPropsGameObject);
                _instantiatedObjects?.Add(instantiatedObject);
                UnturnedLog.info($"Instantiated scene object: {barnAsset.BarnName}");
            }
            else UnturnedLog.info($"Scene prefab is null for: {barnAsset.name}");

            // Set up the skybox if provided
            if (barnAsset.BarnSkyboxMaterial != null) RenderSettings.skybox = barnAsset.BarnSkyboxMaterial;
            else UnturnedLog.info($"Scene skybox is null for: {barnAsset.name}");
        }

        public static void LoadSceneFromSavedData()
        {
            Guid guid = ReadSelectedMenu();

            if (guid == Guid.Empty)
            {
                UnturnedLog.info("No valid menu GUID found to load.");
                return;
            }

            SceneAsset sceneAsset = SDG.Unturned.Assets.find<SceneAsset>(guid);

            if (sceneAsset != null) SetupCustomScene(sceneAsset);
            else UnturnedLog.info($"SceneAsset with GUID {guid} not found.");
        }

        private static void CleanupPreviousScene(SceneAsset sceneAsset)
        {
            UnturnedLog.info("Cleaning up previous scene...");

            // Destroy all tracked instantiated objects
            if (_instantiatedObjects != null)
            {
                foreach (var obj in _instantiatedObjects)
                {
                    if (obj != null)
                    {
                        GameObject.Destroy(obj);
                        UnturnedLog.info($"Destroyed object: {obj.name}");
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
                    GameObject.Destroy(resource);
                    UnturnedLog.info($"Removed resource: {resource.name}");
                }
            }

            GameObject flag = GameObject.Find("Ukranian Flag (solo)");
            if (flag != null)
            {
                GameObject.Destroy(flag);
                UnturnedLog.info("Removed Ukrainian flag.");
            }

            GameObject props = GameObject.Find("Props");
            if (props != null)
            {
                GameObject.Destroy(props);
                UnturnedLog.info("Removed Props GameObject.");
            }
            
            GameObject menu = GameObject.Find("Menu");
            AudioSource menuMusic = menu.GetComponent<AudioSource>();

            if (sceneAsset.BarnAudioClip != null)
            {
                UnturnedLog.info($"Menu Music replaced");
                menuMusic.clip = sceneAsset.BarnAudioClip;
                menuMusic.Play();
            }
            else {
                UnturnedLog.info($"Menu Music not replaced");
                menuMusic.clip = _originalMusic; 
                menuMusic.Play();
            }

            UnturnedLog.info("Scene cleanup complete.");
        }
    }
}
