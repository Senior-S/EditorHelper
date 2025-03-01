using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using HighlightingSystem;
using SDG.Framework.Modules;
using SDG.Unturned;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorHelper;

public class ProjectMain : IModuleNexus
{
    private Harmony _harmony;
    private static List<Highlighter> _highlightedObjects = [];
    public static SleekButtonIcon HighlightButton;
    
    public void initialize()
    {
        _harmony = new Harmony("com.seniors.editorhelper");
        _harmony.PatchAll(this.GetType().Assembly);

        CommandWindow.LogWarning($"Editor helper v{this.GetType().Assembly.GetName().Version}");
        CommandWindow.Log("<<SSPlugins>>");
    }

    internal static void HighlightObjects(ISleekElement button)
    {
        GameObject mostRecentSelectedGameObject = EditorObjects.GetMostRecentSelectedGameObject();

        if (mostRecentSelectedGameObject == null) return;
        
        LevelObject levelObject = FindLevelObject(mostRecentSelectedGameObject);
        if (levelObject?.asset == null) return;

        List<LevelObject> levelObjects = GetObjectsByGuid(levelObject.asset.GUID);
        foreach (LevelObject lObj in levelObjects)
        {
            if (lObj == null || lObj.transform == null || lObj.transform.gameObject == null || lObj == levelObject) continue;
            Highlighter highlighter = lObj.transform.GetComponent<Highlighter>();
            if (!highlighter)
            {
                highlighter = lObj.transform.gameObject.AddComponent<Highlighter>();
            }
            highlighter.overlay = true;
            highlighter.ConstantOn(Color.yellow);
            _highlightedObjects.Add(highlighter);
        }

        HighlightButton.text = $"Highlight objects ({levelObjects.Count})";
    }
    
    internal static void UnhighlightObjects(ISleekElement button)
    {
        UnhighlightAll();
    }

    public static void UnhighlightAll(Transform ignore = null)
    {
        if (_highlightedObjects.Count < 1) return;

        foreach (Highlighter highlightedObject in _highlightedObjects)
        {
            if (highlightedObject.transform == ignore) continue;
            
            Object.DestroyImmediate(highlightedObject);
        }
        _highlightedObjects.Clear();
        HighlightButton.text = "Highlight objects";
    }

    private static LevelObject FindLevelObject(GameObject rootGameObject)
    {
        if (rootGameObject == null)
        {
            return null;
        }
        Transform transform = rootGameObject.transform;
        if (Regions.tryGetCoordinate(transform.position, out byte x, out byte y))
        {
            for (int i = 0; i < LevelObjects.objects[x, y].Count; i++)
            {
                if (LevelObjects.objects[x, y][i].transform == transform)
                {
                    return LevelObjects.objects[x, y][i];
                }
            }
        }
        return null;
    }

    private static List<LevelObject> GetObjectsByGuid(Guid guid)
    {
        IEnumerable<LevelObject> levelObjects = LevelObjects.objects.Cast<List<LevelObject>>().SelectMany(list => list);

        return levelObjects.Where(c => c != null && c.asset != null && c.asset.GUID == guid).ToList();
    }


    public void shutdown()
    {
        _harmony.UnpatchAll(_harmony.Id);

        CommandWindow.Log("<<SSPlugins>>");
    }
}