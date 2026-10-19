using System.Collections.Generic;
using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.Progression
{
    public class UpgradeSystem : MonoBehaviour
    {
        public List<UpgradeDef> allUpgrades;
        private SaveService _save;

        void Awake() => ServiceLocator.Register(this);
        void Start() => _save = ServiceLocator.Get<SaveService>();

        public bool CanPurchase(UpgradeDef def)
        {
            if (_save.Meta.purchasedUpgrades.Contains(def.id)) return false;
            if (!string.IsNullOrEmpty(def.prereqId) && !_save.Meta.purchasedUpgrades.Contains(def.prereqId)) return false;
            foreach(var c in def.costs) if (_save.Meta.Get(c.type) < c.amount) return false;
            return true;
        }

        public bool TryPurchase(UpgradeDef def)
        {
            if (!CanPurchase(def)) return false;
            foreach(var c in def.costs) _save.Meta.Consume(c.type, c.amount);
            _save.Meta.purchasedUpgrades.Add(def.id);
            _ = _save.PersistMetaAsync();
            EventBus.Publish(new UpgradePurchased(def.id));
            Debug.Log($"[Upgrade] Purchased {def.id}");
            return true;
        }

        public IReadOnlyList<UpgradeDef> All => allUpgrades;
    }
}
