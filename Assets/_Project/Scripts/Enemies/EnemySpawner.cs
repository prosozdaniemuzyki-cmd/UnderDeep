using System.Collections.Generic;
using UnityEngine;
using UnderDeep.Core;
using UnderDeep.World;

namespace UnderDeep.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        public RustWorm wormPrefab;
        public Transform poolParent;
        private readonly List<EnemyBase> _active = new();

        void Start()
        {
            // spawn based on biome
            InvokeRepeating(nameof(TrySpawn), 3f, 5f);
        }

        void TrySpawn()
        {
            var gsm = ServiceLocator.Get<GameStateMachine>();
            if (gsm.Current != GameState.Run) return;
            var drone = ServiceLocator.Get<Player.DroneController>();
            var world = ServiceLocator.Get<WorldGrid>();
            int depth = drone.Depth;
            float chance = depth switch { <20 => 0.3f, <40 => 0.2f, _ => 0.1f };
            if (Random.value > chance) return;
            var spawnGrid = new Vector2Int(Random.Range(0, world.width), depth + Random.Range(5,10));
            if (!world.InBounds(spawnGrid)) return;
            if (!world.IsAir(spawnGrid)) return;
            var pos = world.GridToWorld(spawnGrid);
            var worm = Instantiate(wormPrefab, pos, Quaternion.identity, poolParent);
            _active.Add(worm);
        }
    }
}
