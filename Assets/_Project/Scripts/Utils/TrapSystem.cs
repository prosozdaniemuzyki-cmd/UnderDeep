using UnityEngine;
using UnderDeep.Core;
using UnderDeep.World;
using UnderDeep.Player;

namespace UnderDeep.Utils
{
    /// <summary>Обрабатывает эффекты ловушек при разрушении блока. Вешается на WorldGrid.</summary>
    // Живёт в Utils (Assembly-CSharp): эффекты бьют по Player (урон/яд/стан), а World → Player запрещено контрактом (asmdef-аудит 1.2.1)
    public class TrapSystem : MonoBehaviour
    {
        private WorldGrid _world;

        void Awake() => _world = GetComponent<WorldGrid>();
        void OnEnable() { EventBus.Subscribe<BlockMined>(OnMined); }
        void OnDisable() { EventBus.Unsubscribe<BlockMined>(OnMined); }

        void OnMined(BlockMined e)
        {
            if (e.type == BlockType.GasVein) TriggerGas(e.pos);
            if (e.type == BlockType.Wire) TriggerWire(e.pos);
            if (e.type == BlockType.CrystalWall) CheckCrystalExplosion(e.pos);
        }

        void TriggerGas(Vector2Int pos)
        {
            Debug.Log($"[Trap] Gas vein at {pos} — яд 3с");
            if (ServiceLocator.TryGet<DroneController>(out var drone))
            {
                var worldPos = _world.GridToWorld(pos);
                if (Vector2.Distance(drone.transform.position, worldPos) < 2.5f)
                {
                    // наносим периодический урон
                    if (drone.TryGetComponent<GasPoison>(out var poison)) poison.Apply(3f, 0.5f);
                    else
                    {
                        var p = drone.gameObject.AddComponent<GasPoison>();
                        p.Apply(3f, 0.5f);
                    }
                }
                // визуальный эффект — зелёное облако
                EventBus.Publish(new TrapTriggered(pos, TrapType.Gas));
            }
        }

        void TriggerWire(Vector2Int pos)
        {
            Debug.Log($"[Trap] Wire at {pos} — удар током");
            if (ServiceLocator.TryGet<DroneController>(out var drone))
            {
                var wpos = _world.GridToWorld(pos);
                if (Vector2.Distance(drone.transform.position, wpos) < 1.2f)
                {
                    if (drone.TryGetComponent<HealthSystem>(out var hp)) hp.TakeDamage(1);
                    if (drone.TryGetComponent<DroneController>(out var ctrl))
                        drone.StartCoroutine(ShockStun(ctrl, 0.6f));
                    EventBus.Publish(new TrapTriggered(pos, TrapType.Wire));
                }
            }
        }

        System.Collections.IEnumerator ShockStun(DroneController ctrl, float t)
        {
            ctrl.enabled = false;
            yield return new WaitForSeconds(t);
            ctrl.enabled = true;
        }

        void CheckCrystalExplosion(Vector2Int pos)
        {
            // если рядом много кристаллов — цепная реакция (редко)
            if (Random.value < 0.05f)
                Debug.Log("[Trap] Crystal chain! (stub)");
        }
    }

    public class GasPoison : MonoBehaviour
    {
        float _time, _tick;
        int _dmg;

        public void Apply(float duration, float tickInterval)
        {
            _time = duration; _tick = tickInterval;
            _dmg = 1;
        }

        void Update()
        {
            if (_time <= 0) { Destroy(this); return; }
            _time -= Time.deltaTime;
            _tick -= Time.deltaTime;
            if (_tick <= 0)
            {
                _tick = 0.5f;
                if (TryGetComponent<HealthSystem>(out var h)) h.TakeDamage(_dmg);
                // экран зеленеет
                EventBus.Publish(new TrapTriggered(Vector2Int.zero, TrapType.GasTick));
            }
        }
    }
    // TrapType / TrapTriggered перенесены в Core/EventBus.cs
}
