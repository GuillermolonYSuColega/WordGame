using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Card : MonoBehaviour
{
    [Header("Front")]
    [SerializeField] private Image backgroundRarityColor;           
    [SerializeField] private TMP_Text wordText;
    [SerializeField] private Image iconCategory;

    [Header("Icons by Category")]
    [SerializeField] private Sprite iconNoun; 
    [SerializeField] private Sprite iconVerb;      
    [SerializeField] private Sprite iconAdjective;  
    [SerializeField] private Sprite iconOther;

    private static readonly Dictionary<RARITY, Color> ColorByRarity = new()
    {
        { RARITY.Common,      Hex("#9CA3AF") },
        { RARITY.Uncommon,    Hex("#22C55E") },
        { RARITY.Rare,        Hex("#3B82F6") },
        { RARITY.Epic,        Hex("#A855F7") },
        { RARITY.Legendary,   Hex("#F59E0B") },
    };

    public void Paint(WordData w)
    {
        wordText.text = w.word;

        if (w.rarity.HasValue && ColorByRarity.TryGetValue(w.rarity.Value, out Color c))
            backgroundRarityColor.color = c;

        iconCategory.sprite = GetIconFor(w.category);
    }

    private Sprite GetIconFor(CATEGORY c) => c switch
    {
        CATEGORY.Noun      => iconNoun,
        CATEGORY.Verb      => iconVerb,
        CATEGORY.Adjective => iconAdjective,
        _                  => iconOther,
    };

    private static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString(h, out Color c);
        return c;
    }
}