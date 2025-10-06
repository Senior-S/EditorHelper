using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using EditorHelper2.Assets;
using EditorHelper2.Commands;
using EditorHelper2.common.Helpers;
using EditorHelper2.Loader;
using HarmonyLib;
using SDG.Framework.Modules;
using SDG.Unturned;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorHelper2;

public class EditorHelper : IModuleNexus
{
    private static Harmony Harmony { get; set; }
    private static DiscordRichPresence _richPresence;

    public static DiscordRichPresence GetRichPresence()
    {
        return _richPresence;
    }
    
    public EditorHelper()
    {
        Harmony = new Harmony("com.seniors.editorhelper2");
    }
    
    public void initialize()
    {
        Harmony.PatchAll(this.GetType().Assembly);
        
        Task.Run(UpdaterCore.Init);
        Level.onLevelExited += () => Task.Run(UpdaterCore.Init);
        CommandWindow.LogFormat("Editor Helper 2 v{0}", GetType().Assembly.GetName().Version);
        
        RegisterCustomAssets();
        
        int loadedExtensions = ExtensionManager.LoadAllExtensions();
        CommandWindow.LogFormat("[EditorHelper2] Loaded {0} extensions.", loadedExtensions);
        
        InitDiscordRichPresence();
    }
    
    private void InitDiscordRichPresence()
    {
        GameObject gameObject = new("Discord");
        Object.DontDestroyOnLoad(gameObject);
        _richPresence = gameObject.AddComponent<DiscordRichPresence>();
    }

    private void RegisterCustomAssets()
    {
        SDG.Unturned.Assets.assetTypes.addType("BarnAsset",typeof(BarnAsset));
    }

    public static void RegisterCustomCommands()
    {
        Commander.register(new CommandHeal());
        Commander.register(new CommandMaxSkills());
        Commander.register(new CommandResetSkills());
        Commander.register(new CommandI());
        Commander.register(new CommandV());
        Commander.register(new CommandExp());
        Commander.register(new CommandTp());
        Commander.register(new CommandJump());
        Commander.register(new CommandFly());
        Commander.register(new CommandAmmo());
    } 

    public void shutdown()
    {
        Harmony.UnpatchAll(Harmony.Id);
        Object.Destroy(_richPresence);
    }
}
