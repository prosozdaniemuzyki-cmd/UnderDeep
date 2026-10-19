using UnityEngine;
using UnderDeep.Core;
using System.Collections.Generic;

namespace UnderDeep.Progression
{
    /// <summary>5 частей памяти, разбросаны по биомам. Открывают историю + способность.</summary>
    public class ArtifactManager : MonoBehaviour
    {
        public List<ArtifactDef> allArtifacts; // 5 шт
        private SaveService _save;

        void Awake() => ServiceLocator.Register(this);
        void Start() => _save = ServiceLocator.Get<SaveService>();

        void OnEnable() => EventBus.Subscribe<BlockMined>(OnMined);
        void OnDisable() => EventBus.Unsubscribe<BlockMined>(OnMined);

        void OnMined(BlockMined e)
        {
            if (e.type != BlockType.ArtifactChunk) return;
            // определяем какой артефакт в этом биоме
            int idx = Mathf.Clamp(e.pos.y / 20, 0, 4); // 0..4
            string id = $"artifact_{idx+1}";
            if (_save.Meta.artifacts.Contains(id)) return;
            _save.Meta.artifacts.Add(id);
            _ = _save.PersistMetaAsync();
            EventBus.Publish(new ArtifactFound(id, _save.Meta.artifacts.Count));
            Debug.Log($"[Artifact] Found {id} ({_save.Meta.artifacts.Count}/5) — {_save.Meta.artifacts.Count==5? \"Финал открыт!\" : \"\"}");
            // награда
            ApplyPerk(id);
        }

        void ApplyPerk(string id)
        {
            // пример перков
            var drone = ServiceLocator.TryGet<Player.DroneController>(out var d) ? d : null;
            switch(id)
            {
                case "artifact_1": // память инженера — +10 топлива
                    if (ServiceLocator.TryGet<Player.FuelSystem>(out var f)) f.SetMaxFromUpgrade(10);
                    break;
                case "artifact_2": // радар v2
                    break;
                case "artifact_3": // устойчивость к яду
                    break;
                case "artifact_4":
                case "artifact_5": break;
            }
        }

        public bool HasAll => _save != null && _save.Meta.artifacts.Count >= 5;
    }

    [CreateAssetMenu(menuName="UnderDeep/ArtifactDef")]
    public class ArtifactDef : ScriptableObject
    {
        public string id;
        public string title;
        [TextArea] public string lore;
        public Sprite icon;
        public BiomeId biome;
    }
}
