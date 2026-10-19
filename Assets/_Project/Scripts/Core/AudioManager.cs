using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.Core
{
    public class AudioManager : MonoBehaviour
    {
        [Header("Clips")]
        public AudioClip drillLoop;
        public AudioClip drillEnd;
        public AudioClip pickup;
        public AudioClip damage;
        public AudioClip evac;
        public AudioClip whisper;

        private AudioSource _sfx, _music, _loop;

        void Awake()
        {
            ServiceLocator.Register(this);
            _sfx = gameObject.AddComponent<AudioSource>();
            _music = gameObject.AddComponent<AudioSource>();
            _loop = gameObject.AddComponent<AudioSource>();
            _loop.loop = true;
            DontDestroyOnLoad(gameObject);
        }

        void OnEnable()
        {
            EventBus.Subscribe<BlockMined>(OnMined);
            EventBus.Subscribe<DrillStarted>(OnDrillStart);
            EventBus.Subscribe<DrillEnded>(OnDrillEnd);
            EventBus.Subscribe<HealthChanged>(OnHealth);
        }
        void OnDisable()
        {
            EventBus.Unsubscribe<BlockMined>(OnMined);
            EventBus.Unsubscribe<DrillStarted>(OnDrillStart);
            EventBus.Unsubscribe<DrillEnded>(OnDrillEnd);
            EventBus.Unsubscribe<HealthChanged>(OnHealth);
        }

        void OnMined(BlockMined e) => Play(pickup, 0.8f);
        void OnDrillStart(DrillStarted e)
        {
            if (drillLoop && !_loop.isPlaying) { _loop.clip = drillLoop; _loop.volume = 0.5f; _loop.Play(); }
        }
        void OnDrillEnd(DrillEnded e)
        {
            _loop.Stop();
            Play(drillEnd, 0.6f);
        }
        void OnHealth(HealthChanged e) { if (e.hp < e.maxHp) Play(damage, 1f); }

        void Play(AudioClip c, float vol){ if(c) _sfx.PlayOneShot(c, vol); }

        public void SetMusic(AudioClip clip, float vol=0.4f){ _music.clip=clip; _music.loop=true; _music.volume=vol; _music.Play(); }
    }
}
