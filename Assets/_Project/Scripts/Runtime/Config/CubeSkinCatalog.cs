using System;
using System.Collections.Generic;
using CubeTower.Core.Cubes;
using UnityEngine;

namespace CubeTower.Runtime.Config
{
    /// <summary>
    /// Maps cube colour ids to sprites. Visuals live in the build while gameplay config may come from anywhere.
    /// </summary>
    [CreateAssetMenu(menuName = "Cube Tower/Cube Skin Catalog", fileName = "CubeSkinCatalog")]
    public sealed class CubeSkinCatalog : ScriptableObject
    {
        [SerializeField] private List<Entry> _entries = new List<Entry>();

        [Tooltip("Used when a colour id is missing in the catalog.")]
        [SerializeField] private Sprite _fallback;

        private Dictionary<CubeColorId, Sprite> _lookup;
        private HashSet<CubeColorId> _reportedMissing;

        public IReadOnlyList<Entry> Entries => _entries;

        public bool Contains(CubeColorId id) => Lookup.ContainsKey(id);

        public Sprite GetSprite(CubeColorId id)
        {
            if (Lookup.TryGetValue(id, out var sprite))
                return sprite;

            _reportedMissing ??= new HashSet<CubeColorId>();
            if (_reportedMissing.Add(id))
                Debug.LogError($"[{nameof(CubeSkinCatalog)}] No sprite for cube colour '{id}'. Fallback sprite is used.", this);

            return _fallback;
        }

        private Dictionary<CubeColorId, Sprite> Lookup => _lookup ??= BuildLookup();

        private Dictionary<CubeColorId, Sprite> BuildLookup()
        {
            var lookup = new Dictionary<CubeColorId, Sprite>(_entries.Count);
            foreach (var entry in _entries)
            {
                if (!CubeColorId.TryCreate(entry.ColorId, out var id) || entry.Sprite == null)
                    continue;

                if (!lookup.TryAdd(id, entry.Sprite))
                    Debug.LogWarning($"[{nameof(CubeSkinCatalog)}] Duplicate colour id '{id}'.", this);
            }

            return lookup;
        }

        private void OnValidate()
        {
            _lookup = null;
            _reportedMissing = null;
        }

        [Serializable]
        public struct Entry
        {
            public string ColorId;
            public Sprite Sprite;
        }
    }
}
