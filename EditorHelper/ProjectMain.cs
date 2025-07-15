using System;
using System.IO;
using System.Threading.Tasks;
using EditorHelper.Commands;
using EditorHelper.Editor;
using EditorHelper.Extras;
using EditorHelper.Schematics;
using HarmonyLib;
using SDG.Framework.Modules;
using SDG.Unturned;

namespace EditorHelper;

public class EditorHelper : IModuleNexus
{
    public static EditorHelper Instance;

    public ObjectsManager ObjectsManager;
    public RoadsManager RoadsManager;
    public NodesManager NodesManager;
    public EditorManager EditorManager;
    public FoliageManager FoliageManager;
    public VisibilityManager VisibilityManager;
    
    public SchematicsManager SchematicsManager;
    
    private Harmony _harmony;

    public void initialize()
    {
        Instance = this;
        
        _harmony = new Harmony("com.seniors.editorhelper");
        _harmony.PatchAll(this.GetType().Assembly);

        Task.Run(UpdaterCore.Init);
        SchematicsManager = new SchematicsManager();
        
        CommandWindow.LogWarning($"Editor helper v{this.GetType().Assembly.GetName().Version}");
        CommandWindow.Log("<<SSPlugins>>");
    }

    public static void RegisterCommands()
    {
        Commander.register(new HealCommand());
        Commander.register(new MaxSkillsCommand());
        Commander.register(new ICommand());
        Commander.register(new VCommand());
        Commander.register(new ExpCommand());
        Commander.register(new TpCommand());
    }

    public void shutdown()
    {
        _harmony.UnpatchAll(_harmony.Id);

        CommandWindow.Log("<<SSPlugins>>");
    }
}