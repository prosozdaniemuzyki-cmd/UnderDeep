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
        public int depletedRescueCrystalCost = 20;

        public float Current { get; private set; }
        public bool IsEmpty => Current <= 0.01f;
        public bool IsDepleted { get; private set; }
        public bool CanRefuelFromCrystal => !IsDepleted && Current >= evacCost &&
            Current < maxFuel - 0.01f && fuelPerRunCrystal > 0 &&
            ServiceLocator.TryGet<PlayerInventory>(out var inventory) && inventory.Get(ResourceType.Crystal) > 0;
        public bool CanPayEvac => Current >= evacCost;
        public bool CanPayDepletedRescue => Current < evacCost && depletedRescueCrystalCost > 0 &&
            ServiceLocator.TryGet<SaveService>(out var save) && save.Meta.crystal >= depletedRescueCrystalCost;

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
            foreach (var id in save.Meta.purchasedUpgrades)
                if (id.StartsWith("battery")) batteries++;
            maxFuel = 100 + batteries * 20;
            Current = maxFuel;
            IsDepleted = false;
            Publish();
        }

        public void SetMaxFromUpgrade(int extra)
        {
            maxFuel += extra;
            Publish();
        }

        public bool Consume(float amount)
        {
            if (IsDepleted || amount <= 0) return false;
            Current = Mathf.Max(0, Current - amount);
            if (Current <= 0.01f)
            {
                Current = 0;
                IsDepleted = true;
            }
            Publish(amount);
            return true;
        }

        public void Refill(float amount)
        {
            if (IsDepleted || amount <= 0) return;
            Current = Mathf.Min(maxFuel, Current + amount);
            Publish(amount);
        }

        public bool TryRefuelFromRunCrystal()
        {
            // Keep the last evac reserve separate from the one-crystal refuel loop.
            if (IsDepleted || Current < evacCost || Current >= maxFuel - 0.01f || fuelPerRunCrystal <= 0)
                return false;
            if (!ServiceLocator.TryGet<PlayerInventory>(out var inventory) ||
                !inventory.Consume(ResourceType.Crystal, 1)) return false;
            Refill(fuelPerRunCrystal);
            Debug.Log($"[Fuel] Refueled +{fuelPerRunCrystal:0} using 1 run crystal");
            return true;
        }

        void Publish(float delta = 0) => EventBus.Publish(new FuelChanged(Current, maxFuel, -delta));

        // called by DrillSystem
        public bool TryConsumeDrill(float dt) => Consume(fuelPerSecDrill * dt);

        /// <summary>Evacuation costs fuel; below the evac reserve, the timed rescue costs persistent meta crystals.</summary>
        public bool TryPayEvac()
        {
            if (Current >= evacCost)
            {
                Consume(evacCost);
                return true;
            }

            if (depletedRescueCrystalCost <= 0 ||
                !ServiceLocator.TryGet<SaveService>(out var save)) return false;
            return save.Meta.Consume(ResourceType.Crystal, depletedRescueCrystalCost);
        }
    }
}
