using UnityEngine;
using UnityEngine.InputSystem;

namespace UnderDeep.Player
{
    /// <summary>Единая точка ввода: клавиатура + геймпад + тач/мышь. Дает вектор движения и кнопки способностей.</summary>
    public class InputReader : MonoBehaviour
    {
        public Vector2 Move { get; private set; }
        public Vector2 LastNonZeroMove { get; private set; } = Vector2.down;
        public bool DashPressed { get; private set; }
        public bool ShieldHeld { get; private set; }
        public bool MouseDrillActive { get; private set; }
        public Vector2Int MouseDrillTarget { get; private set; }

        [Header("Gamepad")]
        public float deadZone = 0.2f;
        public float stickPowerCurve = 1.2f;

        // Кэш для тача
        Camera _cam;
        World.WorldGrid _world;
        Player.DroneController _drone;

        void Awake() => _cam = Camera.main;
        void Start()
        {
            _world = FindFirstObjectByType<World.WorldGrid>();
            _drone = GetComponent<Player.DroneController>();
            if (_drone==null) _drone = FindFirstObjectByType<Player.DroneController>();
        }

        void Update()
        {
            ReadMove();
            ReadDashShield();
            ReadMouseDrill();
            if (Move.sqrMagnitude > 0.01f) LastNonZeroMove = Move.normalized;
        }

        void ReadMove()
        {
            Vector2 v = Vector2.zero;

            // Keyboard WASD + стрелки
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v.y += 1;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v.y -= 1;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) v.x -= 1;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) v.x += 1;
            }
            // Gamepad left stick + dpad
            if (Gamepad.current != null)
            {
                var s = Gamepad.current.leftStick.ReadValue();
                if (s.magnitude > deadZone)
                {
                    float m = Mathf.Pow(s.magnitude, stickPowerCurve);
                    s = s.normalized * m;
                    v += s;
                }
                var d = Gamepad.current.dpad.ReadValue();
                v += d;
            }

            // Clamp
            if (v.magnitude > 1) v.Normalize();
            // deadzone final
            if (v.magnitude < deadZone) v = Vector2.zero;

            Move = v;
        }

        void ReadDashShield()
        {
            bool dash = false, shield = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame) dash = true;
                if (Keyboard.current.qKey.isPressed || Keyboard.current.leftCtrlKey.isPressed) shield = true;
            }
            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonEast.wasPressedThisFrame) dash = true; // B / Circle
                if (Gamepad.current.leftTrigger.isPressed || Gamepad.current.leftShoulder.isPressed) shield = true;
            }
            DashPressed = dash;
            ShieldHeld = shield;
        }

        void ReadMouseDrill()
        {
            MouseDrillActive = false;
            if (_world==null || _cam==null || _drone==null) return;

            // Мышь ЛКМ — тап по соседнему блоку (остаётся как альтернатива стику)
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                var wp = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                var grid = _world.WorldToGrid(wp);
                if (Vector2Int.Distance(_drone.GridPos, grid) <= 1.5f)
                {
                    MouseDrillActive = true;
                    MouseDrillTarget = grid;
                }
            }
            // Тач
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                var wp = _cam.ScreenToWorldPoint(Touchscreen.current.primaryTouch.position.ReadValue());
                var grid = _world.WorldToGrid(wp);
                if (Vector2Int.Distance(_drone.GridPos, grid) <= 1.5f)
                {
                    MouseDrillActive = true;
                    MouseDrillTarget = grid;
                }
            }
        }

        public Vector2Int QuantizeDirection(Vector2 raw)
        {
            if (raw.sqrMagnitude < 0.001f) return Vector2Int.down; // default down gravity feel
            // 8-way
            float ang = Mathf.Atan2(raw.y, raw.x) * Mathf.Rad2Deg;
            if (ang < 0) ang += 360;
            // 0°=E, 90°=N
            if (ang >= 337.5f || ang < 22.5f) return Vector2Int.right;
            if (ang >= 22.5f && ang < 67.5f) return new Vector2Int(1,1);
            if (ang >= 67.5f && ang < 112.5f) return Vector2Int.up;
            if (ang >= 112.5f && ang < 157.5f) return new Vector2Int(-1,1);
            if (ang >= 157.5f && ang < 202.5f) return Vector2Int.left;
            if (ang >= 202.5f && ang < 247.5f) return new Vector2Int(-1,-1);
            if (ang >= 247.5f && ang < 292.5f) return Vector2Int.down;
            return new Vector2Int(1,-1);
        }
    }
}
