// CategoryConfig.cs
// Palabrario · Per-category PRESENTATION config (ScriptableObject).
// -----------------------------------------------------------------------------
// Small hand-authored layer: ~5 entries with the icon Sprite for each
// grammatical category. Moves the category->icon map out of Card's hardcoded code.
//
// Create the asset: right-click in Project > Create > Palabrario > Category Config.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using Palabrario.Data;

[CreateAssetMenu(fileName = "CategoryConfig", menuName = "Palabrario/Category Config")]
public class CategoryConfig : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public CATEGORY category;
        public Sprite icon;        // cube / bolt / prism / ...
    }

    [Tooltip("One entry per category (Noun, Verb, Adjective, ...).")]
    [SerializeField] private Entry[] entries;

    [SerializeField] private Sprite defaultIcon;   // for categories without an entry

    private Dictionary<CATEGORY, Sprite> _index;

    private void OnEnable() => Reindex();

    private void Reindex()
    {
        _index = new Dictionary<CATEGORY, Sprite>();
        if (entries == null) return;
        foreach (var e in entries)
            _index[e.category] = e.icon;
    }

    /// <summary>Icon for the category, or the fallback if missing.</summary>
    public Sprite GetIcon(CATEGORY category)
    {
        if (_index == null) Reindex();
        return _index.TryGetValue(category, out var s) && s != null ? s : defaultIcon;
    }
}

