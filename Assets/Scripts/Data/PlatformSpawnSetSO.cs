using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public struct PlatformSpawnEntry
    {
        [SerializeField] private PlatformDataSO _data;
        [SerializeField] private float _weight;

        public PlatformDataSO Data => _data;
        public float Weight => _weight;
    }

    [CreateAssetMenu(menuName = "Game/Platform Spawn Set", fileName = "PlatformSpawnSet")]
    public class PlatformSpawnSetSO : ScriptableObject
    {
        [SerializeField] private PlatformSpawnEntry[] _entries;

        public IReadOnlyList<PlatformSpawnEntry> Entries => _entries;

        public PlatformDataSO GetRandomPlatform()
        {
            float totalWeight = 0f;
            for (int i = 0; i < _entries.Length; i++)
            {
                totalWeight += _entries[i].Weight;
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            for (int i = 0; i < _entries.Length; i++)
            {
                cumulative += _entries[i].Weight;
                if (roll <= cumulative)
                {
                    return _entries[i].Data;
                }
            }

            return _entries[_entries.Length - 1].Data;
        }
    }
}
