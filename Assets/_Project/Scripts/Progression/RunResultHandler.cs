using System.Threading.Tasks;
using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.Progression
{
    public class RunResultHandler : MonoBehaviour
    {
        [Min(1f)] public float fuelDecisionSeconds = 10f;

        private bool _handledRunEnd;
        private bool _fuelCountdownActive;
        private bool _evacuationPaymentInProgress;
        private float _fuelDeadline;
        private int _lastCountdownSecond = -1;

        public bool FuelCountdownActive => _fuelCountdownActive;

        void Awake() => ServiceLocator.Register(this);

        void OnEnable()
        {
            EventBus.Subscribe<RunEnded>(OnRunEnded);
            EventBus.Subscribe<EvacRequested>(OnEvacRequested);
            EventBus.Subscribe<FuelChanged>(OnFuelChanged);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<RunEnded>(OnRunEnded);
            EventBus.Unsubscribe<EvacRequested>(OnEvacRequested);
            EventBus.Unsubscribe<FuelChanged>(OnFuelChanged);
        }

        void Update()
        {
            if (!_fuelCountdownActive || _handledRunEnd) return;

            float remaining = Mathf.Max(0, _fuelDeadline - Time.unscaledTime);
            int seconds = Mathf.CeilToInt(remaining);
            if (seconds != _lastCountdownSecond)
            {
                _lastCountdownSecond = seconds;
                EventBus.Publish(new FuelRescueCountdownChanged(seconds, true));
            }

            if (remaining > 0) return;
            _fuelCountdownActive = false;
            EventBus.Publish(new FuelRescueCountdownChanged(0, false));

            if (ServiceLocator.TryGet<Player.HealthSystem>(out var health))
            {
                health.ForceDeath();
                return;
            }

            if (ServiceLocator.TryGet<GameStateMachine>(out var state))
            {
                state.SetState(GameState.Death);
                EventBus.Publish(new RunEnded(RunEndReason.Death, state.MaxDepthThisRun));
            }
        }

        void OnFuelChanged(FuelChanged evt)
        {
            if (_fuelCountdownActive || _evacuationPaymentInProgress || _handledRunEnd ||
                !ServiceLocator.TryGet<Player.FuelSystem>(out var fuel) || evt.current >= fuel.evacCost) return;
            if (!ServiceLocator.TryGet<GameStateMachine>(out var state) || state.Current != GameState.Run) return;

            _fuelCountdownActive = true;
            float duration = Mathf.Max(1f, fuelDecisionSeconds);
            _fuelDeadline = Time.unscaledTime + duration;
            _lastCountdownSecond = Mathf.CeilToInt(duration);
            EventBus.Publish(new FuelRescueCountdownChanged(_lastCountdownSecond, true));
            Debug.Log($"[Run] Fuel dropped below the {fuel.evacCost:0}-unit evacuation reserve. Rescue costs {fuel.depletedRescueCrystalCost} meta crystals within {duration:0} seconds or death.");
        }

        // Player sends an event rather than depending directly on this progression system.
        void OnEvacRequested(EvacRequested e) => Evacuate();

        async void OnRunEnded(RunEnded e)
        {
            if (_handledRunEnd) return;
            _handledRunEnd = true;
            _fuelCountdownActive = false;

            var save = ServiceLocator.Get<SaveService>();
            bool keepLoot = e.reason == RunEndReason.Evac || e.reason == RunEndReason.EmergencyEvac;
            if (ServiceLocator.TryGet<Player.PlayerInventory>(out var inventory))
                inventory.ApplyToMeta(keepLoot);

            save.Meta.totalRuns++;
            await save.SubmitDepthAsync(e.depth);
            await save.PersistMetaAsync();
            Debug.Log($"[Run] Ended {e.reason} depth={e.depth} keep={keepLoot}");
        }

        /// <summary>Evacuate for fuel, or pay the high-cost rescue during the depleted-fuel countdown.</summary>
        public async void Evacuate()
        {
            if (!TryGetActiveRun(out var state, out var fuel)) return;
            bool depletedRescue = fuel.Current < fuel.evacCost;
            if (depletedRescue && !_fuelCountdownActive) return;
            bool paid;
            _evacuationPaymentInProgress = true;
            try { paid = fuel.TryPayEvac(); }
            finally { _evacuationPaymentInProgress = false; }
            if (!paid)
            {
                Debug.Log(depletedRescue
                    ? $"[Run] Rescue requires {fuel.depletedRescueCrystalCost} persistent meta crystals before the fuel timer expires."
                    : $"[Run] Regular evacuation requires {fuel.evacCost:0} fuel. Rescue is available below that reserve for {fuelDecisionSeconds:0} seconds at {fuel.depletedRescueCrystalCost} meta crystals.");
                return;
            }

            await CompleteEvacuation(depletedRescue ? RunEndReason.EmergencyEvac : RunEndReason.Evac, state);
        }

        /// <summary>Explicit button for the 20-meta-crystal rescue during the fuel-depletion countdown.</summary>
        public async void EmergencyEvacuate()
        {
            if (!TryGetActiveRun(out var state, out var fuel) || fuel.Current >= fuel.evacCost || !_fuelCountdownActive)
                return;
            if (!fuel.TryPayEvac()) return;
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
            _fuelCountdownActive = false;
            EventBus.Publish(new FuelRescueCountdownChanged(0, false));
            state.SetState(GameState.Evac);
            EventBus.Publish(new RunEnded(reason, state.MaxDepthThisRun));

            var save = ServiceLocator.Get<SaveService>();
            await save.PersistMetaAsync();
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Base");
            state.SetState(GameState.Base);
            if (ServiceLocator.TryGet<IPlatformProvider>(out var platform))
                _ = platform.ShowInterstitialAsync();
        }
    }
}
