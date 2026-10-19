using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnderDeep.Core;

namespace UnderDeep.Player
{
    /// <summary>Фонарик дрона. URP Light2D + пульсация, влияет на безумие.</summary>
    public class PlayerLight : MonoBehaviour
    {
        public Light2D pointLight;
        public float baseIntensity = 1f;
        public float baseRadius = 5f;
        public float flickerSpeed = 0.1f;
        public float flickerAmp = 0.08f;

        private float _t;
        private bool _hasRadar;

        void Awake()
        {
            if (!pointLight) pointLight = GetComponentInChildren<Light2D>();
            if (!pointLight)
            {
                // создаём
                var go = new GameObject("PlayerLight");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                pointLight = go.AddComponent<Light2D>();
                pointLight.lightType = Light2D.LightType.Point;
                pointLight.pointLightOuterRadius = baseRadius;
                pointLight.intensity = baseIntensity;
                pointLight.color = new Color(1f, 0.95f, 0.8f);
                pointLight.falloffIntensity = 0.6f;
            }
        }

        void Start()
        {
            if (ServiceLocator.TryGet<SaveService>(out var s))
                _hasRadar = s.Meta.purchasedUpgrades.Contains("radar");
            if (_hasRadar) pointLight.pointLightOuterRadius = baseRadius * 1.4f;
            EventBus.Subscribe<SanityChanged>(OnSanity);
        }
        void OnDestroy() => EventBus.Unsubscribe<SanityChanged>(OnSanity);

        void OnSanity(SanityChanged e)
        {
            // при высоком безумии свет меркнет
            if (e.stage >= SanityStage.FakeEnemies)
                pointLight.intensity = baseIntensity * 0.5f;
            else
                pointLight.intensity = baseIntensity;
        }

        void Update()
        {
            // лёгкий фликер + дыхание
            _t += Time.deltaTime * 5f;
            float flick = Mathf.PerlinNoise(_t, 0) * flickerAmp;
            pointLight.intensity = baseIntensity + flick - (ServiceLocator.TryGet<SanitySystem>(out var s) ? s.sanity / 400f : 0);

            // цвет меняется от глубины
            if (ServiceLocator.TryGet<DroneController>(out var d))
            {
                float depthF = d.Depth / 100f;
                // глубже — более холодный/синий
                pointLight.color = Color.Lerp(new Color(1f,0.95f,0.8f), new Color(0.7f,0.8f,1f), depthF * 0.4f);
            }
        }

        public float CurrentRadius => pointLight ? pointLight.pointLightOuterRadius : baseRadius;
        public bool IsBright => pointLight.intensity > 0.8f;
    }
}
