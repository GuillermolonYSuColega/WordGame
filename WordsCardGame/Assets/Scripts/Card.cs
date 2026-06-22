using UnityEngine;
using TMPro;

public class Card : MonoBehaviour
{
    [Header("Front")]
    [SerializeField] private SpriteRenderer backgroundRarityColor;
    [SerializeField] private TMP_Text wordText;
    [SerializeField] private SpriteRenderer iconCategory;

    [Header("Config (ScriptableObjects)")]
    [SerializeField] private RarityConfig rarityConfig;
    [SerializeField] private CategoryConfig categoryConfig;

    public void Paint(WordData w)
    {
        wordText.text = w.word;
        backgroundRarityColor.color = rarityConfig.GetColor(w.rarity);
        iconCategory.sprite = categoryConfig.GetIcon(w.category);
    }
}
