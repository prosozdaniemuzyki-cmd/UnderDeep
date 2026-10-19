using UnityEngine;
using UnderDeep.Core;
using UnderDeep.World;

namespace UnderDeep.Core
{
    public class VFXManager : MonoBehaviour
    {
        [Header("Prefabs")]
        public ParticleSystem drillParticles;
        public ParticleSystem breakParticles;
        public GameObject damageFlashPrefab;

        void OnEnable()
        {
            EventBus.Subscribe<BlockDamaged>(OnDamaged);
            EventBus.Subscribe<BlockMined>(OnMined);
            EventBus.Subscribe<HealthChanged>(OnHealth);
            EventBus.Subscribe<TrapSystem.TrapTriggered>(OnTrap);
        }
        void OnDisable()
        {
            EventBus.Unsubscribe<BlockDamaged>(OnDamaged);
            EventBus.Unsubscribe<BlockMined>(OnMined);
            EventBus.Unsubscribe<HealthChanged>(OnHealth);
            EventBus.Unsubscribe<TrapSystem.TrapTriggered>(OnTrap);
        }

        void OnDamaged(BlockDamaged e)
        {
            if (drillParticles==null) return;
            if (!ServiceLocator.TryGet<WorldGrid>(out var w)) return;
            drillParticles.transform.position = w.GridToWorld(e.pos);
            if (!drillParticles.isPlaying) drillParticles.Play();
        }
        void OnMined(BlockMined e)
        {
            if (breakParticles==null) return;
            if (!ServiceLocator.TryGet<WorldGrid>(out var w)) return;
            breakParticles.transform.position = w.GridToWorld(e.pos);
            breakParticles.Play();
            // дрожь экрана
            if (Camera.main) Camera.main.transform.position += (Vector3)Random.insideUnitCircle * 0.05f;
        }
        void OnHealth(HealthChanged e)
        {
            if (e.hp < e.maxHp && damageFlashPrefab)
            {
                // красный флеш
            }
        }
        void OnTrap(TrapSystem.TrapTriggered e)
        {
            // звук/частицы по типу
            Debug.Log($"[VFX] Trap {e.type} at {e.pos}");
        }
    }
}
