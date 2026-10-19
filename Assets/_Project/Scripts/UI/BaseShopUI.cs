using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnderDeep.Core;
using UnderDeep.Progression;

namespace UnderDeep.UI
{
    /// <summary>Base shop UI. Supports a lightweight runtime-built layout for the vertical slice.</summary>
    public class BaseShopUI : MonoBehaviour
    {
        public Transform listRoot;
        public GameObject rowPrefab;
        public TMP_Text resourcesText;
        public Text resourcesLegacyText;
        public Button startRunButton;

        private UpgradeSystem _upgrades;
        private SaveService _save;

        void Start()
        {
            if (!ServiceLocator.TryGet(out _save))
            {
                Debug.LogError("[BaseShopUI] SaveService is not registered. Start the game from Boot.");
                enabled = false;
                return;
            }

            if (!ServiceLocator.TryGet(out _upgrades))
            {
                Debug.LogError("[BaseShopUI] UpgradeSystem is not registered.");
                enabled = false;
                return;
            }

            if (startRunButton != null)
                startRunButton.onClick.AddListener(OnStartRun);
            Rebuild();
        }

        public void Rebuild()
        {
            if (_save == null || _upgrades == null) return;
            var resourceSummary = $"РЕСУРСЫ   МЕТАЛЛ {_save.Meta.rustMetal}   КРИСТАЛЛ {_save.Meta.crystal}   БИОГЕЛЬ {_save.Meta.bioGel}   МАКС. ГЛУБИНА {_save.Meta.maxDepthReached} м";
            if (resourcesText != null) resourcesText.text = resourceSummary;
            if (resourcesLegacyText != null) resourcesLegacyText.text = resourceSummary;

            if (listRoot == null) return;
            for (int i = listRoot.childCount - 1; i >= 0; i--)
                Destroy(listRoot.GetChild(i).gameObject);

            foreach (var def in _upgrades.All)
            {
                if (def == null) continue;
                var row = rowPrefab != null ? Instantiate(rowPrefab, listRoot) : CreateRuntimeRow(listRoot);
                var label = row.GetComponentInChildren<TMP_Text>(true);
                var legacyLabel = row.GetComponentInChildren<Text>(true);
                var button = row.GetComponentInChildren<Button>(true);

                bool owned = _save.Meta.purchasedUpgrades.Contains(def.id);
                bool canPurchase = !owned && _upgrades.CanPurchase(def);
                string status = owned ? "  [КУПЛЕНО]" : canPurchase ? "  [КУПИТЬ]" : "  [НЕДОСТУПНО]";
                string text = $"{def.displayName}{status}\n{CostStr(def)}";
                if (label != null) label.text = text;
                else if (legacyLabel != null) legacyLabel.text = text;

                if (button != null)
                {
                    button.interactable = canPurchase;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        if (_upgrades.TryPurchase(def)) Rebuild();
                    });
                }
            }
        }

        GameObject CreateRuntimeRow(Transform parent)
        {
            var row = new GameObject("UpgradeRow", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var rect = (RectTransform)row.transform;
            rect.sizeDelta = new Vector2(0, 54);
            row.GetComponent<Image>().color = new Color(0.11f, 0.19f, 0.24f, 0.96f);
            var layout = row.GetComponent<LayoutElement>();
            layout.minHeight = 54;
            layout.preferredHeight = 54;

            var textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(row.transform, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12, 4);
            textRect.offsetMax = new Vector2(-12, -4);
            var label = textObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 15;
            label.color = new Color(0.8f, 0.94f, 0.89f);
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            row.GetComponent<Button>().targetGraphic = row.GetComponent<Image>();
            return row;
        }

        string CostStr(UpgradeDef def)
        {
            var result = "";
            if (def.costs == null) return result;
            foreach (var cost in def.costs)
                result += $"{cost.type}: {cost.amount}   ";
            return result;
        }

        void OnStartRun()
        {
            if (_save == null) return;
            if (ServiceLocator.TryGet<GameStateMachine>(out var gsm)) gsm.SetState(GameState.Run);
            _save.NewRun(Random.Range(1, 1_000_000));
            UnityEngine.SceneManagement.SceneManager.LoadScene("Run");
        }
    }
}
