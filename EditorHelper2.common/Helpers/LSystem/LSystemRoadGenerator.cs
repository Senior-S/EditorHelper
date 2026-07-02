using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EditorHelper2.common.Helpers.LSystem;

public static class LSystemRoadGenerator
{
    public static bool TryGenerate(LevelObject startObject, out string errorMessage)
    {
        return TryGenerate(startObject, LSystemRoadSettings.Default, out errorMessage);
    }

    public static bool TryGenerate(LevelObject startObject, LSystemRoadSettings settings, out string errorMessage)
    {
        return TryGenerate(startObject, settings, out _, out errorMessage);
    }

    public static bool TryGenerate(LevelObject startObject, LSystemRoadSettings settings, out List<Transform> generatedRoads, out string errorMessage)
    {
        if (settings == null)
        {
            generatedRoads = new List<Transform>();
            errorMessage = "Road growth settings are required.";
            return false;
        }

        RoadHelper? roadHelper = RoadHelper.Create(settings);
        if (roadHelper == null)
        {
            generatedRoads = new List<Transform>();
            errorMessage = "Could not load the configured L-System road assets.";
            return false;
        }

        Random.State previousRandomState = Random.state;
        Random.InitState(settings.Seed);
        try
        {
            LevelObjects.step++;
            new LSystemVisualizer(roadHelper, startObject, settings).Start();
        }
        finally
        {
            Random.state = previousRandomState;
        }

        generatedRoads = roadHelper.PlacedRoads;
        errorMessage = string.Empty;
        return true;
    }
}