using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.LSystem;

public class RoadHelper : MonoBehaviour
{
    //public GameObject roadStraight, roadTee, roadQuad, roadCorner, roadEnd;
    private readonly Dictionary<Vector3Int, Transform> _roadDictionary = new();
    private readonly HashSet<Vector3Int> _fixRoadCandidates = new();
    
    public ObjectAsset roadStraight, roadTee, roadQuad, roadCorner, roadEnd;

    private GameObject _roadStraightGameObject = null;
    
    private int offset = 24;
    
    public void PlaceStreetPosition(ref Vector3 startPosition, Vector3 direction, int roadLength)
    {
        if (roadLength % 2 == 0)
        {
            roadLength /= 2;
        }

        for (int i = 0; i < roadLength; i++)
        {
            if (_roadStraightGameObject == null)
            {
                _roadStraightGameObject = roadStraight.GetOrLoadModel();
            }
            Quaternion rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(-90f, 0f, 0f);
            GameObject roadGameObject = Instantiate(_roadStraightGameObject, Vector3.zero, rotation);
            Mesh mesh = roadGameObject.GetComponent<MeshFilter>().mesh;
            startPosition += roadGameObject.transform.up * mesh.bounds.size.x;
            /*if (offset < 1)
            {
                offset = (int)mesh.bounds.size.x;
            }*/
            Vector3Int position = Vector3Int.RoundToInt(startPosition);
            Object.Destroy(roadGameObject);
            if (_roadDictionary.ContainsKey(position))
            {
                continue;
            }
            Transform objectTransform = PlaceObject(roadStraight, position, rotation);
            
            _roadDictionary.Add(position, objectTransform);
            if (i == 0 || i == roadLength - 1)
            {
                _fixRoadCandidates.Add(position);
            }
        }
    }

    private Transform PlaceObject(ObjectAsset objectAsset, Vector3Int position, Quaternion rotation)
    {
        EditorObjects.selectedObjectAsset = objectAsset;
        LevelObjects.step++;
        return LevelObjects.registerAddObject(position, rotation, Vector3.one, EditorObjects.selectedObjectAsset, EditorObjects.selectedItemAsset);
    }

    private void RemoveObject(Transform objectTransform)
    {
        LevelObjects.registerRemoveObject(objectTransform);
    }

    public void FixRoad()
    {
        foreach (Vector3Int position in _fixRoadCandidates)
        {
            List<Direction> neighborDirections = PlacementHelper.FindNeighbor(position, _roadDictionary.Keys, offset);
            
            Debug.Log("Neighbors: " + neighborDirections.Count);
            
            if (neighborDirections.Count == 1)
            {
                Quaternion endRotation = _roadDictionary[position].rotation;
                //Destroy(_roadDictionary[position]);
                RemoveObject(_roadDictionary[position]);
                _roadDictionary[position] = PlaceObject(roadEnd, position, endRotation) /*Instantiate(roadEnd, position, endRotation)*/;
            }
            else if (neighborDirections.Count == 2)
            {
                if (neighborDirections.Contains(Direction.Up) && neighborDirections.Contains(Direction.Down)
                    || neighborDirections.Contains(Direction.Right) && neighborDirections.Contains(Direction.Left)) continue;
                
                Vector3 rotationEuler = new(-90f, 0, 0);
                rotationEuler.z = neighborDirections.Contains(Direction.Right) ? 90 : 0;

                //Destroy(_roadDictionary[position]);
                RemoveObject(_roadDictionary[position]);
                _roadDictionary[position] = PlaceObject(roadCorner, position, Quaternion.Euler(rotationEuler)) /*Instantiate(roadCorner, position, Quaternion.Euler(rotationEuler))*/;
            }
            else if (neighborDirections.Count == 3)
            {
                Quaternion rotation = Quaternion.Euler(-90f, 0, -90);
                if (neighborDirections.Contains(Direction.Right) 
                    && neighborDirections.Contains(Direction.Down) 
                    && neighborDirections.Contains(Direction.Left)
                   )
                {
                    rotation = Quaternion.Euler(-90f, 0, 0);
                }
                else if (neighborDirections.Contains(Direction.Down) 
                         && neighborDirections.Contains(Direction.Left)
                         && neighborDirections.Contains(Direction.Up))
                {
                    rotation = Quaternion.Euler(-90f, 0, -270);
                }
                else if (neighborDirections.Contains(Direction.Left) 
                         && neighborDirections.Contains(Direction.Up)
                         && neighborDirections.Contains(Direction.Right))
                {
                    rotation = Quaternion.Euler(-90f, 0, -180);
                }

                //Destroy(_roadDictionary[position]);
                RemoveObject(_roadDictionary[position]);
                _roadDictionary[position] = PlaceObject(roadTee, position, rotation) /*Instantiate(roadTee, position, rotation)*/;
            }
            else if(neighborDirections.Count == 4)
            {
                //Destroy(_roadDictionary[position]);
                RemoveObject(_roadDictionary[position]);
                _roadDictionary[position] = PlaceObject(roadQuad, position, Quaternion.Euler(-90f, 0f, 0f)) /*Instantiate(roadQuad, position, Quaternion.Euler(-90f, 0f, 0f))*/;
            }
        }
    }
}