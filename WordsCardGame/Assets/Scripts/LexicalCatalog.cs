using System;
using System.Collections.Generic;
using System.Linq;

namespace Palabrario.Data
{
    public class LexicalCatalog
    {
        private readonly Dictionary<int, WordData> _byId = new();
        private readonly Dictionary<RARITY, List<WordData>> _pool = new();

        public int TotalCount { get; private set; }
        public int TotalInPool { get; private set; }

        public Dictionary<RARITY, float> DropWeights = new()
        {
            { RARITY.Common,     60f },
            { RARITY.Uncommon,   25f },
            { RARITY.Rare,       10f },
            { RARITY.Epic,        4f },
            { RARITY.Legendary,   1f },
        };

        private readonly Random _rng = new();

        public LexicalCatalog(IEnumerable<WordData> data)
        {
            foreach (RARITY r in Enum.GetValues(typeof(RARITY)).Cast<RARITY>())
                _pool[r] = new List<WordData>();

            foreach (WordData w in data)
            {
                _byId[w.id] = w;
                TotalCount++;
                if (w.includedInPool && w.rarity.HasValue)
                {
                    _pool[w.rarity.Value].Add(w);
                    TotalInPool++;
                }
            }
        }

        public WordData GetById(int id) =>
            _byId.TryGetValue(id, out WordData w) ? w : null;

        public IReadOnlyList<WordData> GetByRarity(RARITY r) => _pool[r];

        public WordData DrawCard()
        {
            RARITY tier = ChooseTier();
            List<WordData> list = _pool[tier];
            return list.Count == 0 ? null : list[_rng.Next(list.Count)];
        }

        private RARITY ChooseTier()
        {
            float total = 0f;
            foreach (KeyValuePair<RARITY, float> kv in DropWeights)
                if (_pool[kv.Key].Count > 0) total += kv.Value;

            float t = (float)(_rng.NextDouble() * total);
            foreach (KeyValuePair<RARITY, float> kv in DropWeights)
            {
                if (_pool[kv.Key].Count == 0) continue;
                if (t < kv.Value) return kv.Key;
                t -= kv.Value;
            }
            return RARITY.Common;
        }
    }
}