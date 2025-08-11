using System;
using System.Collections.Generic;
using System.Reflection;
using DanielWillett.UITools;
using DanielWillett.UITools.Core.Extensions;
using EditorHelper2.API.Abstract;
using EditorHelper2.Loader;
using HarmonyLib;
using SDG.Framework.Modules;
using SDG.Unturned;

namespace EditorHelper2;

public class EditorHelper : IModuleNexus
{
    public static Harmony Harmony { get; private set; }
    private bool _extensionsLoaded = false;
    
    public EditorHelper()
    {
        Harmony = new Harmony("com.seniors.editorhelper2");
        
        //UnturnedUIToolsNexus.UIExtensionManager.
    }
    
    public void initialize()
    {
        Harmony.PatchAll(this.GetType().Assembly);
        
        CommandWindow.LogFormat("Editor Helper 2 v{0}", this.GetType().Assembly.GetName().Version);

        Level.onLevelLoaded += OnLevelLoaded;
    }

    private void OnLevelLoaded(int level)
    {
        if (_extensionsLoaded || level != Level.BUILD_INDEX_MENU) return;
        
        int loadedExtensions = ExtensionManager.LoadAllExtensions();
        _extensionsLoaded = true;
        CommandWindow.LogFormat("[EditorHelper2] Loaded {0} extensions.", loadedExtensions);
    }

    public void shutdown()
    {
        Level.onLevelLoaded -= OnLevelLoaded;
        
        Harmony.UnpatchAll(Harmony.Id);
    }
    
    public static void InvokeStaticEvent(Type classType, string eventName, params object[] args)
    {
        try
        {
            if (classType == null) return;
            FieldInfo? eventField = classType.GetField(eventName, BindingFlags.Static | BindingFlags.NonPublic);
            if (eventField == null) return;
            Delegate eventDelegate = (Delegate)eventField.GetValue(null);

            eventDelegate?.DynamicInvoke(args);
        }
        catch (Exception ex)
        {
            CommandWindow.LogErrorFormat("Error invoking event {0}: {1}", eventName, ex.Message);
        }
    }
}
