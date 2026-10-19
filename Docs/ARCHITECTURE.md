# Архитектура Deep Lone

## Слои

```
Boot (ServiceLocator, Platform init, Save load)
  → Base (магазин апгрейдов, UpgradeSystem)
  → Run (WorldGrid, Player, Enemies, HUD)
     → Evac/Death (RunResult → MetaSave)
  → Base (цикл)
```

## Диаграмма зависимостей

```
Core (EventBus, ServiceLocator, GameStateMachine, SaveService, Pool)
  ↑
World ———→ Player ———→ Enemies
  ↑            ↑
  └──── Progression (UpgradeSystem, RunResult)
               ↑
              UI
               ↑
          Platform (IPlatformProvider)
```

## Ключевые классы

### Core
- `GameStateMachine` — стейт-машина (Boot/Base/Run/Pause/Death/Evac)
- `EventBus` — zero-GC ивент-шина
- `ServiceLocator` — регистрация IPlatformProvider, ISaveService, WorldGrid и т.д.
- `SaveService` — сериализация MetaSave в JSON → IPlatformProvider

### World
- `WorldGrid` — `BlockData[width,height]`, источник истины. Синхронизирует Tilemap.
- `WorldGenerator` — детерминированная генерация по seed + биомам глубины.
- `BiomeDef` (SO) — палитра блоков, шанс руды, декор, враги.
- `BlockDef` (SO) — hp, твердость, дроп, цвет.

### Player
- `DroneController` — движение по сетке, коллизия через WorldGrid.CanPass
- `DrillSystem` — рейкаст по клику/тачу, прогресс бурения, расход топлива/тепла
- `FuelSystem`, `OverheatSystem`, `SanitySystem`, `HealthSystem`
- `Inventory` — runInventory + metaInventory (через Progression)

### Enemies
- `EnemyBase` : MonoBehaviour + IPoolable
- `EnemySpawner` — спавнит по биому, пуллит

### Progression
- `UpgradeSystem` — покупка, применение StatModifier
- `RunResultHandler` — подсчет лута, сохранение/обнуление

### UI
- `HUD` — топливо, тепло, глубина, безумие, инвентарь
- `BaseShopUI` — список апгрейдов
- `DeathScreen`, `EvacScreen`

### Platform
- `IPlatformProvider` — абстракция
- `YandexProvider`, `VKProvider`, `StandaloneProvider`
- `Plugins/WebGL/*.jslib` — JS-мост

## Поток данных бурения

```
Input (Mouse/WASD) → DroneController → DrillSystem.CanDrill(pos) → WorldGrid.CanDrill
→ DrillSystem.BeginDrill(pos) → тик: FuelSystem.Consume(), OverheatSystem.AddHeat()
→ прогресс >= hardness → WorldGrid.DestroyBlock(pos) → EventBus.BlockMined → Inventory.Add → VFX/Pool
```

## Сохранения

- `MetaSave` (JSON) → `IPlatformProvider.SaveAsync` → Yandex Player Storage / PlayerPrefs
- `RunSave` — в памяти, не сериализуется до Evac

## WebGL особенности

- Boot сцена — 1-2 сек, вызывает `IPlatformProvider.GameReady()` после `InitAsync()`
- `visibilitychange` → `GameplayStop()`/`Time.timeScale=0`
- Нет `Thread`, `Task` только с `UniTask` или корутинами (используем `Awaitable` Unity 6)
