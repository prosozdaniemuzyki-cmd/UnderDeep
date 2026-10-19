using UnityEngine;
using UnityEngine.InputSystem;
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

        [Header("References")]
        public Transform eyeDisplay; // зелёный глаз
        public SpriteRenderer bodyRenderer;

        private Rigidbody2D _rb;
        private Vector2 _input;
        private Vector2Int _gridPos;
        private WorldGrid _world;
        private FuelSystem _fuel;
        private Camera _cam;
        private int _lastReportedDepth = int.MinValue;

        public Vector2Int GridPos => _gridPos;
        public int Depth => _gridPos.y;

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
            _cam = Camera.main;
            // Spawn in the small prepared shaft, directly below the surface.
            _gridPos = new Vector2Int(_world.width / 2, Mathf.Min(5, _world.height - 1));
            transform.position = _world.GridToWorld(_gridPos);
        }

        void Update()
        {
            // Input (new Input System if available, fallback to old)
            _input = Vector2.zero;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) _input.y += 1;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) _input.y -= 1;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) _input.x -= 1;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) _input.x += 1;
            }
            // mouse click drill handled by DrillSystem

            // Publish only when entering a new grid row (avoids per-frame event churn).
            if (_lastReportedDepth != Depth && ServiceLocator.TryGet<GameStateMachine>(out var gsm))
            {
                gsm.ReportDepth(Depth);
                _lastReportedDepth = Depth;
            }
        }

        void FixedUpdate()
        {
            if (_input == Vector2.zero) return;
            float speed = moveSpeed;
            if (_fuel != null && _fuel.IsEmpty) speed *= slowFactorWhenNoFuel;

            var target = (Vector2)transform.position + _input.normalized * speed * Time.fixedDeltaTime;
            var targetGrid = _world.WorldToGrid(target);

            // grid collision: can only move into Air
            if (_world.CanPass(targetGrid) || targetGrid == _gridPos)
            {
                _rb.MovePosition(target);
                if (targetGrid != _gridPos)
                {
                    var prev = _gridPos;
                    _gridPos = targetGrid;
                    EventBus.Publish(new PlayerMoved(prev, _gridPos));
                }
            }
            else
            {
                // slide along axis
                var tryX = new Vector2Int(targetGrid.x, _gridPos.y);
                var tryY = new Vector2Int(_gridPos.x, targetGrid.y);
                if (_world.CanPass(tryX))
                {
                    _rb.MovePosition(new Vector2(target.x, transform.position.y));
                }
                else if (_world.CanPass(tryY))
                {
                    _rb.MovePosition(new Vector2(transform.position.x, target.y));
                }
            }

            // eye animation
            if (eyeDisplay) eyeDisplay.localScale = Vector3.Lerp(eyeDisplay.localScale, _input.magnitude>0.1f? Vector3.one*1.1f: Vector3.one, Time.deltaTime*8);
        }

        public Vector2Int GetLookTile()
        {
            if (_input==Vector2.zero) return _gridPos + Vector2Int.down;
            var dir = new Vector2Int(Mathf.RoundToInt(_input.x), Mathf.RoundToInt(_input.y));
            if (dir==Vector2Int.zero) dir = Vector2Int.down;
            return _gridPos + dir;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
