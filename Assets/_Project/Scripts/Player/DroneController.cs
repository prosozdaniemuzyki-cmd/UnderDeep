using UnityEngine;
using UnderDeep.Core;
using UnderDeep.World;

namespace UnderDeep.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class DroneController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 5f;
        public float slowFactorWhenNoFuel = 0.5f;

        [Header("Gravity (v2: дрон появляется сверху и падает вниз)")]
        public float gravity = 18f;
        public float terminalVelocity = 10f;
        public float fallThreshold = 0.1f;

        [Header("References")]
        public Transform eyeDisplay;
        public SpriteRenderer bodyRenderer;

        private Rigidbody2D _rb;
        private Vector2 _input;
        private Vector2Int _gridPos;
        private WorldGrid _world;
        private FuelSystem _fuel;
        private InputReader _inputReader;
        private int _lastReportedDepth = int.MinValue;

        // falling
        float _fallSpeed;
        bool _isGrounded;
        bool _wasFalling;

        // invuln for dash/shield
        bool _invulnerable;

        public Vector2Int GridPos => _gridPos;
        // Depth теперь считается от поверхности сверху вниз: 0 на поверхности, растёт внизу
        public int Depth => _world != null ? _world.GetDepthForY(_gridPos.y) : _gridPos.y;
        public bool IsGrounded => _isGrounded;
        public bool IsFalling => !_isGrounded && _fallSpeed > 0.1f;
        public Vector2 RawInput => _input;
        public bool IsInvulnerable => _invulnerable;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0;
            ServiceLocator.Register(this);
        }

        void Start()
        {
            _world = ServiceLocator.Get<WorldGrid>();
            _fuel = GetComponent<FuelSystem>();
            _inputReader = GetComponent<InputReader>();
            if (_inputReader == null) _inputReader = gameObject.AddComponent<InputReader>();

            // v2 fix: спавн на поверхности СВЕРХУ, спуск ВНИЗ (depth растёт вниз)
            int sx = _world.width / 2;
            _gridPos = new Vector2Int(sx, _world.height - 2); // y = 98 при height 100 — внутри шахты у поверхности
            transform.position = _world.GridToWorld(_gridPos);
            _fallSpeed = 0;
            Debug.Log($"[Drone] Spawn at {_gridPos} world {transform.position} depth {Depth}");
        }

        void Update()
        {
            // читаем ввод через InputReader (клавиатура + геймпад)
            if (_inputReader != null) _input = _inputReader.Move;
            else _input = Vector2.zero;

            // depth publish
            if (_lastReportedDepth != Depth && ServiceLocator.TryGet<GameStateMachine>(out var gsm))
            {
                gsm.ReportDepth(Depth);
                _lastReportedDepth = Depth;
            }

            // глаз — направление
            if (eyeDisplay)
            {
                Vector3 targetScale = _input.sqrMagnitude > 0.01f ? new Vector3(Mathf.Sign(_input.x != 0 ? _input.x : 1), 1, 1) * 1.1f : Vector3.one;
                eyeDisplay.localScale = Vector3.Lerp(eyeDisplay.localScale, targetScale, Time.deltaTime * 8);
            }
        }

        void FixedUpdate()
        {
            HandleGravity();

            // движение только если есть ввод
            if (_input.sqrMagnitude < 0.01f)
            {
                // даже без ввода гравитация уже обработана
                return;
            }

            // если падавем быстро — не мешаем гравитации, только горизонтальный ввод
            if (IsFalling && Mathf.Abs(_input.x) < 0.1f) return;

            float speed = moveSpeed;
            if (_fuel != null && _fuel.IsEmpty) speed *= slowFactorWhenNoFuel;

            Vector2 delta = _input.normalized * speed * Time.fixedDeltaTime;
            // если падаем — ограничиваем вертикальный ввод вниз (гравитация сама)
            if (IsFalling && _input.y < 0) delta.y = 0;

            var target = (Vector2)transform.position + delta;
            var targetGrid = _world.WorldToGrid(target);

            if (_world.CanPass(targetGrid) || targetGrid == _gridPos)
            {
                _rb.MovePosition(target);
                TryChangeGrid(targetGrid);
            }
            else
            {
                // скольжение по осям (как раньше)
                var tryX = new Vector2Int(targetGrid.x, _gridPos.y);
                var tryY = new Vector2Int(_gridPos.x, targetGrid.y);
                if (_world.CanPass(tryX))
                    _rb.MovePosition(new Vector2(target.x, transform.position.y));
                else if (_world.CanPass(tryY))
                    _rb.MovePosition(new Vector2(transform.position.x, target.y));
                // если не можем пройти — DrillSystem начнёт бурить в этом направлении (см. DrillSystem v2)
            }
        }

        void HandleGravity()
        {
            var below = _gridPos + Vector2Int.down;
            bool supportBelow = !_world.IsAir(below) && _world.InBounds(below);
            // если прямо под нами воздух — падаем
            if (_world.IsAir(below) && _world.InBounds(below))
            {
                _isGrounded = false;
                if (!_wasFalling) EventBus.Publish(new PlayerFalling(_gridPos, _fallSpeed));
                _wasFalling = true;
                _fallSpeed = Mathf.Min(terminalVelocity, _fallSpeed + gravity * Time.fixedDeltaTime);
                var fallDelta = Vector2.down * _fallSpeed * Time.fixedDeltaTime;
                var target = (Vector2)transform.position + fallDelta;
                var targetGrid = _world.WorldToGrid(target);
                // падаем пока Air
                if (_world.CanPass(targetGrid) || targetGrid == _gridPos)
                {
                    _rb.MovePosition(target);
                    TryChangeGrid(targetGrid);
                }
                else
                {
                    // приземлились на блок (который мог быть разрушен бурением — уже Air, но проверяем)
                    _isGrounded = true;
                }
            }
            else
            {
                if (_wasFalling)
                {
                    EventBus.Publish(new PlayerLanded(_gridPos, _fallSpeed));
                    EventBus.Publish(new DroneAnimationRequested(DroneAnimState.Idle, Vector2.zero));
                }
                _wasFalling = false;
                _isGrounded = supportBelow;
                _fallSpeed = 0;
            }
        }

        void TryChangeGrid(Vector2Int newGrid)
        {
            if (newGrid == _gridPos) return;
            var prev = _gridPos;
            _gridPos = newGrid;
            _world.WorldToGrid(transform.position); // sync sanity
            EventBus.Publish(new PlayerMoved(prev, _gridPos));
        }

        public Vector2Int GetLookTile()
        {
            if (_input.sqrMagnitude < 0.01f) return _gridPos + Vector2Int.down;
            var q = _inputReader != null ? _inputReader.QuantizeDirection(_input) : new Vector2Int(Mathf.RoundToInt(_input.x), Mathf.RoundToInt(_input.y));
            if (q == Vector2Int.zero) q = Vector2Int.down;
            return _gridPos + q;
        }

        public Vector2Int GetAutoDrillTarget()
        {
            // приоритет: мышь/тач
            if (_inputReader != null && _inputReader.MouseDrillActive) return _inputReader.MouseDrillTarget;
            // иначе направление движения
            return GetLookTile();
        }

        public void SetInvulnerable(bool v) => _invulnerable = v;
        public void TeleportToGrid(Vector2Int pos)
        {
            if (!_world.InBounds(pos)) return;
            var prev = _gridPos;
            _gridPos = pos;
            transform.position = _world.GridToWorld(pos);
            EventBus.Publish(new PlayerMoved(prev, pos));
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = _isGrounded ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            if (_world != null) { Gizmos.color = Color.cyan; Gizmos.DrawLine(transform.position, transform.position + (Vector3)_input); }
        }
    }
}
