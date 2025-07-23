using UnityEngine;

namespace EditorHelper.LSystem;

public class Rule
{
    public char Letter;
    private readonly string[] results = null;
    private readonly bool randomResult = true;
    public float chanceToIgnoreRule = 0.3f;


    public Rule(char letter, string[] results, bool randomResult = true, float ignoreChance = 0.3f)
    {
        Letter = letter;
        this.results = results;
        this.randomResult = randomResult;
        chanceToIgnoreRule = ignoreChance;
    }
    
    public string GetResult()
    {
        if (randomResult)
        {
            return results[Random.Range(0, results.Length)];
        }
        
        return results[0];
    }
}