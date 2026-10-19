using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.Player
{
    public class HealthSystem : MonoBehaviour
    {
        public int maxHp = 3;
        public int hp;
        private bool _shieldUsed;

        void Awake() { hp = maxHp; }
        void Start()
        {
            // check shield upgrade
            if (ServiceLocator.TryGet<SaveService>(out var save))
                _shieldUsed = !save.Meta.purchasedUpgrades.Contains("shield");
            EventBus.Publish(new HealthChanged(hp, maxHp));
        }

        public void TakeDamage(int dmg)
        {
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

        void Die()
        {
            Debug.Log("[Health] DEAD");
            var gsm = ServiceLocator.Get<GameStateMachine>();
            gsm.SetState(GameState.Death);
            EventBus.Publish(new RunEnded(RunEndReason.Death, gsm.MaxDepthThisRun));
            // clear run loot
            if (ServiceLocator.TryGet<PlayerInventory>(out var inv)) inv.ApplyToMeta(false);
        }

        public void Revive()
        {
            hp = Mathf.Max(1, maxHp-1);
            EventBus.Publish(new HealthChanged(hp, maxHp));
            ServiceLocator.Get<GameStateMachine>().SetState(GameState.Run);
        }
    }
}
