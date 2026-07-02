using System.Text;
using EditorHelper2.common.Types.LSystem;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EditorHelper2.common.Helpers.LSystem;

public sealed class LSystemGenerator
{
    private readonly LSystemRule[] _rules;
    private readonly string _rootSentence;
    private readonly int _iterationLimit;

    public LSystemGenerator(LSystemRule[] rules, string rootSentence, int iterationLimit)
    {
        _rules = rules;
        _rootSentence = rootSentence;
        _iterationLimit = iterationLimit;
    }

    public string GenerateSentence(string? word = null)
    {
        return GrowRecursive(word ?? _rootSentence);
    }

    private string GrowRecursive(string word, int iterationIndex = 0)
    {
        if (iterationIndex >= _iterationLimit) return word;

        StringBuilder stringBuilder = new();
        foreach (char c in word)
        {
            stringBuilder.Append(c);
            ProcessRulesRecursively(stringBuilder, c, iterationIndex);
        }

        return stringBuilder.ToString();
    }

    private void ProcessRulesRecursively(StringBuilder stringBuilder, char character, int iterationIndex)
    {
        foreach (LSystemRule rule in _rules)
        {
            if (rule.Letter != character) continue;
            if (iterationIndex > 0 && Random.value < rule.ChanceToIgnoreRule) return;

            stringBuilder.Append(GrowRecursive(rule.GetResult(), ++iterationIndex));
        }
    }
}