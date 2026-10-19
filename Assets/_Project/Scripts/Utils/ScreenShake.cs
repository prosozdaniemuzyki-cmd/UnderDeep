using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.Utils
{
    public class ScreenShake : MonoBehaviour
    {
        Vector3 _orig;
        void Awake() => _orig = transform.localPosition;
        void OnEnable()
        {
            EventBus.Subscribe<HealthChanged>(OnHealth);
            EventBus.Subscribe<World.TrapSystem.TrapTriggered>(OnTrap);
        }
        void OnDisable()
        {
            EventBus.Unsubscribe<HealthChanged>(OnHealth);
            EventBus.Unsubscribe<World.TrapSystem.TrapTriggered>(OnTrap);
        }
        void OnHealth(HealthChanged e){ if(e.hp < e.maxHp) Shake(0.15f, 0.25f); }
        void OnTrap(World.TrapSystem.TrapTriggered e){ Shake(0.1f, 0.2f); }

        public void Shake(float amp, float dur)
        {
            StopAllCoroutines();
            StartCoroutine(Co(amp,dur));
        }
        System.Collections.IEnumerator Co(float amp,float dur)
        {
            float t=0;
            while(t<dur)
            {
                t+= Time.deltaTime;
                transform.localPosition = _orig + (Vector3)Random.insideUnitCircle * amp;
                yield return null;
            }
            transform.localPosition = _orig;
        }
    }
}
