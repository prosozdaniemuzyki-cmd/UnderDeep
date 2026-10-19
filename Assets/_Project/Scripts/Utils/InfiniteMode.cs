using UnityEngine;
using UnderDeep.Core;
using UnderDeep.World;
using UnderDeep.Player;

namespace UnderDeep.Utils
{
    /// <summary>Бесконечный режим после финала — процедурка без дна (пока заглушка).</summary>
    // Вынесен из Progression/EndingManager.cs: нужен WorldGrid, а Progression → World запрещено контрактом (asmdef-аудит 1.2.1)
    public class InfiniteMode : MonoBehaviour
    {
        // после финала — процедурка без дна
        public int chunkHeight = 50;
        private WorldGrid _world;
        void Awake() => _world = FindFirstObjectByType<WorldGrid>();
        void Update()
        {
            if (!ServiceLocator.TryGet<SaveService>(out var s)) return;
            if (!s.Meta.purchasedUpgrades.Contains("infinite_unlocked")) return;
            if (!ServiceLocator.TryGet<DroneController>(out var d)) return;
            if (d.Depth > _world.height - 10)
            {
                // расширяем мир (заглушка — логируем)
                Debug.Log("[Infinite] Расширение мира (процедурка без дна) — TODO: сдвиг грида");
            }
        }
    }
}
