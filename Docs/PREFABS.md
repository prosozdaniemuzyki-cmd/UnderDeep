# Префабы и настройка тайлов/игрока

С версии `1.6.0+` весь контент можно настраивать через **префабы и ScriptableObjects** в инспекторе, а не только кодом. `VerticalSliceBootstrap` оставлен как fallback — если префаба нет в сцене, он создаст временный объект (как раньше).

## Где лежат ассеты

```
Assets/_Project/Prefabs/
  WorldGrid.prefab      # Мир: размер, биомы, тайлы, список BlockDef
  Drone.prefab          # Игрок LONE-1: скорость, гравитация, ссылки на спрайты
  Tiles/                # (генерируется) Tile assets для Tilemap (по 1 на BlockDef)

Assets/_Project/ScriptableObjects/
  Biomes/*.asset        # Surface, Metal, BioCaves, Crystal, Infection, Core
  Blocks/*.asset        # Dirt, Stone, RustMetal, CrystalOre, BioGel, GasVein, Wire, CrystalWall, ArtifactChunk, SurfaceCrust, FuelCanister, EnergyCell, Bedrock
  Upgrades/*.asset      # апгрейды

Assets/Editor/PrefabGenerator.cs  # меню UnderDeep → Generate ...
```

## Как настраивать тайлы

### 1. Блок = BlockDef

Каждый тип блока — это `BlockDef` ассет в `ScriptableObjects/Blocks/`:

- `type` — enum `BlockType` (не менять после создания)
- `displayName` — имя в UI
- `hp`, `hardness` — прочность и время бурения (сек, при drill=1.0)
- `dropType`, `dropMin/Max` — что выпадает (RustMetal/Crystal/BioGel/Artifact)
- `tileColor` — цвет тайла, если нет спрайта (используется Bootstrap для генерации Tile)
- `canDrill`, `isSurfaceCrust`, `fuelReward` — логика (для SurfaceCrust = false, для FuelCanister = +25 топлива)
- `sprite` — спрайт тайла (если назначен, используется вместо `tileColor`)

**Где редактировать:** Выдели `Assets/_Project/ScriptableObjects/Blocks/Dirt.asset` → Inspector → меняй `hardness` (0.45), `tileColor`, `sprite`. Изменение сразу отразится при следующем `Generate()` мира.

> Примечание: `SurfaceCrust` и `Bedrock` имеют `canDrill=false` — Bootstrap не даст их бурить даже если hardness=1.

### 2. Тайл = Tile

Если нужен кастомный спрайт:

1. В Unity меню `UnderDeep → Generate Tile Assets` (или `Generate All Prefabs + Tiles`).
2. Скрипт создаст `Assets/_Project/Prefabs/Tiles/DirtTile.asset` и т.д. — каждый Tile ссылается на белый пиксель + `tileColor` из BlockDef.
3. Замени `sprite` в Tile на свой (например, `Art/Tiles/dirt.png` с PPU=16, Filter=Point) и цвет оставь белым.
4. Перетяни Tile в поля `WorldGrid` префаба: `dirtTile`, `stoneTile`, ... `fuelCanisterTile`. Если поле заполнено, Bootstrap **не перегенерирует** тайлы.

Если поля тайлов пустые — Bootstrap сгенерирует их runtime из `BlockDef.tileColor` (как раньше).

### 3. Мир = WorldGrid.prefab

Открой `Prefabs/WorldGrid.prefab` → Inspector (`WorldGrid`):

- `width`/`height` — размер карты (по умолчанию 50×100). Если 0 — Bootstrap подставит дефолт.
- `seed` — сид генерации (127). Переопределяется сейвом Run если есть.
- `surfaceHeight` — толщина небуримой корки сверху (2). Изменение видно только после новой генерации.
- `tilemap` — ссылка на `Ground` Tilemap (создаётся автоматически, если пусто).
- `dirtTile` … `energyCellTile` — переопределение тайлов (см. п.2).
- `blockDefs` — список из 13 BlockDef. Можно удалить/добавить, переопределить hardness/цвет. Если пусто — Bootstrap создаст `CreateSliceBlockDefs()`.
- `biomes` — список из 6 BiomeDef. Можно настроить `stoneChance`, `crystalChance` и т.д. в `Biomes/*.asset`.

**Как использовать в сцене:**
Перетяни `WorldGrid.prefab` в `Run.unity` сцену (на корень). `VerticalSliceBootstrap` найдёт его через `Find<WorldGrid>()` и **не затрет** твои значения (`width`, `height`, `blockDefs`, `biomes`, `surfaceHeight`, `*_Tile`). Недостающий `Grid`/`Tilemap` создаст сам.

Чтобы вернуться к хардкоду — просто удали префаб из сцены, Bootstrap создаст временный.

## Как настраивать игрока

### Drone.prefab

Открой `Prefabs/Drone.prefab`:

- `DroneController`:
  - `moveSpeed` — 5 м/с
  - `gravity` — 18, `terminalVelocity` — 10
  - `slowFactorWhenNoFuel` — 0.5
  - `eyeDisplay` / `bodyRenderer` — ссылки (подключаются автоматически)
- `SpriteRenderer` — цвет `#59DB9E`, `SortingOrder=5`, `Sprite` — назначь свой спрайт дрона (PPU=16, Pivot=center). Если пусто — будет белый пиксель.
- `Rigidbody2D` — `Kinematic`, `GravityScale=0`
- `BoxCollider2D` — `Size 0.8×0.8`
- Дочерний `EyeDisplay` — `SpriteRenderer` с глазом (цвет `#1AFAB8`), можно заменить на анимацию.

Остальные системы (`FuelSystem`, `AbilitySystem`, `DroneAnimator`, `PlayerLight`...) добавляются **автоматически** при старте через `EnsurePlayerComponents()` если их нет в префабе. Если хочешь настроить Inspector-параметры:

1. Добавь компоненты вручную в префаб (Add Component → `FuelSystem`/`AbilitySystem`/etc).
2. Настрой: `FuelSystem.maxFuel`, `fuelPerCanister` (25), `AbilitySystem.dashDistance` (2), `dashFuelCost` (8) и т.д.
3. При запуске Bootstrap не затрет существующие компоненты, а только добавит недостающие.

**Как использовать в сцене:**
Перетяни `Drone.prefab` в `Run.unity`, переименуй в `LONE-1` если нужно (Bootstrap ищет `DroneController`, имя не важно). Камера `CameraFollow` подхватит его автоматически.

## Меню генерации

В Unity (после открытия проекта):

- `UnderDeep → Generate All Prefabs + Tiles` — пересоздаёт BlockDef/ Tiles/ WorldGrid/ Drone (не перезаписывает существующие BlockDef, только Tiles/префабы).
- `UnderDeep → Generate BlockDef Assets` — только `ScriptableObjects/Blocks/*.asset`.
- `UnderDeep → Generate Tile Assets` — создаёт `WhitePixel.png` + `Tiles/*Tile.asset` из `BlockDef.tileColor`.
- `UnderDeep → Generate WorldGrid Prefab` — префаб с текущими `BlockDef`/`BiomeDef`/`Tile` ссылками.
- `UnderDeep → Generate Drone Prefab` — префаб дрона с дефолтными компонентами.

> Генератор безопасен: не удаляет твои ассеты, пропускает существующие BlockDef. Для принудительной пересоздания — удали файл и запусти снова.

## Миграция со старого подхода (код)

Старый код в `VerticalSliceBootstrap.CreateSliceBlockDefs()` оставлен как fallback (`LoadBlockDefsOrCreateDefaults()`). Если в проекте нет `.asset` в `Blocks/` — используются хардкодные дефы. Поэтому старые сцены без префабов продолжат работать.

Чтобы полностью перейти на ассеты:
1. Убедись что `Blocks/` содержит 13 ассетов (уже есть).
2. Убедись что `WorldGrid.prefab` ссылается на них (поле `blockDefs` заполнено — уже).
3. Удали вызов `CreateSliceBlockDefs()` из кастомных сцен если переопределял Bootstrap.

## Частые вопросы

**Q: Поменял `tileColor` в BlockDef, но в игре цвет старый?**
A: Если в `WorldGrid.prefab` назначены Tile ассеты — цвет берётся из Tile (`tile.color`), а не из BlockDef. Либо переназначь Tile через `Generate Tile Assets`, либо очисти поля `*Tile` в WorldGrid чтобы вернуться к runtime-генерации.

**Q: Хочу свой спрайт дрона.**
A: Импортируй спрайт в `Art/Drone/` (Texture Type=Sprite, Pixels Per Unit=16, Filter=Point, Compression=None), перетяни в `Drone.prefab → SpriteRenderer → Sprite`.

**Q: Хочу добавить новый блок (например, Lava).**
A: 1) `Create → UnderDeep → BlockDef`, назначь `type=Lava` (добавь в enum `BlockType`), `hardness`, `tileColor`. 2) `Generate Tile Assets` → появится `LavaTile`. 3) Добавь в `WorldGrid.prefab → blockDefs`. 4) Добавь обработку в `WorldGrid.RollBlock`/`WorldGrid.TileFor` если нужна особая логика спавна.

**Q: Можно ли редактировать размер шахты?**
A: Да, `WorldGrid.surfaceHeight` (2) и код `Generate()` — шахта 2×6 жёстко зашита (`for y=height-1 .. height-6`). Если нужно шире — правь `WorldGrid.cs` или вынеси в поля `shaftWidth`/`shaftDepth`.

