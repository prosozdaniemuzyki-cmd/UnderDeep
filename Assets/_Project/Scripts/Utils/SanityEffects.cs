using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnderDeep.Core;
using UnderDeep.World;
using UnderDeep.Player;
using System.Collections.Generic;

namespace UnderDeep.Utils
{
    /// <summary>Визуальные и геймплейные эффекты безумия: искажения, ложные ресурсы/враги, потеря управления.</summary>
    // Живёт в Utils (Assembly-CSharp): спавнит Enemies.Hallucination и рисует UI (Image/TMP), а Player → Enemies запрещено контрактом (asmdef-аудит 1.2.1)
    public class SanityEffects : MonoBehaviour
    {
        [Header("FX")]
        public Image vignette;
        public Image noiseOverlay;
        public TMP_Text whispersText;
        public float whisperInterval = 12f;

        private float _whisperTimer;
        private readonly string[] _whispers = new[]
        {
            "ты слышишь нас...", "копай глубже", "не оборачивайся",
            "LONE-1, ты не один", "ядро зовёт", "они оставили тебя"
        };

        // fake resources
        private List<GameObject> _fakeOres = new();
        public GameObject fakeOrePrefab; // простой спрайт кристалла

        void OnEnable()
        {
            EventBus.Subscribe<SanityChanged>(OnSanity);
            EventBus.Subscribe<BlockMined>(OnMined);
        }
        void OnDisable()
        {
            EventBus.Unsubscribe<SanityChanged>(OnSanity);
            EventBus.Unsubscribe<BlockMined>(OnMined);
        }

        void OnSanity(SanityChanged e)
        {
            UpdateFX(e.sanity, e.stage);
            if (e.stage >= SanityStage.FakeResources && _fakeOres.Count == 0) SpawnFakeOres();
            if (e.stage >= SanityStage.FakeEnemies) TrySpawnFakeEnemy();
            if (e.stage == SanityStage.Blackout) TriggerBlackout();
        }

        void UpdateFX(float sanity, SanityStage stage)
        {
            if (vignette)
                vignette.color = new Color(0,0,0, sanity / 250f);
            if (noiseOverlay)
            {
                noiseOverlay.color = new Color(1,1,1, stage >= SanityStage.LightDistortion ? (sanity-25)/300f : 0);
                // лёгкое искажение через scale
                float s = 1f + (sanity/100f)*0.02f * Mathf.Sin(Time.time*3f);
                noiseOverlay.rectTransform.localScale = Vector3.one * s;
            }
            // хроматика через камеру (простой offset)
            if (stage >= SanityStage.LightDistortion && Camera.main)
                Camera.main.transform.localPosition += (Vector3)Random.insideUnitCircle * (sanity/10000f);
        }

        void SpawnFakeOres()
        {
            if (!fakeOrePrefab || !ServiceLocator.TryGet<WorldGrid>(out var world)) return;
            var player = ServiceLocator.Get<DroneController>();
            for(int i=0;i<6;i++)
            {
                var gp = player.GridPos + new Vector2Int(Random.Range(-5,6), Random.Range(2,8));
                if (!world.InBounds(gp) || !world.IsAir(gp)) continue;
                var go = Instantiate(fakeOrePrefab, world.GridToWorld(gp), Quaternion.identity);
                go.name = "FakeOre";
                _fakeOres.Add(go);
                // при бурении — пшик
            }
        }

        void OnMined(BlockMined e)
        {
            // если выкопал фейк — исчезает без лута
            for(int i=_fakeOres.Count-1;i>=0;i--)
            {
                if (_fakeOres[i]==null) {_fakeOres.RemoveAt(i); continue;}
                var w = ServiceLocator.Get<WorldGrid>();
                var gp = w.WorldToGrid(_fakeOres[i].transform.position);
                if (gp==e.pos)
                {
                    Destroy(_fakeOres[i]);
                    _fakeOres.RemoveAt(i);
                    Debug.Log("[Sanity] Fake ore vanished!");
                }
            }
        }

        void TrySpawnFakeEnemy()
        {
            if (Random.value > 0.02f) return;
            // спавн галлюцинации
            if (ServiceLocator.TryGet<Enemies.Hallucination>(out _)) return; // уже есть?
            var drone = ServiceLocator.Get<DroneController>();
            var pos = drone.transform.position + (Vector3)Random.insideUnitCircle * 4f;
            var go = new GameObject("FakeEnemy");
            go.transform.position = pos;
            go.AddComponent<Enemies.Hallucination>();
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = null; sr.color = new Color(1,0.6f,0.8f,0.5f);
            go.AddComponent<BoxCollider2D>().isTrigger = true;
        }

        void TriggerBlackout()
        {
            Debug.Log("[Sanity] BLACKOUT 5s — потеря управления");
            var drone = ServiceLocator.Get<DroneController>();
            if (drone) drone.enabled = false;
            Invoke(nameof(EnableDrone), 5f);
        }
        void EnableDrone()
        {
            if (ServiceLocator.TryGet<DroneController>(out var d)) d.enabled = true;
        }

        void Update()
        {
            _whisperTimer += Time.deltaTime;
            if (_whisperTimer > whisperInterval && ServiceLocator.TryGet<SanitySystem>(out var s) && s.sanity > 50)
            {
                _whisperTimer = 0;
                if (whispersText)
                {
                    whispersText.text = _whispers[Random.Range(0,_whispers.Length)];
                    whispersText.gameObject.SetActive(true);
                    Invoke(nameof(HideWhisper), 2.5f);
                }
                // аудио шёпот (stub)
            }
        }
        void HideWhisper(){ if(whispersText) whispersText.gameObject.SetActive(false); }
    }
}
