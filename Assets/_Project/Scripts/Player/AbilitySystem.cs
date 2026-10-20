using UnityEngine;
using UnderDeep.Core;
using UnderDeep.World;

namespace UnderDeep.Player
{
    /// <summary>Рывок (Dash) и щит — стоимость топливо/тепло, выбор для уклонения от врагов.</summary>
    public class AbilitySystem : MonoBehaviour
    {
        [Header("Dash")]
        public float dashDistance = 2f; // тайла
        public float dashFuelCost = 8f;
        public float dashHeatCost = 25f;
        public float dashCooldown = 1.5f;
        public float iFrameDuration = 0.3f;

        [Header("Shield")]
        public float shieldFuelPerSec = 6f;
        public float shieldHeatPerSec = 15f;
        public float shieldMaxDuration = 3f;

        FuelSystem _fuel;
        OverheatSystem _heat;
        HealthSystem _health;
        DroneController _controller;
        InputReader _input;
        WorldGrid _world;

        float _dashCooldownTimer;
        float _iFrameTimer;
        float _shieldTimer;
        bool _shieldActive;

        void Start()
        {
            _fuel = GetComponent<FuelSystem>();
            _heat = GetComponent<OverheatSystem>();
            _health = GetComponent<HealthSystem>();
            _controller = GetComponent<DroneController>();
            _input = GetComponent<InputReader>();
            if (_input==null) _input = gameObject.AddComponent<InputReader>();
            _world = FindFirstObjectByType<WorldGrid>();
        }

        void Update()
        {
            if (ServiceLocator.TryGet<GameStateMachine>(out var gsm) && gsm.Current != GameState.Run) return;

            _dashCooldownTimer -= Time.deltaTime;
            if (_iFrameTimer>0) { _iFrameTimer -= Time.deltaTime; if (_iFrameTimer<=0) _controller.SetInvulnerable(false); }

            // Dash: нажат B/Shift
            if (_input != null && _input.DashPressed && _dashCooldownTimer<=0)
                TryDash();

            // Shield: удержание Q/LT
            bool wantShield = _input != null && _input.ShieldHeld;
            if (wantShield && !_shieldActive && _fuel.Current > 5 && !_heat.IsStunned) StartShield();
            if (!wantShield && _shieldActive) StopShield();
            if (_shieldActive) TickShield();
        }

        void TryDash()
        {
            if (_fuel.IsEmpty || _heat.IsStunned) return;
            Vector2 dir = _input.LastNonZeroMove;
            if (dir.sqrMagnitude<0.1f) dir = Vector2.right;

            // проверка топлива/тепла
            if (_fuel.Current < dashFuelCost) return;

            _fuel.Consume(dashFuelCost);
            _heat.AddHeat(dashHeatCost);

            // рывок: 2 тайла в направлении, ломаем слабые блоки (Dirt/Rust) на пути
            Vector2Int q = _input.QuantizeDirection(dir);
            Vector2Int start = _controller.GridPos;
            Vector2Int target = start + q * Mathf.RoundToInt(dashDistance);
            // если q диагональ — нормализуем до 1 шага (рывок по диагонали 1 тайл)
            if (Mathf.Abs(q.x)==1 && Mathf.Abs(q.y)==1) target = start + q;

            // пробуем пройти, ломая Dirt/Rust на пути
            for(int i=1;i<=2;i++)
            {
                var step = start + q * i;
                if (_world.CanPass(step)) continue;
                var block = _world.Get(step);
                if (block.type==BlockType.Dirt || block.type==BlockType.RustMetal)
                    _world.DrillTick(step, 10f, 10f); // мгновенно
                else if (block.type!=BlockType.Air) { target = start + q * (i-1); break; }
            }

            if (_world.CanPass(target))
                _controller.TeleportToGrid(target);

            _dashCooldownTimer = dashCooldown;
            _iFrameTimer = iFrameDuration;
            _controller.SetInvulnerable(true);

            EventBus.Publish(new AbilityTriggered(AbilityType.Dash, dir));
            EventBus.Publish(new DroneAnimationRequested(DroneAnimState.Move, dir));
            Debug.Log($"[Ability] Dash to {target}");
        }

        void StartShield()
        {
            _shieldActive = true;
            _shieldTimer = shieldMaxDuration;
            EventBus.Publish(new AbilityTriggered(AbilityType.Shield, Vector2.zero));
            Debug.Log("[Ability] Shield ON");
        }
        void StopShield()
        {
            _shieldActive = false;
            Debug.Log("[Ability] Shield OFF");
        }
        void TickShield()
        {
            _shieldTimer -= Time.deltaTime;
            bool ok = _fuel.Consume(shieldFuelPerSec * Time.deltaTime);
            _heat.AddHeat(shieldHeatPerSec * Time.deltaTime);
            if (!ok || _shieldTimer<=0 || _heat.IsStunned) StopShield();
            // блокировка урона — HealthSystem проверит _shieldActive? Пока заглушка: i-frame
            _controller.SetInvulnerable(true);
            _iFrameTimer = 0.1f;
        }

        public bool IsShieldActive => _shieldActive;
        public bool IsInvulnerable => _iFrameTimer>0 || _shieldActive;

        // Вызывается из HealthSystem для проверки
        public bool TryBlockDamage(int dmg)
        {
            if (IsShieldActive) { Debug.Log("[Shield] Blocked damage"); return true; }
            if (_iFrameTimer>0) { Debug.Log("[iFrame] Blocked damage"); return true; }
            return false;
        }
    }
}
