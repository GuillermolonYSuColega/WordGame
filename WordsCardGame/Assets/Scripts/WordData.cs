using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public enum CATEGORY
{
    Noun,   
    Verb,        
    Adjective,     
    Adverb,
    Locution,
    Unknown
}

public enum RARITY
{
    Common,        // #9CA3AF  
    Uncommon,      // #22C55E
    Rare,          // #3B82F6
    Epic,          // #A855F7
    Legendary      // #F59E0B
}

[Serializable]
public class WordData
{
    [JsonProperty("id")] 
    public int id;

    [JsonProperty("word")] 
    public string word;     

    [JsonProperty("normalizedWord")] 
    public string normalizedWord;         

    [JsonProperty("category")]
    [JsonConverter(typeof(StringEnumConverter))]
    public CATEGORY category;

    [JsonProperty("categorySource")] 
    public string categorySource;

    [JsonProperty("rarity")]
    [JsonConverter(typeof(StringEnumConverter))]
    public RARITY? rarity;

    [JsonProperty("zipfFrequency")] 
    public float zipfFrequency;

    [JsonProperty("includedInPool")] 
    public bool includedInPool;   

    [JsonProperty("isLocution")] 
    public bool isLocution;

    [JsonProperty("regimen")] 
    public List<string> regimen = new();

    [JsonProperty("regimenSource")] 
    public string regimenSource;   

    [JsonProperty("region")] 
    public string region;          

    [JsonProperty("isPoetic")] 
    public bool? isPoetic;        

    [JsonProperty("shortDefinition")] 
    public string shortDefinition;   

    [JsonProperty("fullDefinition")] 
    public string fullDefinition;

    // --- Convenience accessors for the card back (ignored in JSON serialization) ---

    [JsonIgnore] 
    public bool IsTransitive => regimen != null && regimen.Contains("transitivo");

    [JsonIgnore] 
    public bool IsIntransitive => regimen != null && regimen.Contains("intransitivo");

    [JsonIgnore] 
    public bool IsPronominal => regimen != null && regimen.Contains("pronominal");

    public string GrammaticalDescription()
    {
        string baseCat = category switch
        {
            CATEGORY.Noun => "Sustantivo",
            CATEGORY.Verb => "Verbo",
            CATEGORY.Adjective => "Adjetivo",
            CATEGORY.Adverb => "Adverbio",
            CATEGORY.Locution => "Locución",
            _ => "",
        };

        if (category != CATEGORY.Verb || regimen == null || regimen.Count == 0)
            return baseCat;

        return $"{baseCat} {JoinNaturally(regimen)}";
    }

    private static string JoinNaturally(List<string> items)
    {
        if (items.Count == 1) return items[0];
        string last = items[items.Count - 1];
        string connector = last.StartsWith("i") || last.StartsWith("hi") ? "e" : "y";
        string previous = string.Join(", ", items.GetRange(0, items.Count - 1));
        return $"{previous} {connector} {last}";
    }
}

[Serializable]
public class CardInProperty
{
    [JsonProperty("wordId")] 
    public int wordId;        

    [JsonProperty("masteryLevel")] 
    public int masteryLevel;    

    [JsonProperty("isShiny")] 
    public bool isShiny;         

    [JsonProperty("obtainedDate")] 
    public long obtainedDate;   

    [JsonProperty("timesUsed")] 
    public int timesUsed;      
}