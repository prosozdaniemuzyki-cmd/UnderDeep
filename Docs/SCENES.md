# Сцены Deep Lone

## Build Settings порядок

1. `Assets/_Project/Scenes/Boot.unity`
2. `Assets/_Project/Scenes/Base.unity`
3. `Assets/_Project/Scenes/Run.unity`

## Boot

Цель: инициализация SDK и загрузка сохранения.

Объекты:
- `Boot` (GameObject) с `UnderDeep.Core.Boot` — `DontDestroyOnLoad`, вызывает Yandex/VK init, грузит MetaSave, создаёт GameStateMachine, вызывает `GameReady()` и грузит `Base`.
- `Main Camera` + `Global Light 2D` (URP).

Настройки: камера orthographic size 5, фон `#31497A`.

## Base (База)

Магазин между забегами.

Объекты:
- `Main Camera`
- `Canvas (Screen Space - Overlay)`:
  - `ResourcesText` (TMP) — показывает ресурсы MetaSave
  - `ListRoot` (VerticalLayout) — спавнит строки апгрейдов из `BaseShopUI`
  - `RowPrefab` — префаб строки: Text + Button
  - `StartRunButton` — `Тонуть вглубь!`
- `BaseShop` (GO) с `UpgradeSystem` (список UpgradeDef) + `BaseShopUI`
- `EventSystem`

Логика: `BaseShopUI.Rebuild()` читает `SaveService.Meta` и `UpgradeSystem.All`, строит список. `OnStartRun()` делает `NewRun(seed)` и грузит `Run`.

## Run (Забег)

Основная шахта.

Объекты:
- `Grid` + `Tilemap` (Ground) + `TilemapCollider2D` + `CompositeCollider2D` + `AutoWorldSetup`
- `WorldGrid` (width=50 height=100) — Tilemap sync, генерация
- `Player` (Drone):
  - Sprite (пиксель-арт 32x32), `Rigidbody2D` (Kinematic), `BoxCollider2D`
  - `DroneController`, `DrillSystem`, `FuelSystem`, `OverheatSystem`, `SanitySystem`, `HealthSystem`, `PlayerInventory`
  - `CircleCollider2D` для подбора? (опционно)
- `Enemies` (пустой) + `EnemySpawner` + `poolParent`
- `HUD Canvas`:
  - `FuelBar` (Slider), `HeatBar` (Slider), `SanityBar`
  - `DepthText`, `LootText`, `HpText`
  - `Vignette` (Image, чёрный с альфой)
  - `EvacButton` → `RunResultHandler.Evacuate()`
  - `ReviveButton` → Rewarded
  - `DeathPanel` (скрыта) — `DeathScreen`
- `Main Camera` + `CameraFollow` (цель = Player)
- `Global Light 2D` (интенсивность 0.7) + `Point Light 2D` на игроке (радиус 5, flicker)
- `RunResultHandler`

Логика: игрок бурит кликом по тайлу (соседний). `WorldGrid.DrillTick` → `BlockMined` → `Inventory.Add`. Топливо/перегрев/безумие тикают.

## Быстрая настройка в редакторе

1. Открой `Run.unity`
2. Добавь `Grid` → `Tilemap` → назначь в `WorldGrid.tilemap`
3. Создай ассеты плиток: `RightClick → 2D → Tiles → Rectangular Tile` для каждого BlockType, цвет по `BlockDef.tileColor`
4. Назначь их в `WorldGrid` (dirtTile, crystalTile…)
5. Создай апгрейды: `UnderDeep → Create Default Upgrades`
6. Создай биомы: `UnderDeep → Create Default Biomes` → назначь в `WorldGrid.biomes`
7. Префаб игрока: квадрат 32px + глаз (зелёный круг) как child `eyeDisplay`
8. Префаб червя: красный квадрат 32px + `RustWorm` + `BoxCollider2D` (Trigger)

См. также `Assets/Editor/SetupDefaults.cs`.
