using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.LSystem;

public class Visualizer
{
    private readonly LSystemGenerator _lsystem;
    private readonly List<Vector3> _positions = [];
    
    private readonly RoadHelper _roadHelper;
    
    private int _length = 8;

    private readonly LevelObject _startObject;

    public Visualizer(LSystemGenerator lsystem, RoadHelper roadHelper, LevelObject startObject)
    {
        _lsystem = lsystem;
        _roadHelper = roadHelper;
        _startObject = startObject;
    }

    /*private float angle
    {
        get => Random.Range(30f, 75f);
    }*/

    private const float Angle = 90f;

    public int Length
    {
        get => _length > 0 ? _length : 1;
        set => _length = value;
    }

    public void Start()
    {
        string sequence = _lsystem.GenerateSentence();
        
        VisualizeSequence(sequence);
    }

    private void VisualizeSequence(string sequence)
    {
        Stack<AgentParameters> savePoints = new();
        Vector3 currentPosition = _startObject.transform.position;
        Vector3 direction = _startObject.transform.up;
        
        _positions.Add(currentPosition);
        
        foreach (char c in sequence)
        {
            EncodingLetters encoding = (EncodingLetters)c;

            switch (encoding)
            {
                case EncodingLetters.Save:
                    savePoints.Push(new AgentParameters(currentPosition, direction, Length));
                    break;
                case EncodingLetters.Load:
                    if (savePoints.Count > 0)
                    {
                        AgentParameters agentParameters = savePoints.Pop();
                        currentPosition = agentParameters.position;
                        direction = agentParameters.direction;
                        Length = agentParameters.length;
                    }
                    break;
                case EncodingLetters.Draw:
                    _roadHelper.PlaceStreetPosition(ref currentPosition, direction, Length);
                    Length -= 2;
                    _positions.Add(currentPosition);
                    break;
                case EncodingLetters.TurnRight:
                    direction = Quaternion.AngleAxis(Angle, Vector3.up) * direction;
                    break;
                case EncodingLetters.TurnLeft:
                    direction = Quaternion.AngleAxis(-Angle, Vector3.up) * direction;
                    break;
                default:
                    break;
            }
        }
        
        _roadHelper.FixRoad();
    }

    private enum EncodingLetters
    {
        Unknown = '1',
        Save = '[',
        Load = ']',
        Draw = 'F',
        TurnRight = '+',
        TurnLeft = '-'
    }
}