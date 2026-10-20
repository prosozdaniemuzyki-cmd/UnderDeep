using UnityEngine;
using UnderDeep.Core;
using UnderDeep.World;

namespace UnderDeep.Player
{
    /// <summary>v2: бур автоматически в направлении движения/стики, без отдельной кнопки Space. Клик/тап остаётся как альтернатива.</summary>
    public class DrillSystem : MonoBehaviour
    {
        [Header("Config")]
        public float drillPower = 1f;
        public float autoMagnetRadius = 0f;
        [Tooltip("Ускорение бурения вниз при падении")] public float downDrillBonus = 1.2f;

        private WorldGrid _world;
        private FuelSystem _fuel;
        private OverheatSystem _heat;
        private Vector2Int? _target;
        private DroneController _drone;
        private InputReader _input;
        private bool _noFuelWarningShown;

        void Start()
        {
            _world = ServiceLocator.Get<WorldGrid>();
            _fuel = GetComponent<FuelSystem>();
            _heat = GetComponent<OverheatSystem>();
            _drone = GetComponent<DroneController>();
            _input = GetComponent<InputReader>();
            if (_input==null) _input = gameObject.AddComponent<InputReader>();
            ApplyUpgrades();
        }

        void ApplyUpgrades()
        {
            if (!ServiceLocator.TryGet<SaveService>(out var save)) return;
            if (save.Meta.purchasedUpgrades.Contains("drill_mk2")) drillPower = 1.25f;
            if (save.Meta.purchasedUpgrades.Contains("drill_mk3")) drillPower = 1.5f;
            if (save.Meta.purchasedUpgrades.Contains("magnet")) autoMagnetRadius = 3f;
        }

        void Update()
        {
            if (!ServiceLocator.TryGet<GameStateMachine>(out var gsm) || gsm.Current != GameState.Run) return;

            // Эвакуация B — работает даже в стане
            if (_input != null && false) {} // InputReader Dash already handled
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.bKey.wasPressedThisFrame)
            {
                EventBus.Publish(new EvacRequested());
                if (ServiceLocator.Get<GameStateMachine>().Current != GameState.Run) return;
            }
            // Gamepad Start/Select тоже эвакуация
            if (UnityEngine.InputSystem.Gamepad.current != null && UnityEngine.InputSystem.Gamepad.current.startButton.wasPressedThisFrame)
                EventBus.Publish(new EvacRequested());

            if (_heat != null && _heat.IsStunned) return;

            // v2: убран R → пополнение только блоками FuelCanister (см. FuelSystem.OnFuelBlockMined)

            Vector2Int desired = _drone.GetAutoDrillTarget();

            // авто-бурение пока зажат ввод (или тап)
            bool wantsDrill = false;
            if (_input.MouseDrillActive) wantsDrill = _world.CanDrill(desired);
            else if (_input.Move.sqrMagnitude > 0.01f) wantsDrill = _world.CanDrill(desired);

            if (wantsDrill)
            {
                if (_target != desired)
                {
                    _target = desired;
                    EventBus.Publish(new DrillStarted(desired));
                    EventBus.Publish(new DrillAutoStarted(desired, _input.Move));
                }
                TickDrill(desired);
            }
            else
            {
                if (_target.HasValue) { EventBus.Publish(new DrillEnded(_target.Value, false)); _target=null; }
            }

            // био-гель E остаётся
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (ServiceLocator.TryGet<PlayerInventory>(out var inv) && inv.Get(ResourceType.BioGel)>0)
                {
                    inv.Add(ResourceType.BioGel, -1);
                    GetComponent<SanitySystem>()?.UseBioGel();
                }
            }
        }

        void TickDrill(Vector2Int pos)
        {
            if (!_world.CanDrill(pos)) return;
            if (_fuel != null && _fuel.IsEmpty)
            {
                if (!_noFuelWarningShown) Debug.Log("[Drill] No fuel; rescue countdown is active.");
                _noFuelWarningShown = true;
                return;
            }
            _noFuelWarningShown = false;

            float power = drillPower;
            // бонус вниз при падении/гравитации
            if (pos == _drone.GridPos + Vector2Int.down && _drone.IsFalling) power *= downDrillBonus;

            _fuel?.TryConsumeDrill(Time.deltaTime);
            _heat?.AddHeat(Time.deltaTime);

            var res = _world.DrillTick(pos, power, Time.deltaTime);
            if (res.destroyed)
            {
                EventBus.Publish(new DrillEnded(pos, true));
                _target = null;
                // топливо-блоки дают топливо
                if (res.type==BlockType.FuelCanister || res.type==BlockType.EnergyCell)
                    _fuel?.OnFuelBlockMined(res.type);
            }
        }
    }
}
