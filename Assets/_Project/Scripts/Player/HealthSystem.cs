using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.Player
{
    public class HealthSystem : MonoBehaviour
    {
        public int maxHp = 3;
        public int hp;
        private bool _shieldUsed;
        private bool _dead;

        void Awake()
        {
            hp = maxHp;
            ServiceLocator.Register(this);
        }
        void Start()
        {
            // check shield upgrade
            if (ServiceLocator.TryGet<SaveService>(out var save))
                _shieldUsed = !save.Meta.purchasedUpgrades.Contains("shield");
            EventBus.Publish(new HealthChanged(hp, maxHp));
        }

        public void TakeDamage(int dmg)
        {
            if (_dead || dmg <= 0) return;
            if (dmg > 0 && !_shieldUsed)
            {
                // щит спасает 1 раз от смертельного
                _shieldUsed = true;
                Debug.Log("[Health] Щит сработал! Спас от урона");
                EventBus.Publish(new HealthChanged(hp, maxHp));
                return;
            }
            hp -= dmg;
            hp = Mathf.Clamp(hp, 0, maxHp);
            EventBus.Publish(new HealthChanged(hp, maxHp));
            if (hp <= 0) Die();
        }

        public void Heal(int amt)
        {
            hp = Mathf.Clamp(hp + amt, 0, maxHp);
            EventBus.Publish(new HealthChanged(hp, maxHp));
        }

        public void ForceDeath()
        {
            if (_dead) return;
            hp = 0;
            EventBus.Publish(new HealthChanged(hp, maxHp));
            Die();
        }

        void Die()
        {
            if (_dead) return;
            _dead = true;
            Debug.Log("[Health] DEAD");
            if (TryGetComponent<DroneController>(out var drone)) drone.enabled = false;
            if (TryGetComponent<DrillSystem>(out var drill)) drill.enabled = false;
            if (ServiceLocator.TryGet<GameStateMachine>(out var gsm))
            {
                gsm.SetState(GameState.Death);
                EventBus.Publish(new RunEnded(RunEndReason.Death, gsm.MaxDepthThisRun));
            }
        }

        public void Revive()
        {
            _dead = false;
            hp = Mathf.Max(1, maxHp-1);
            EventBus.Publish(new HealthChanged(hp, maxHp));
            ServiceLocator.Get<GameStateMachine>().SetState(GameState.Run);
        }
    }
}
