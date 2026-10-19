using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.Progression
{
    public class RunResultHandler : MonoBehaviour
    {
        void OnEnable()
        {
            EventBus.Subscribe<RunEnded>(OnRunEnded);
        }
        void OnDisable(){ EventBus.Unsubscribe<RunEnded>(OnRunEnded); }

        async void OnRunEnded(RunEnded e)
        {
            var save = ServiceLocator.Get<SaveService>();
            bool isEvac = e.reason == RunEndReason.Evac;
            var inv = ServiceLocator.Get<Player.PlayerInventory>();
            inv.ApplyToMeta(isEvac);

            if (isEvac)
            {
                await save.PersistMetaAsync();
                await save.SubmitDepthAsync(e.depth);
            }
            save.Meta.totalRuns++;
            Debug.Log($"[Run] Ended {e.reason} depth={e.depth} keep={isEvac}");
        }

        // Called by UI button
        public async void Evacuate()
        {
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
