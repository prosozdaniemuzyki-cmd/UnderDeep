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
        void OnDisable(){ EventBus.Unsubscribe<RunEnded>(OnRunEnded); EventBus.Unsubscribe<EvacRequested>(OnEvacRequested); }

        // хоткей B из Player/DrillSystem приходит событием — прямой ссылки Player → Progression нет
        void OnEvacRequested(EvacRequested e) => Evacuate();

        async void OnRunEnded(RunEnded e)
        {
            if (_handledRunEnd) return;
            _handledRunEnd = true;

            var save = ServiceLocator.Get<SaveService>();
            bool isEvac = e.reason == RunEndReason.Evac;
            if (ServiceLocator.TryGet<Player.PlayerInventory>(out var inventory))
                inventory.ApplyToMeta(isEvac);

            save.Meta.totalRuns++;
            await save.SubmitDepthAsync(e.depth);
            await save.PersistMetaAsync();
            Debug.Log($"[Run] Ended {e.reason} depth={e.depth} keep={isEvac}");
        }

        // Called by UI button
        public async void Evacuate()
        {
            if (_handledRunEnd || !ServiceLocator.TryGet<GameStateMachine>(out var currentState) || currentState.Current != GameState.Run)
                return;
            var fuel = ServiceLocator.Get<Player.FuelSystem>();
            if (!fuel.TryPayEvac())
            {
                Debug.Log("[Run] Not enough fuel for evac!");
                return;
            }
            var gsm = ServiceLocator.Get<GameStateMachine>();
            gsm.SetState(GameState.Evac);
            EventBus.Publish(new RunEnded(RunEndReason.Evac, gsm.MaxDepthThisRun));
            var save = ServiceLocator.Get<SaveService>();
            await save.PersistMetaAsync();
            // rewarded double loot
            UnityEngine.SceneManagement.SceneManager.LoadScene("Base");
            gsm.SetState(GameState.Base);
            if (ServiceLocator.TryGet<IPlatformProvider>(out var p)) _ = p.ShowInterstitialAsync();
        }
    }
}
