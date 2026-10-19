using System.Collections.Generic;
using UnityEngine;
using UnderDeep.Core;
using UnderDeep.World;

namespace UnderDeep.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        public RustWorm wormPrefab;
        public FireflyGuardian fireflyPrefab;
        public CrystalSpider spiderPrefab;
        public Hallucination hallucinationPrefab;
        public Transform poolParent;
        private readonly List<EnemyBase> _active = new();

        void Start()
        {
            InvokeRepeating(nameof(TrySpawn), 3f, 4f);
        }

        void TrySpawn()
        {
            var gsm = ServiceLocator.Get<GameStateMachine>();
            if (gsm.Current != GameState.Run) return;
            var drone = ServiceLocator.Get<Player.DroneController>();
            var world = ServiceLocator.Get<WorldGrid>();
            if (drone == null || world == null) return;
            int depth = drone.Depth;
            var biome = GameStateMachine.DepthToBiome(depth);

            // выбор врага по биому
            EnemyBase prefab = null;
            float chance = 0.25f;
            switch (biome)
            {
                case BiomeId.Metal:
                case BiomeId.Surface: prefab = wormPrefab; chance = 0.35f; break;
                case BiomeId.BioCaves: prefab = fireflyPrefab; chance = 0.3f; break;
                case BiomeId.Crystal: prefab = spiderPrefab; chance = 0.3f; break;
                case BiomeId.Infection:
                case BiomeId.Core: prefab = hallucinationPrefab; chance = 0.4f; break;
            }
            if (prefab == null) return;
            if (Random.value > chance) return;

            // ищем пустую клетку рядом с игроком
            Vector2Int spawnGrid = Vector2Int.zero;
            bool found = false;
            for(int i=0;i<10;i++)
            {
                var cand = new Vector2Int(Random.Range(0, world.width), depth + Random.Range(3,10));
                if (!world.InBounds(cand)) continue;
                if (world.IsAir(cand)) { spawnGrid=cand; found=true; break; }
            }
            if (!found) return;
            var pos = world.GridToWorld(spawnGrid);
            var enemy = Instantiate(prefab, pos, Quaternion.identity, poolParent);
            // задать цвет по биому если нет спрайта
            var sr = enemy.GetComponentInChildren<SpriteRenderer>();
            if (sr && sr.sprite==null) sr.color = biome switch { BiomeId.BioCaves=>Color.yellow, BiomeId.Crystal=>Color.cyan, BiomeId.Infection=>new Color(1,0.4f,0.8f), _=>Color.red };
            _active.Add(enemy);
            _active.RemoveAll(e=> e==null);
            if (_active.Count > 12) { var old=_active[0]; _active.RemoveAt(0); Destroy(old.gameObject); }
        }
    }
}
