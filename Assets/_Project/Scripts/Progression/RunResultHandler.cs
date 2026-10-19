using System.Threading.Tasks;
using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.Progression
{
    public class RunResultHandler : MonoBehaviour
    {
        private bool _handledRunEnd;

        void Awake() => ServiceLocator.Register(this);

        void OnEnable()
        {
            EventBus.Subscribe<RunEnded>(OnRunEnded);
            EventBus.Subscribe<EvacRequested>(OnEvacRequested);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<RunEnded>(OnRunEnded);
            EventBus.Unsubscribe<EvacRequested>(OnEvacRequested);
        }

        // Player sends an event rather than depending directly on this progression system.
        void OnEvacRequested(EvacRequested e) => Evacuate();

        async void OnRunEnded(RunEnded e)
        {
            if (_handledRunEnd) return;
            _handledRunEnd = true;

            var save = ServiceLocator.Get<SaveService>();
            bool keepLoot = e.reason == RunEndReason.Evac;
            if (ServiceLocator.TryGet<Player.PlayerInventory>(out var inventory))
                inventory.ApplyToMeta(keepLoot);

            save.Meta.totalRuns++;
            await save.SubmitDepthAsync(e.depth);
            await save.PersistMetaAsync();
            Debug.Log($"[Run] Ended {e.reason} depth={e.depth} keep={keepLoot}");
        }

        /// <summary>Evacuate for 10 fuel, or one crystal from this run / meta save.</summary>
        public async void Evacuate()
        {
            if (!TryGetActiveRun(out var state, out var fuel)) return;
            if (!fuel.TryPayEvac())
            {
                Debug.Log("[Run] Cannot pay normal evacuation. Refuel with R / the HUD button, or use emergency evacuation and lose run loot.");
                return;
            }

            await CompleteEvacuation(RunEndReason.Evac, state);
        }

        /// <summary>Softlock escape: returns to base but discards all unbanked run loot.</summary>
        public async void EmergencyEvacuate()
        {
            if (!TryGetActiveRun(out var state, out var fuel) || fuel.CanPayEvac) return;
            await CompleteEvacuation(RunEndReason.EmergencyEvac, state);
        }

        bool TryGetActiveRun(out GameStateMachine state, out Player.FuelSystem fuel)
        {
            state = null;
            fuel = null;
            if (_handledRunEnd || !ServiceLocator.TryGet<GameStateMachine>(out state) || state.Current != GameState.Run)
                return false;
            return ServiceLocator.TryGet(out fuel);
        }

        async Task CompleteEvacuation(RunEndReason reason, GameStateMachine state)
        {
            state.SetState(GameState.Evac);
            EventBus.Publish(new RunEnded(reason, state.MaxDepthThisRun));

            var save = ServiceLocator.Get<SaveService>();
            await save.PersistMetaAsync();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Base");
            state.SetState(GameState.Base);
            if (ServiceLocator.TryGet<IPlatformProvider>(out var platform))
                _ = platform.ShowInterstitialAsync();
        }
    }
}
