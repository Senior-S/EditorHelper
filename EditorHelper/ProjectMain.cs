using System.Threading.Tasks;
using EditorHelper.Commands;
using EditorHelper.CustomAssets;
using EditorHelper.Editor.Managers;
using EditorHelper.Extras;
using EditorHelper.Menu;
using EditorHelper.Patches.Menu.UI;
using EditorHelper.Schematics;
using HarmonyLib;
using SDG.Framework.Modules;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper;

public class EditorHelper : IModuleNexus
{
    public static EditorHelper Instance;
    
    public ObjectsManager ObjectsManager;
    public RoadsManager RoadsManager;
    public NodesManager NodesManager;
    public EditorManager EditorManager;
    public FoliageManager FoliageManager;
    public FoliageCollectionManager FoliageCollectionManager;
    public FoliageAssetManager FoliageAssetManager;
    public MaterialAssetManager MaterialAssetManager;
    public AnimalSpawnsManager AnimalSpawnsManager;
    public VehicleSpawnsManager VehicleSpawnsManager;
    public VisibilityManager VisibilityManager;
    public BarnAssetManager BarnAssetManager;
    
    public SchematicsManager SchematicsManager;

    private Harmony _harmony;

    public void initialize()
    {
        Instance = this;
        
        _harmony = new Harmony("com.seniors.editorhelper");
        _harmony.PatchAll(this.GetType().Assembly);

        Task.Run(UpdaterCore.Init);
        SchematicsManager = new SchematicsManager();
        
        Level.onLevelExited += () => Task.Run(UpdaterCore.Init);

        // Registering assets on Module Nexus level, vanilla does it similarly
        RegisterCustomAssets();
        
        CommandWindow.LogWarning($"Editor helper v{this.GetType().Assembly.GetName().Version}");
        CommandWindow.Log("<<SPlugins>>");
    }

    public static void RegisterCommands()
    {
        Commander.register(new CommandHeal());
        Commander.register(new CommandMaxSkills());
        Commander.register(new CommandResetSkills());
        Commander.register(new ICommand());
        Commander.register(new CommandV());
        Commander.register(new CommandExp());
        Commander.register(new CommandTp());
        Commander.register(new CommandJump());
        Commander.register(new CommandFly());
        Commander.register(new CommandAmmo());
    }

    public static void RegisterCustomAssets()
    {
        Assets.assetTypes.addType("SceneAsset",typeof(SceneAsset));
    }

    public void shutdown()
    {
        _harmony.UnpatchAll(_harmony.Id);

        CommandWindow.Log("<<SPlugins>>");
    }
}