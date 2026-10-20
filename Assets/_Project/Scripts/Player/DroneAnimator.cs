using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.Player
{
    /// <summary>База под анимации: эвакуация/смерть/урон/бурение. v1 — заглушка на SpriteRenderer+Particle, легко заменить на Animator Controller.</summary>
    public class DroneAnimator : MonoBehaviour
    {
        [Header("Refs")]
        public SpriteRenderer bodyRenderer;
        public Transform eyeDisplay;
        public ParticleSystem drillFx;
        public ParticleSystem hitFx;
        public ParticleSystem evacFx;

        [Header("Colors")]
        public Color idleColor = new Color(0.35f, 0.86f, 0.62f);
        public Color drillColor = new Color(0.95f, 0.85f, 0.35f);
        public Color damageColor = new Color(1f, 0.35f, 0.35f);
        public Color fallColor = new Color(0.6f, 0.75f, 0.95f);

        DroneAnimState _state = DroneAnimState.Idle;
        float _stateTimer;

        void OnEnable()
        {
            EventBus.Subscribe<DrillAutoStarted>(OnDrill);
            EventBus.Subscribe<HealthChanged>(OnHealth);
            EventBus.Subscribe<RunEnded>(OnRunEnded);
            EventBus.Subscribe<PlayerFalling>(OnFalling);
            EventBus.Subscribe<PlayerLanded>(OnLanded);
            EventBus.Subscribe<AbilityTriggered>(OnAbility);
            EventBus.Subscribe<DroneAnimationRequested>(OnAnimReq);
            EventBus.Subscribe<FuelCanisterCollected>(OnFuel);
        }
        void OnDisable()
        {
            EventBus.Unsubscribe<DrillAutoStarted>(OnDrill);
            EventBus.Unsubscribe<HealthChanged>(OnHealth);
            EventBus.Unsubscribe<RunEnded>(OnRunEnded);
            EventBus.Unsubscribe<PlayerFalling>(OnFalling);
            EventBus.Unsubscribe<PlayerLanded>(OnLanded);
            EventBus.Unsubscribe<AbilityTriggered>(OnAbility);
            EventBus.Unsubscribe<DroneAnimationRequested>(OnAnimReq);
            EventBus.Unsubscribe<FuelCanisterCollected>(OnFuel);
        }

        void OnDrill(DrillAutoStarted e) => Play(DroneAnimState.DrillSide, e.dir);

        void OnHealth(HealthChanged e)
        {
            if (e.hp < e.maxHp) Play(DroneAnimState.Damage, Vector2.zero);
        }
        void OnRunEnded(RunEnded e)
        {
            if (e.reason==RunEndReason.Death) Play(DroneAnimState.Death, Vector2.zero);
            else if (e.reason==RunEndReason.Evac || e.reason==RunEndReason.EmergencyEvac) Play(DroneAnimState.Evacuate, Vector2.zero);
        }
        void OnFalling(PlayerFalling e) => Play(DroneAnimState.Fall, Vector2.down);
        void OnLanded(PlayerLanded e) => Play(DroneAnimState.Idle, Vector2.zero);
        void OnAbility(AbilityTriggered e)
        {
            if (e.type==AbilityType.Dash) Play(DroneAnimState.Move, e.dir);
            if (e.type==AbilityType.Shield) Play(DroneAnimState.Idle, e.dir);
        }
        void OnAnimReq(DroneAnimationRequested e) => Play(e.state, e.dir);
        void OnFuel(FuelCanisterCollected e) { /* вспышка */ if (bodyRenderer) bodyRenderer.color = Color.Lerp(bodyRenderer.color, Color.yellow, 0.5f); }

        void Play(DroneAnimState s, Vector2 dir)
        {
            _state = s;
            _stateTimer = 0.25f;
            Apply(s, dir);
        }

        void Apply(DroneAnimState s, Vector2 dir)
        {
            if (bodyRenderer==null) return;
            switch(s)
            {
                case DroneAnimState.Idle: bodyRenderer.color = idleColor; break;
                case DroneAnimState.Move: bodyRenderer.color = idleColor; if (eyeDisplay) eyeDisplay.localScale = Vector3.one*1.05f; break;
                case DroneAnimState.DrillSide:
                case DroneAnimState.DrillDown: bodyRenderer.color = drillColor; if (drillFx) { drillFx.transform.position = transform.position + (Vector3)dir*0.6f; if (!drillFx.isPlaying) drillFx.Play(); } break;
                case DroneAnimState.Fall: bodyRenderer.color = fallColor; break;
                case DroneAnimState.Damage: bodyRenderer.color = damageColor; if (hitFx) hitFx.Play(); break;
                case DroneAnimState.Death: bodyRenderer.color = Color.gray; break;
                case DroneAnimState.Evacuate: bodyRenderer.color = Color.cyan; if (evacFx) evacFx.Play(); break;
            }
            // TODO: заменить на Animator.SetTrigger(state.ToString()) когда появятся клипы
        }

        void Update()
        {
            if (_stateTimer>0)
            {
                _stateTimer -= Time.deltaTime;
                if (_stateTimer<=0) { _state = DroneAnimState.Idle; if (bodyRenderer) bodyRenderer.color = idleColor; }
            }
        }

        // Вызывается извне напрямую, если нет EventBus
        public void Trigger(DroneAnimState state, Vector2 dir) => EventBus.Publish(new DroneAnimationRequested(state, dir));
    }
}
