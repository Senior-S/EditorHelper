using EditorHelper2.Assets;
using EditorHelper2.Extensions.Menu;
using EditorHelper2.Loader;
using HarmonyLib;
using SDG.Framework.Modules;
using SDG.Unturned;

namespace EditorHelper2;

public class EditorHelper : IModuleNexus
{
    public static Harmony Harmony { get; private set; }
    public static BarnAssetManager BarnAssetManager;
    
    public EditorHelper()
    {
        Harmony = new Harmony("com.seniors.editorhelper2");
    }
    
    public void initialize()
    {
        Harmony.PatchAll(this.GetType().Assembly);
        
        CommandWindow.LogFormat("Editor Helper 2 v{0}", this.GetType().Assembly.GetName().Version);
        
        RegisterCustomAssets();

        int loadedExtensions = ExtensionManager.LoadAllExtensions();
        CommandWindow.LogFormat("[EditorHelper2] Loaded {0} extensions.", loadedExtensions);
    }
    
    public static void RegisterCustomAssets()
    {
        SDG.Unturned.Assets.assetTypes.addType("BarnAsset",typeof(BarnAsset));
    }

    public void shutdown()
    {
        Harmony.UnpatchAll(Harmony.Id);
    }
}
