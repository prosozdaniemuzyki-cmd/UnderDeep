using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.Progression
{
    /// <summary>Финал на ядре (1000м). Два варианта. После финала — бесконечный режим.</summary>
    public class EndingManager : MonoBehaviour
    {
        void OnEnable() => EventBus.Subscribe<DepthChanged>(OnDepth);
        void OnDisable() => EventBus.Unsubscribe<DepthChanged>(OnDepth);

        void OnDepth(DepthChanged e)
        {
            if (e.depth < 98) return; // 98 из 100 = ядро
            TriggerEnding();
        }

        void TriggerEnding()
        {
            var save = ServiceLocator.Get<SaveService>();
            bool hasAllArtifacts = save.Meta.artifacts.Count >= 5;
            Debug.Log($"[Ending] Ядро достигнуто! Артефактов {save.Meta.artifacts.Count}/5 HasAll={hasAllArtifacts}");
            var gsm = ServiceLocator.Get<GameStateMachine>();
            gsm.SetState(GameState.Ending);
            EventBus.Publish(new RunEnded(hasAllArtifacts? RunEndReason.Escape : RunEndReason.CoreActivate, gsm.MaxDepthThisRun));
            // показать UI выбора (в реальном UI — две кнопки)
            // пока — авто:
            if (hasAllArtifacts) EscapeEnding(); else SacrificeEnding();
        }

        void SacrificeEnding()
        {
            Debug.Log("[Ending] LONE-1 жертвует собой. Планета возрождается.");
            // ресет с бонусом
            Time.timeScale = 0.2f;
            Invoke(nameof(ReturnToBase), 2f);
        }
        void EscapeEnding()
        {
            Debug.Log("[Ending] LONE-1 улетает на корабле. Открыт бесконечный режим.");
            var save = ServiceLocator.Get<SaveService>();
            if (!save.Meta.purchasedUpgrades.Contains("infinite_unlocked"))
            {
                save.Meta.purchasedUpgrades.Add("infinite_unlocked");
                _ = save.PersistMetaAsync();
            }
            ReturnToBase();
        }
        void ReturnToBase()
        {
            Time.timeScale = 1;
            ServiceLocator.Get<GameStateMachine>().SetState(GameState.Base);
            UnityEngine.SceneManagement.SceneManager.LoadScene("Base");
        }
    }

    public class InfiniteMode : MonoBehaviour
    {
        // после финала — процедурка без дна
        public int chunkHeight = 50;
        private World.WorldGrid _world;
        void Awake() => _world = FindFirstObjectByType<World.WorldGrid>();
        void Update()
        {
            if (!ServiceLocator.TryGet<SaveService>(out var s)) return;
            if (!s.Meta.purchasedUpgrades.Contains("infinite_unlocked")) return;
            if (!ServiceLocator.TryGet<Player.DroneController>(out var d)) return;
            if (d.Depth > _world.height - 10)
            {
                // расширяем мир (заглушка — логируем)
                Debug.Log("[Infinite] Расширение мира (процедурка без дна) — TODO: сдвиг грида");
            }
        }
    }
}
