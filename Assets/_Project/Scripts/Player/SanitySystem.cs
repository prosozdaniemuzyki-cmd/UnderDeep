using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.Player
{
    public class SanitySystem : MonoBehaviour
    {
        [Header("Config")]
        public float sanity = 0f; // 0..100
        public float gainPerSecDeep = 0.8f; // after 500м (30 тайлов MVP)
        public float lossPerSecLight = 0.7f;
        public float lossPerSecShallow = 1.2f;
        public float gainPerDrillDark = 2f;
        public float gainPerTrap = 5f;

        private PlayerLight _light;

        void Awake() => _light = GetComponent<PlayerLight>();

        void Update()
        {
            var drone = GetComponent<DroneController>();
            if (drone == null) return;
            int depth = drone.Depth;
            bool deep = depth >= 30;
            bool bright = _light ? _light.IsBright : true;

            if (deep)
            {
                float mult = bright ? 0.6f : 1.5f;
                sanity += gainPerSecDeep * mult * Time.deltaTime;
            }
            else
            {
                sanity = Mathf.Max(0, sanity - lossPerSecShallow * Time.deltaTime);
            }

            if (bright && sanity > 0) sanity -= lossPerSecLight * Time.deltaTime;

            sanity = Mathf.Clamp(sanity, 0, 100);
            var stage = StageFrom(sanity);
            EventBus.Publish(new SanityChanged(sanity, stage));
            if (sanity >= 100) Panic();
        }

        public SanityStage StageFrom(float s)
        {
            if (s < 25) return SanityStage.Clear;
            if (s < 50) return SanityStage.LightDistortion;
            if (s < 75) return SanityStage.FakeResources;
            if (s < 100) return SanityStage.FakeEnemies;
            return SanityStage.Blackout;
        }

        void Panic()
        {
            sanity = 70;
            Debug.Log("[Sanity] BLACKOUT 5s!");
            var drone = GetComponent<DroneController>();
            if (drone) { drone.enabled = false; drone.Invoke(nameof(RestoreControl), 5f); }
            EventBus.Publish(new SanityChanged(sanity, StageFrom(sanity)));
        }
        void RestoreControl() { var d = GetComponent<DroneController>(); if (d) d.enabled = true; }

        public void AddSanity(float amt)
        {
            sanity = Mathf.Clamp(sanity + amt, 0, 100);
            EventBus.Publish(new SanityChanged(sanity, StageFrom(sanity)));
            Debug.Log($"[Sanity] +{amt} → {sanity:F0}");
        }

        public void UseBioGel()
        {
            sanity = Mathf.Max(0, sanity - 30);
            EventBus.Publish(new SanityChanged(sanity, StageFrom(sanity)));
            if (TryGetComponent<HealthSystem>(out var h)) h.Heal(1);
        }
    }
}
