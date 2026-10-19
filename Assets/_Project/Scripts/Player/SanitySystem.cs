using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.Player
{
    public class SanitySystem : MonoBehaviour
    {
        [Header("Config")]
        public float sanity = 0f; // 0..100
        public float gainPerSecDeep = 0.8f; // after 50m
        public float lossPerSecLight = 0.5f;
        public float gainPerDrillDark = 2f;

        void Update()
        {
            var drone = GetComponent<DroneController>();
            if (drone == null) return;
            int depth = drone.Depth;
            if (depth >= 30) // 500м в концепте = 30 тайлов в MVP
            {
                sanity += gainPerSecDeep * Time.deltaTime;
            }
            else
            {
                sanity = Mathf.Max(0, sanity - lossPerSecLight * Time.deltaTime);
            }
            sanity = Mathf.Clamp(sanity, 0, 100);
            var stage = StageFrom(sanity);
            EventBus.Publish(new SanityChanged(sanity, stage));
            if (sanity >= 100) Panic();
        }

        SanityStage StageFrom(float s)
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
            Debug.Log("[Sanity] Blackout 5s!");
            // stun handled by Overheat or separate
        }

        public void UseBioGel() { sanity = Mathf.Max(0, sanity - 30); EventBus.Publish(new SanityChanged(sanity, StageFrom(sanity))); }
    }
}
