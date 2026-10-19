using UnityEngine;
using UnityEngine.InputSystem;
using UnderDeep.Core;
using UnderDeep.World;

namespace UnderDeep.Player
{
    public class DrillSystem : MonoBehaviour
    {
        [Header("Config")]
        public float drillPower = 1f; // multiplied by upgrade
        public float autoMagnetRadius = 0f;

        private Camera _cam;
        private WorldGrid _world;
        private FuelSystem _fuel;
        private OverheatSystem _heat;
        private Vector2Int? _target;
        private DroneController _drone;

        void Start()
        {
            _cam = Camera.main;
            _world = ServiceLocator.Get<WorldGrid>();
            _fuel = GetComponent<FuelSystem>();
            _heat = GetComponent<OverheatSystem>();
            _drone = GetComponent<DroneController>();
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
            if (ServiceLocator.Get<GameStateMachine>().Current != GameState.Run) return;
            if (_heat != null && _heat.IsStunned) return;

            // Convert one crystal collected this run into emergency drilling fuel.
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                _fuel?.TryRefuelFromRunCrystal();

            Vector2Int? desired = null;

            // Mouse / touch
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                var wp = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                var grid = _world.WorldToGrid(wp);
                // only neighbor tiles (including diagonal)
                if (Vector2Int.Distance(_drone.GridPos, grid) <= 1.5f) desired = grid;
            }

            // Also drill look tile if Space pressed
            if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
                desired = _drone.GetLookTile();

            if (desired.HasValue)
            {
                if (_target != desired)
                {
                    _target = desired;
                    EventBus.Publish(new DrillStarted(desired.Value));
                }
                TickDrill(desired.Value);
            }
            else
            {
                if (_target.HasValue) { EventBus.Publish(new DrillEnded(_target.Value, false)); _target=null; }
            }

            // bio gel use (лечит и снижает безумие)
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (ServiceLocator.TryGet<PlayerInventory>(out var inv) && inv.Get(ResourceType.BioGel)>0)
                {
                    inv.Add(ResourceType.BioGel, -1);
                    GetComponent<SanitySystem>()?.UseBioGel();
                }
            }
            // быстрый эвак на B: Player не ссылается на Progression — шлём запрос через EventBus, его ловит RunResultHandler
            if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
            {
                EventBus.Publish(new EvacRequested());
            }
        }

        void TickDrill(Vector2Int pos)
        {
            if (!_world.CanDrill(pos))
            {
                // maybe move into it? no
                return;
            }
            if (_fuel != null && _fuel.IsEmpty)
            {
                Debug.Log("[Drill] No fuel!");
                return;
            }
            // fuel + heat
            _fuel?.TryConsumeDrill(Time.deltaTime);
            _heat?.AddHeat(Time.deltaTime);

            var res = _world.DrillTick(pos, drillPower, Time.deltaTime);
            if (res.destroyed)
            {
                EventBus.Publish(new DrillEnded(pos, true));
                _target = null;
                // magnet auto pick already via BlockMined -> Inventory
                // spawn VFX via pool (stub)
            }
        }
    }
}
