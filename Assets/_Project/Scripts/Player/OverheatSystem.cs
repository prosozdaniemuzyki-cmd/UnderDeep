using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.Player
{
    public class OverheatSystem : MonoBehaviour
    {
        public float heatPerSec = 15f;
        public float coolPerSec = 25f;
        public float overheatThreshold = 100f;
        public float stunDuration = 3f;

        public float Heat { get; private set; }
        public bool IsStunned { get; private set; }
        private float _stunTimer;
        private bool _drilledThisFrame;

        void Update()
        {
            if (IsStunned)
            {
                _stunTimer -= Time.deltaTime;
                if (_stunTimer <= 0) IsStunned = false;
                _drilledThisFrame = false;
                return;
            }
            if (!_drilledThisFrame)
            {
                Heat = Mathf.Max(0, Heat - coolPerSec * Time.deltaTime);
                EventBus.Publish(new HeatChanged(Heat, false));
            }
            _drilledThisFrame = false;
        }

        public void AddHeat(float dt)
        {
            if (IsStunned) return;
            _drilledThisFrame = true;
            Heat += heatPerSec * dt;
            if (Heat >= overheatThreshold) TriggerOverheat();
            EventBus.Publish(new HeatChanged(Heat, IsStunned));
        }

        void TriggerOverheat()
        {
            Heat = 0;
            IsStunned = true;
            _stunTimer = stunDuration;
            // damage
            if (TryGetComponent<HealthSystem>(out var hp)) hp.TakeDamage(1);
            EventBus.Publish(new HeatChanged(Heat, true));
            Debug.Log("[Overheat] STUN 3s!");
        }
    }
}
