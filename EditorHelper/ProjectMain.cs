using System.Reflection;
using EditorHelper.Editor;
using HarmonyLib;
using SDG.Framework.Modules;
using SDG.Unturned;

namespace EditorHelper;

public class EditorHelper : IModuleNexus
{
    public static EditorHelper Instance;

    public ObjectsManager ObjectsManager;
    
    private Harmony _harmony;

    public void initialize()
    {
        Instance = this;
        
        _harmony = new Harmony("com.seniors.editorhelper");
        _harmony.PatchAll(this.GetType().Assembly);

        CommandWindow.LogWarning($"Editor helper v{this.GetType().Assembly.GetName().Version}");
        CommandWindow.Log("<<SSPlugins>>");
    }

    public void shutdown()
    {
        _harmony.UnpatchAll(_harmony.Id);

        CommandWindow.Log("<<SSPlugins>>");
    }
}