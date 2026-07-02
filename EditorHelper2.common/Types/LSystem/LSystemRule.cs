using Random = UnityEngine.Random;

namespace EditorHelper2.common.Types.LSystem;

public sealed class LSystemRule
{
    public readonly char Letter;
    private readonly string[] _results;
    private readonly bool _randomResult;
    public readonly float ChanceToIgnoreRule;

    public LSystemRule(char letter, string[] results, bool randomResult = true, float ignoreChance = 0.3f)
    {
        Letter = letter;
        _results = results;
        _randomResult = randomResult;
        ChanceToIgnoreRule = ignoreChance;
    }

    public string GetResult()
    {
        return _randomResult ? _results[Random.Range(0, _results.Length)] : _results[0];
    }
}