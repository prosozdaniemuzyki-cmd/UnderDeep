using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.Player
{
    public class FuelSystem : MonoBehaviour
    {
        [Header("Config")]
        public float maxFuel = 100f;
        public float fuelPerSecDrill = 5f;
        public float evacCost = 10f;

        public float Current { get; private set; }
        public bool IsEmpty => Current <= 0.01f;

        void Awake() { Current = maxFuel; }
        void Start() => ApplyUpgrades();

        void ApplyUpgrades()
        {
            if (!ServiceLocator.TryGet<SaveService>(out var save)) return;
            int batteries = 0;
            foreach(var id in save.Meta.purchasedUpgrades) if(id.StartsWith("battery")) batteries++;
            maxFuel = 100 + batteries * 20;
            Current = Mathf.Min(Current, maxFuel);
        }

        public void SetMaxFromUpgrade(int extra) { maxFuel += extra; Publish(); }

        public bool Consume(float amount)
        {
            if (Current <= 0) return false;
            Current = Mathf.Max(0, Current - amount);
            Publish(amount);
            return true;
        }

        public void Refill(float amt) { Current = Mathf.Min(maxFuel, Current + amt); Publish(amt); }
        public void RefillByCrystal(int crystals) => Refill(crystals * 15f);

        void Publish(float delta=0) => EventBus.Publish(new FuelChanged(Current, maxFuel, -delta));

        // called by DrillSystem
        public bool TryConsumeDrill(float dt) => Consume(fuelPerSecDrill * dt);

        public bool TryPayEvac()
        {
            if (Current >= evacCost) { Consume(evacCost); return true; }
            // try crystal
            if (ServiceLocator.TryGet<SaveService>(out var s) && s.Meta.crystal>0)
            { s.Meta.crystal--; return true; }
            return false;
        }
    }
}
