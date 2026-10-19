using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnderDeep.Core;

namespace UnderDeep.UI
{
    public class DeathScreen : MonoBehaviour
    {
        public GameObject panel;
        public TMP_Text resultText;
        public Button reviveButton;
        public Button toBaseButton;

        void OnEnable(){ EventBus.Subscribe<RunEnded>(OnEnded); EventBus.Subscribe<RewardedResult>(OnRewarded); }
        void OnDisable(){ EventBus.Unsubscribe<RunEnded>(OnEnded); EventBus.Unsubscribe<RewardedResult>(OnRewarded); }

        void OnEnded(RunEnded e)
        {
            if (e.reason==RunEndReason.Death) Show($"Критический урон на {e.depth}м\nЛут потерян (кроме артефактов)");
            else if(e.reason==RunEndReason.Evac) Show($"Эвакуация с {e.depth}м\nЛут сохранён!");
        }
        void Show(string msg){ panel.SetActive(true); resultText.text=msg; Time.timeScale=0; }

        void OnRewarded(RewardedResult r){ if(r.placement=="revive" && r.success) Hide(); }
        void Hide(){ panel.SetActive(false); Time.timeScale=1; }

        public async void OnRevive()
        {
            if(ServiceLocator.TryGet<IPlatformProvider>(out var p)){
                bool ok=await p.ShowRewardedAsync("revive");
                EventBus.Publish(new RewardedResult("revive", ok));
                if(ok) FindFirstObjectByType<Player.HealthSystem>()?.Revive();
            }
        }
        public void OnToBase(){
            Time.timeScale=1;
            var gsm=ServiceLocator.Get<GameStateMachine>();
            gsm.SetState(GameState.Base);
            UnityEngine.SceneManagement.SceneManager.LoadScene("Base");
        }
    }
}
