using System.Text;
using UnityEngine;

namespace EditorHelper.LSystem;

public class LSystemGenerator
{
    private readonly Rule[] _rules;
    private readonly string _rootSentence;

    private readonly int _iterationLimit;

    public LSystemGenerator(Rule[] rules, string rootSentence, int iterationLimit)
    {
        _rules = rules;
        _rootSentence = rootSentence;
        _iterationLimit = iterationLimit;
    }

    public string GenerateSentence(string word = null)
    {
        if (word == null)
        {
            word = _rootSentence;
        }

        return GrowRecursive(word);
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
        foreach (Rule rule in _rules)
        {
            if (rule.Letter == character)
            {
                if (iterationIndex > 0 && Random.value < rule.chanceToIgnoreRule)
                {
                    return;
                }
                stringBuilder.Append(GrowRecursive(rule.GetResult(), ++iterationIndex));
            }
        }
    }
}