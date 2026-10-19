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
        public float fuelPerRunCrystal = 15f;

        public float Current { get; private set; }
        public bool IsEmpty => Current <= 0.01f;
        public bool CanRefuelFromCrystal => Current < maxFuel - 0.01f &&
            ServiceLocator.TryGet<PlayerInventory>(out var inventory) && inventory.Get(ResourceType.Crystal) > 0;
        public bool CanPayEvac => Current >= evacCost ||
            (ServiceLocator.TryGet<PlayerInventory>(out var inventory) && inventory.Get(ResourceType.Crystal) > 0) ||
            (ServiceLocator.TryGet<SaveService>(out var save) && save.Meta.crystal > 0);

        void Awake()
        {
            Current = maxFuel;
            ServiceLocator.Register(this);
        }
        void Start() => ApplyUpgrades();

        void ApplyUpgrades()
        {
            if (!ServiceLocator.TryGet<SaveService>(out var save)) return;
            int batteries = 0;
            foreach(var id in save.Meta.purchasedUpgrades) if(id.StartsWith("battery")) batteries++;
            maxFuel = 100 + batteries * 20;
            Current = maxFuel;
            Publish();
        }

        public void SetMaxFromUpgrade(int extra) { maxFuel += extra; Publish(); }

        public bool Consume(float amount)
        {
            if (Current <= 0) return false;
            Current = Mathf.Max(0, Current - amount);
            Publish(amount);
            return true;
        }

        public void Refill(float amt)
        {
            if (amt <= 0) return;
            Current = Mathf.Min(maxFuel, Current + amt);
            Publish(amt);
        }

        public void RefillByCrystal(int crystals) => Refill(crystals * fuelPerRunCrystal);

        public bool TryRefuelFromRunCrystal()
        {
            if (Current >= maxFuel - 0.01f || fuelPerRunCrystal <= 0) return false;
            if (!ServiceLocator.TryGet<PlayerInventory>(out var inventory) ||
                !inventory.Consume(ResourceType.Crystal, 1)) return false;
            Refill(fuelPerRunCrystal);
            Debug.Log($"[Fuel] Refueled +{fuelPerRunCrystal:0} using 1 run crystal");
            return true;
        }

        void Publish(float delta=0) => EventBus.Publish(new FuelChanged(Current, maxFuel, -delta));

        // called by DrillSystem
        public bool TryConsumeDrill(float dt) => Consume(fuelPerSecDrill * dt);

        public bool TryPayEvac()
        {
            if (Current >= evacCost) { Consume(evacCost); return true; }
            // Spend run loot before persistent crystals.
            if (ServiceLocator.TryGet<PlayerInventory>(out var inventory) &&
                inventory.Consume(ResourceType.Crystal, 1)) return true;
            if (ServiceLocator.TryGet<SaveService>(out var save) && save.Meta.Consume(ResourceType.Crystal, 1)) return true;
            return false;
        }
    }
}
