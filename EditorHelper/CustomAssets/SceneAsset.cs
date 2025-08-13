using System;
using SDG.Unturned;
using System.IO;
using UnityEngine;

namespace EditorHelper.CustomAssets
{
    public class SceneAsset : Asset
    {
        public GameObject? BarnPropsGameObject { get; private set; }
        public Material? BarnSkyboxMaterial { get; private set; }
        public AudioClip? BarnAudioClip { get; private set; }
        public Texture2D? BarnIcon { get; set; }
        public string BarnName { get; private set; } = "";

        public override void PopulateAsset(in PopulateAssetParameters p)
        {
            MasterBundleReference<GameObject> prefabRef = p.data.readMasterBundleReference<GameObject>("Prefab", p.bundle);
            MasterBundleReference<Material> skyboxRef = p.data.readMasterBundleReference<Material>("Skybox_Material", p.bundle);
            MasterBundleReference<AudioClip> audioClipRef = p.data.readMasterBundleReference<AudioClip>("Audio", p.bundle);
            BarnName = p.data.GetString("Name");

            string filePath = this.getFilePath();
            string directoryPath = Path.GetDirectoryName(filePath) ?? string.Empty;
            string iconPath = Path.Combine(directoryPath, "Icon.png");

            BarnIcon = new Texture2D(2, 2);
            try 
            {
                byte[] fileData = File.ReadAllBytes(iconPath);
                BarnIcon.LoadImage(fileData);
            }
            catch (Exception e) { UnturnedLog.info("Error loading image: " + e.Message); }

            if (prefabRef.isValid) BarnPropsGameObject = prefabRef.loadAsset();
            if (skyboxRef.isValid) BarnSkyboxMaterial = skyboxRef.loadAsset();
            if (audioClipRef.isValid) BarnAudioClip = audioClipRef.loadAsset();
            
            UnturnedLog.info("Populated " + BarnName + " Barn Asset.");
        }
    }
}