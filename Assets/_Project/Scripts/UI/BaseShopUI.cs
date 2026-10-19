using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnderDeep.Core;
using UnderDeep.Progression;

namespace UnderDeep.UI
{
    public class BaseShopUI : MonoBehaviour
    {
        public Transform listRoot;
        public GameObject rowPrefab;
        public TMP_Text resourcesText;
        public Button startRunButton;

        private UpgradeSystem _upgrades;
        private SaveService _save;

        void Start()
        {
            _upgrades = ServiceLocator.Get<UpgradeSystem>();
            _save = ServiceLocator.Get<SaveService>();
            Rebuild();
            startRunButton.onClick.AddListener(OnStartRun);
        }

        void Rebuild()
        {
            foreach(Transform c in listRoot) Destroy(c.gameObject);
            resourcesText.text = $"Металл: {_save.Meta.rustMetal}  Кристалл: {_save.Meta.crystal}  Био: {_save.Meta.bioGel}  Глубина: {_save.Meta.maxDepthReached}м";

            foreach(var def in _upgrades.All)
            {
                var go = Instantiate(rowPrefab, listRoot);
                var txt = go.GetComponentInChildren<TMP_Text>();
                var btn = go.GetComponentInChildren<Button>();
                bool owned = _save.Meta.purchasedUpgrades.Contains(def.id);
                bool can = _upgrades.CanPurchase(def);
                txt.text = $"{def.displayName} — {CostStr(def)} {(owned ? "[КУПЛЕНО]" : "")}\n<size=80%>{def.description}</size>";
                btn.interactable = can;
                btn.onClick.AddListener(()=>{ if(_upgrades.TryPurchase(def)) Rebuild(); });
            }
        }

        string CostStr(UpgradeDef d)
        {
            var s=""; foreach(var c in d.costs) s+=$"{c.type}:{c.amount} ";
            return s;
        }

        void OnStartRun()
        {
            var gsm = ServiceLocator.Get<GameStateMachine>();
            gsm.SetState(GameState.Run);
            _save.NewRun(Random.Range(0, int.MaxValue));
            UnityEngine.SceneManagement.SceneManager.LoadScene("Run");
        }
    }
}
