using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.common.Types;

public class ReunRoadDelete : IReun
{
    private readonly byte _material;
    private readonly CachingAssetRef _roadAssetRef;
    private readonly bool _isLoop;
    private readonly List<RoadJointSnapshot> _jointSnapshots;

    private Road? _road;

    public int step { get; private set; }

    private sealed class RoadJointSnapshot
    {
        public Vector3 Vertex { get; set; }
        public Vector3 Tangent0 { get; set; }
        public Vector3 Tangent1 { get; set; }
        public ERoadMode Mode { get; set; }
        public float Offset { get; set; }
        public bool IgnoreTerrain { get; set; }
    }

    public ReunRoadDelete(int newStep, Road road)
    {
        step = newStep;
        _road = road;
        _material = road.material;
        _roadAssetRef = road.RoadAssetRef;
        _isLoop = road.isLoop;
        _jointSnapshots = new List<RoadJointSnapshot>(road.joints.Count);

        foreach (RoadJoint joint in road.joints)
        {
            _jointSnapshots.Add(new RoadJointSnapshot
            {
                Vertex = joint.vertex,
                Tangent0 = joint.getTangent(0),
                Tangent1 = joint.getTangent(1),
                Mode = joint.mode,
                Offset = joint.offset,
                IgnoreTerrain = joint.ignoreTerrain
            });
        }
    }

    public Transform redo()
    {
        if (_road == null) return null;

        LevelRoads.removeRoad(_road);
        _road = null;
        return null;
    }

    public void undo()
    {
        Transform restoredVertex = RestoreRoad();
        if (restoredVertex == null) return;

        _road = LevelRoads.getRoad(restoredVertex, out _, out _);
    }

    private Transform RestoreRoad()
    {
        if (_jointSnapshots.Count == 0) return null;

        Transform firstVertex = LevelRoads.addRoad(_jointSnapshots[0].Vertex);
        Road? restoredRoad = LevelRoads.getRoad(firstVertex, out _, out _);
        if (restoredRoad == null) return null;

        restoredRoad.material = _material;
        restoredRoad.RoadAssetRef = _roadAssetRef;

        for (int i = 1; i < _jointSnapshots.Count; i++)
        {
            restoredRoad.addVertex(i, _jointSnapshots[i].Vertex);
        }

        for (int i = 0; i < _jointSnapshots.Count; i++)
        {
            RoadJointSnapshot snapshot = _jointSnapshots[i];
            RoadJoint restoredJoint = restoredRoad.joints[i];
            restoredJoint.ignoreTerrain = snapshot.IgnoreTerrain;
            restoredJoint.offset = snapshot.Offset;

            // Preserve exact tangent vectors first, then restore tangent mode.
            restoredJoint.mode = ERoadMode.FREE;
            restoredJoint.setTangent(0, snapshot.Tangent0);
            restoredJoint.setTangent(1, snapshot.Tangent1);
            restoredJoint.mode = snapshot.Mode;

            restoredRoad.moveVertex(i, snapshot.Vertex);
        }

        restoredRoad.isLoop = _isLoop;
        restoredRoad.updatePoints();
        return firstVertex;
    }
}
