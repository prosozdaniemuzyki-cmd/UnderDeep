using UnityEngine;
using UnderDeep.Core;
using UnderDeep.World;
using System.Collections.Generic;

namespace UnderDeep.Utils
{
    /// <summary>Подсветка редких руд сквозь стены если куплен радар.</summary>
    public class RadarVision : MonoBehaviour
    {
        public Color crystalGlow = new Color(0.4f,0.8f,1f,0.6f);
        public Color bioGlow = new Color(0.4f,1f,0.6f,0.6f);
        private List<GameObject> _markers = new();

        void Start() => InvokeRepeating(nameof(UpdateRadar), 1f, 1.5f);

        void UpdateRadar()
        {
            if (!ServiceLocator.TryGet<SaveService>(out var save)) return;
            if (!save.Meta.purchasedUpgrades.Contains("radar")) return;
            if (!ServiceLocator.TryGet<WorldGrid>(out var world)) return;
            if (!ServiceLocator.TryGet<Player.DroneController>(out var drone)) return;

            Clear();
            var ores = world.FindResources(ResourceType.Crystal, 12);
            foreach(var p in ores)
            {
                if (Vector2Int.Distance(p, drone.GridPos) > 8) continue;
                var go = new GameObject("RadarMarker");
                go.transform.position = world.GridToWorld(p);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = null;
                sr.color = crystalGlow;
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = Vector2.one * 0.7f;
                // пульс
                go.AddComponent<RadarPulse>();
                _markers.Add(go);
            }
        }
        void Clear(){ foreach(var m in _markers) if(m) Destroy(m); _markers.Clear(); }
        void OnDisable()=> Clear();
    }
    public class RadarPulse : MonoBehaviour
    {
        void Update()
        {
            float s = 1f + Mathf.Sin(Time.time*3f)*0.2f;
            transform.localScale = Vector3.one * s;
        }
    }
}
