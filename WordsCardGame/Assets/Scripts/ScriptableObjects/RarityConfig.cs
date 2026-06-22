using System;
using System.Collections.Generic;
using UnityEngine;
using Palabrario.Data;


[CreateAssetMenu(fileName = "RarityConfig", menuName = "Palabrario/Rarity Config")]
public class RarityConfig : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public RARITY rarity;
        public Color color = Color.white;          // card color (pillar #1)
        public Material holographicMaterial;        // optional: shine/holo (shiny, legendaries)
    }

    [Tooltip("One entry per Rarity value (Common..Legendary).")]
    [SerializeField] private Entry[] entries;

    // Fallback color for cards without a rarity (those in the reserved pool).
    [SerializeField] private Color defaultColor = new Color(0.4f, 0.4f, 0.4f);

    private Dictionary<RARITY, Entry> _index;

    private void OnEnable() => Reindex();

    private void Reindex()
    {
        _index = new Dictionary<RARITY, Entry>();
        if (entries == null) return;
        foreach (var e in entries)
            _index[e.rarity] = e;
    }

    /// <summary>Color for the rarity. If null (reserved card) or the entry is
    /// missing, returns the default color.</summary>
    public Color GetColor(RARITY? rarity)
    {
        if (rarity.HasValue && Find(rarity.Value) is { } e)
            return e.color;
        return defaultColor;
    }

    public Material GetMaterial(RARITY? rarity)
        => rarity.HasValue && Find(rarity.Value) is { } e ? e.holographicMaterial : null;

    private Entry Find(RARITY r)
    {
        if (_index == null) Reindex();
        _index.TryGetValue(r, out var e);
        return e;
    }
}
