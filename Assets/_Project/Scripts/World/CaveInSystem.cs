using UnityEngine;
using UnderDeep.Core;
using System.Collections.Generic;

namespace UnderDeep.World
{
    /// <summary>Обвалы — при выемке ключевых блоков сверху падают камни. Детектирует отсутствие опоры.</summary>
    public class CaveInSystem : MonoBehaviour
    {
        private WorldGrid _world;
        public GameObject fallingRockPrefab; // простой спрайт с Rigidbody2D
        public float checkChance = 0.12f;

        void Awake() => _world = GetComponent<WorldGrid>();
        void OnEnable() => EventBus.Subscribe<BlockMined>(OnMined);
        void OnDisable() => EventBus.Unsubscribe<BlockMined>(OnMined);

        void OnMined(BlockMined e)
        {
            // проверяем колонну над выкопанным блоком
            for(int dy=1; dy<=4; dy++)
            {
                var above = e.pos + Vector2Int.up * dy;
                if (!_world.InBounds(above)) break;
                var data = _world.Get(above);
                if (data.type == BlockType.Air) continue;
                // если под блоком теперь воздух — шанс обвала
                var below = above + Vector2Int.down;
                if (_world.IsAir(below) && Random.value < checkChance)
                {
                    TriggerFall(above);
                    break;
                }
            }
        }

        void TriggerFall(Vector2Int pos)
        {
            Debug.Log($"[CaveIn] Falling rock at {pos}");
            EventBus.Publish(new TrapSystem.TrapTriggered(pos, TrapSystem.TrapType.CaveIn));
            // спавн падающего камня
            if (fallingRockPrefab)
            {
                var worldPos = _world.GridToWorld(pos);
                var rock = Instantiate(fallingRockPrefab, worldPos, Quaternion.identity);
                // падение вниз, наносит урон
                rock.AddComponent<FallingRock>().Init(_world, pos);
            }
            else
            {
                // без префаба — просто уничтожаем блок и наносим урон если игрок под ним
                var wpos = _world.GridToWorld(pos);
                if (ServiceLocator.TryGet<Player.DroneController>(out var d) && Vector2.Distance(d.transform.position, wpos) < 1.5f)
                    d.GetComponent<Player.HealthSystem>()?.TakeDamage(1);
                // мир уже очистится при падении? пока просто удаляем
                _world.GetType().GetMethod("CheckCaveIn", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.Invoke(_world, new object[]{pos});
            }
        }
    }

    public class FallingRock : MonoBehaviour
    {
        private WorldGrid _world;
        private Vector2Int _origin;
        private float _fallSpeed = 6f;
        public void Init(WorldGrid w, Vector2Int pos){ _world=w; _origin=pos; gameObject.AddComponent<BoxCollider2D>().isTrigger=true; }

        void Update()
        {
            transform.position += Vector3.down * _fallSpeed * Time.deltaTime;
            var grid = _world.WorldToGrid(transform.position);
            if (!_world.IsAir(grid) && grid != _origin)
            {
                // ударился
                Destroy(gameObject, 0.1f);
            }
            // урон игроку
            if (ServiceLocator.TryGet<Player.DroneController>(out var d) && Vector2.Distance(transform.position, d.transform.position) < 0.6f)
            {
                d.GetComponent<Player.HealthSystem>()?.TakeDamage(1);
                Destroy(gameObject);
            }
        }
    }
}
