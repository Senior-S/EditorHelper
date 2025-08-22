using System;
using System.Collections.Generic;
using System.Linq;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.common.Helpers.Level.Objects;

public static class ObjectsHelper
{
    private static bool HaveNegativeScale(Vector3 scale)
    {
        for (int i = 0; i < 3; i++)
        {
            if (scale[i] < 0f)
            {
                return true;
            }
        }

        return false;
    }
    
    public static List<LevelObject> GetWrongScaledObjects()
    {
        IEnumerable<LevelObject> levelObjects = LevelObjects.objects.Cast<List<LevelObject>>().SelectMany(list => list);

        return levelObjects.Where(c => c != null && c.transform != null && ObjectsHelper.HaveNegativeScale(c.transform.localScale)).ToList();
    }
    
    public static List<LevelObject> GetObjectsByMod(string filter)
    {
        IEnumerable<LevelObject> levelObjects = LevelObjects.objects.Cast<List<LevelObject>>().SelectMany(list => list);
        
        return levelObjects.Where(c => c != null && c.asset != null && c.asset.GetOriginName().Contains(filter)).ToList();
    }
    
    public static List<LevelObject> GetObjectsByGuid(Guid guid)
    {
        IEnumerable<LevelObject> levelObjects = LevelObjects.objects.Cast<List<LevelObject>>().SelectMany(list => list);

        return levelObjects.Where(c => c != null && c.asset != null && c.asset.GUID == guid).ToList();
    }
    
    public static Vector3 GetObjectFace(Transform objTransform)
    {
        Vector3 cameraDirection = MainCamera.instance.transform.forward;
        
        Vector3[] faces = {
            objTransform.up, // Up? Yeah, using forward makes the object spawn up or down due the object by default is rotated 90 degrees
            -objTransform.up,
            objTransform.right,
            -objTransform.right
        };

        return faces.OrderBy(c => Vector3.Distance(c, cameraDirection)).First();
    }
    
    public static Bounds GetObjectBounds(Transform objectTransform)
    {
        if (objectTransform.TryGetComponent(out MeshFilter meshFilter))
        {
            return meshFilter.mesh.bounds;
        }
        List<MeshFilter> filters = objectTransform.GetComponentsInChildren<MeshFilter>(true)
            .Where(c => !c.transform.name.Equals("nav", StringComparison.OrdinalIgnoreCase)).ToList();
        
        return filters.Count > 0 ? filters[0].mesh.bounds : new Bounds(objectTransform.position, Vector3.one);
    }
}