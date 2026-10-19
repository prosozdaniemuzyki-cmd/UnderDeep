using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnderDeep.Core;

namespace UnderDeep.UI
{
    public class HUD : MonoBehaviour
    {
        [Header("Bars")]
        public Slider fuelBar;
        public Slider heatBar;
        public Slider sanityBar;
        public TMP_Text depthText;
        public TMP_Text lootText;
        public TMP_Text hpText;

        [Header("Sanity FX")]
        public Image vignette;
        public TMP_Text sanityWarning;

        void OnEnable()
        {
            EventBus.Subscribe<FuelChanged>(OnFuel);
            EventBus.Subscribe<HeatChanged>(OnHeat);
            EventBus.Subscribe<HealthChanged>(OnHealth);
            EventBus.Subscribe<SanityChanged>(OnSanity);
            EventBus.Subscribe<ResourceCollected>(OnLoot);
            EventBus.Subscribe<DepthChanged>(OnDepth);
        }
        void OnDisable()
        {
            EventBus.Unsubscribe<FuelChanged>(OnFuel);
            EventBus.Unsubscribe<HeatChanged>(OnHeat);
            EventBus.Unsubscribe<HealthChanged>(OnHealth);
            EventBus.Unsubscribe<SanityChanged>(OnSanity);
            EventBus.Unsubscribe<ResourceCollected>(OnLoot);
            EventBus.Unsubscribe<DepthChanged>(OnDepth);
        }

        void OnFuel(FuelChanged e){ if(fuelBar) {fuelBar.maxValue=e.max; fuelBar.value=e.current;} }
        void OnHeat(HeatChanged e){ if(heatBar) heatBar.value=e.heat; if(e.overheated) Shake(); }
        void OnHealth(HealthChanged e){ if(hpText) hpText.text=$"HP: {e.hp}/{e.maxHp}"; }
        void OnSanity(SanityChanged e){
            if(sanityBar) sanityBar.value=e.sanity;
            if(vignette) vignette.color=new Color(0,0,0, e.sanity/200f);
            if(sanityWarning) sanityWarning.gameObject.SetActive(e.stage>=SanityStage.FakeEnemies);
        }
        void OnLoot(ResourceCollected e){ if(lootText) lootText.text=$"{e.type}+{e.amount} ({e.totalRun})"; }
        void OnDepth(DepthChanged e){ if(depthText) depthText.text=$"{e.depth}м (max {e.maxDepthThisRun})"; }

        void Shake(){ /* simple cam shake */ if(Camera.main) Camera.main.transform.position+=Random.insideUnitSphere*0.1f; }

        // Buttons
        public void OnEvacClick(){ FindFirstObjectByType<Progression.RunResultHandler>()?.Evacuate(); }
        public async void OnReviveClick()
        {
            if (!ServiceLocator.TryGet<IPlatformProvider>(out var p)) return;
            bool ok = await p.ShowRewardedAsync("revive");
            if(ok) FindFirstObjectByType<Player.HealthSystem>()?.Revive();
        }
        public async void OnDoubleLootClick()
        {
            if (!ServiceLocator.TryGet<IPlatformProvider>(out var p)) return;
            bool ok = await p.ShowRewardedAsync("double_loot");
            if(ok){
                if(ServiceLocator.TryGet<Player.PlayerInventory>(out var inv))
                    foreach(var kv in inv.RunLoot) inv.Add(kv.Key, kv.Value); // double
            }
        }
    }
}
