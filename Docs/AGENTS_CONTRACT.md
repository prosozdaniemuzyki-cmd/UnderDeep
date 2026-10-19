# Контракт работы агентов — проект UnderDeep / Deep Lone

> **Версия:** 1.0 (2026-10-19)
> **Движок:** Unity 6000.3 LTS (Unity 6) URP 2D
> **Платформы:** WebGL (Yandex Games, VK Play / VK Games, CrazyGames/itch.io stand-alone)
> **Жанр:** 2D Tile-based Drill Roguelite (Downwell + SteamWorld Dig)
> **Репозиторий:** `prosozdaniemuzyki-cmd/UnderDeep` ветка `arena/6db305ea-underdeep`
> **Язык кода и комментариев:** C# + комментарии на русском / API docs на английском
> **Язык коммитов и документации:** русский

Этот документ — **единый источник истины** для всех AI/человек-агентов работающих над проектом.
Любые изменения архитектуры ОБЯЗАНЫ пройти через обновление этого файла (ADR в конце).

---

## 1. Цель и принципы

1.  **Web-first:** игра должна запускаться в браузере за <15 сек, сборка WebGL ≤ 30 МБ (Brotli).
2.  **Короткая сессия 5–15 мин** — ключевая петля для ЯИгр. Нет блокирующих загрузок, пауза/туториал скипается.
3.  **Детерминизм мира:** один `seed` → один мир. Сид хранится в сохранении забега.
4.  **Один игрок — один дроид LONE-1.** Нет стрельбы, только бурение/движение/менеджмент ресурсов.
5.  **Roguelite-цикл:** смерть = потеря забега, сохранения — только мета-прогресс (апгрейды + артефакты). Это священно, не ломать.
6.  **Не навреди перформансу WebGL:** нет `Update()` с аллокациями, нет `FindObjectOfType` в рантайме, нет потоков, нет синхронного `UnityWebRequest.Wait`.
7.  **Платформо-независимость:** весь платформенный код за интерфейсом `IPlatformProvider`. Игра не знает, Яндекс она или VK.

---

## 2. Роли агентов и зоны ответственности

Каждый агент — владелец своей папки. **Править чужие папки можно только через интерфейс** (public API / события). Прямые правки чужого кода — нарушение контракта.

| Агент | Папка(и) | Отвечает за | Запрещено |
|-------|----------|-------------|-----------|
| **Architect** | `Docs/`, `Assets/_Project/Core/`, `*.asmdef`, `ProjectSettings/` | Контракт, сборки, адрессабельность, CI/CD, ADR | Писать геймплей логику |
| **World Agent** | `Assets/_Project/Scripts/World/`, `Assets/_Project/ScriptableObjects/Biomes/`, `Prefabs/World/` | `WorldGrid`, генерация, `BlockType`, биомы, тайлмап, разрушения, ловушки | Трогать `Player/` кроме `IDrillTarget` |
| **Gameplay/Player Agent** | `Assets/_Project/Scripts/Player/`, `Prefabs/Player/` | `DroneController`, `DrillSystem`, `FuelSystem`, `OverheatSystem`, `SanitySystem`, `Health`, `Inventory` | Генерация мира, UI верстка |
| **Enemy/AI Agent** | `Assets/_Project/Scripts/Enemies/` | `EnemyBase`, `RustWorm`, `Firefly`, `CrystalSpider`, `Hallucination`, спавнер | Менять баланс топлива/бура без согласования |
| **Progression Agent** | `Assets/_Project/Scripts/Progression/`, `ScriptableObjects/Upgrades/` | `UpgradeSystem`, `RunResult`, `MetaSave`, `ArtifactSystem`, баланс цен | Менять платформенный слой |
| **UI/UX Agent** | `Assets/_Project/Scripts/UI/`, `Prefabs/UI/`, `Assets/_Project/UI/` | HUD, экран базы, экран смерти/эвакуации, туториал, экран безумия, локализация | Бизнес-логика добычи |
| **Platform Agent** | `Assets/_Project/Scripts/Platform/`, `Plugins/WebGL/` | `IPlatformProvider`, Yandex SDK, VK Bridge, `SaveService` (WebGL), реклама, лидерборды, IAP | Менять геймплей |
| **TechArt/Audio Agent** | `Assets/_Project/Art/`, `Assets/_Project/Audio/`, `Assets/_Project/VFX/`, `Shaders/` | Тайлы 16/32, SpriteLight, анимации дроида, SFX, URP Renderer2D | Менять C# API |
| **QA Agent** | `Tests/`, `Docs/QA/` | PlayMode/EditMode тесты, чек-листы WebGL, баланс-тесты, регресс | Пушить фичи без тестов |

> **Правило одного владельца:** если файл в таблице принадлежит агенту A, агент B может предложить изменение только через PR с тегом `needs-review:A`.

---

## 3. Архитектура проекта

### 3.1 Структура папок (каноническая)

```
Assets/_Project/
├── Core/                 // Architect: EventBus, GameState, ServiceLocator, Pool, Save
├── Scripts/
│   ├── Core/
│   ├── World/
│   ├── Player/
│   ├── Enemies/
│   ├── Progression/
│   ├── UI/
│   ├── Platform/
│   └── Utils/
├── ScriptableObjects/
│   ├── Blocks/
│   ├── Biomes/
│   └── Upgrades/
├── Prefabs/
│   ├── World/
│   ├── Player/
│   ├── Enemies/
│   └── UI/
├── Art/
├── Audio/
├── Scenes/
│   ├── Boot.unity          // Точка входа, инициализация SDK
│   ├── Base.unity          // База/магазин апгрейдов (между забегами)
│   └── Run.unity           // Забег (тайловая шахта)
└── Plugins/WebGL/          // .jslib для Яндекса/VK
Docs/
├── AGENTS_CONTRACT.md      // этот файл
├── ARCHITECTURE.md
├── BALANCE.md
└── PLATFORM.md
Tests/
├── EditMode/
└── PlayMode/
```

### 3.2 Assembly Definitions (изоляция)

```
UnderDeep.Core.asmdef        // никакого UnityEngine.UI, только Core
UnderDeep.World.asmdef       // зависит от Core
UnderDeep.Player.asmdef      // зависит от Core, World (IDrillTarget)
UnderDeep.Enemies.asmdef     // зависит от Core, World, Player (только интерфейсы)
UnderDeep.Progression.asmdef // зависит от Core, Player
UnderDeep.UI.asmdef          // зависит от Core, Progression, Player (read-only)
UnderDeep.Platform.asmdef    // зависит от Core, Progression (Save)
```

**Запрещены циклические зависимости.** Проверка — `asmdef` reference graph.

### 3.3 Паттерны

*   **Service Locator (минимальный):** `ServiceLocator.Register<IPlatformProvider>(...)` в `Boot`. Доступ только через интерфейс. Не синглтон-зоопарк.
*   **Event Bus:** `EventBus.Publish<T>(T evt)` / `Subscribe`. События — `struct` без аллокаций. Список в `Docs/EVENTS.md`.
*   **State Machine:** `GameState = Boot -> Base -> Run -> Evac/Death -> Base`. Только `GameStateMachine` может переключать.
*   **ScriptableObject как конфиг:** баланс, блоки, биомы, апгрейды — SO. Не хардкод в `MonoBehaviour`.
*   **Object Pool:** для эффектов бурения, дропа ресурсов, врагов.

### 3.4 Code Style

*   C# 10, `.editorconfig` единый.
*   `namespace UnderDeep.{Module}` обязателен.
*   `public` только то, что часть API агента. Остальное `internal` / `[SerializeField] private`.
*   Никаких `FindObjectOfType`, `GameObject.Find`, `SendMessage` в рантайме. Инъекция через инспектор или `ServiceLocator`.
*   `Update()` без `new`, без LINQ-аллокаций. Для WebGL критично.
*   Комментарии к публичным методам — XML-doc на английском.
*   Коммиты: `feat(world): генерация биомов`, `fix(player): перегрев не сбрасывался`.

---

## 4. Контракты данных (не ломать без мажорной версии)

### 4.1 Мир

```csharp
enum BlockType : byte { Air=0, Dirt, Stone, RustMetal, CrystalOre, BioGel,
                        Artifact, Bedrock, GasVein, Wire, CrystalWall }

struct BlockData {
  BlockType type;
  byte hp;            // прочность (0 = воздух)
  byte variant;        // визуальный вариант
}

interface IDrillTarget {
  bool CanDrill(Vector2Int pos);
  DrillResult Drill(Vector2Int pos, float drillPower);
}

struct DrillResult { bool destroyed; BlockType type; int yieldAmount; }
```

*   Размер мира MVP: **50 x 100** тайлов (ширина x глубина). Расширяемо до 50 x 1000 для финала.
*   Координата: `x` 0..width-1, `y` 0..height-1 где `y=0` — поверхность, `y` растет вниз (глубина).
*   Тайлмап: 1 Unity Tilemap для коллизии + 1 для декора. WorldGrid — источник истины, Tilemap — вью.

### 4.2 Игрок (LONE-1)

```csharp
struct PlayerStats {
  float maxFuel;        // 100 + апгрейды
  float fuel;           // расход при бурении
  float heat;           // 0..100, перегрев при 100
  float health;         // 3 HP
  float sanity;         // 0..100, безумие с 500м
  int depth;            // y игрока
}

interface IInventory {
  int Get(ResourceType t);
  void Add(ResourceType t, int amount);
}

enum ResourceType : byte { RustMetal, Crystal, BioGel, Artifact }
```

*   Движение: сетка + сглаживание. Нельзя пройти в `BlockType != Air` без бурения.
*   Топливо: `-fuelPerDrill` за каждый тик бурения. При `fuel==0` скорость x0.5 и запрет бурения камня+.
*   Перегрев: `+heatPerSec` бурение, `-coolPerSec` покой. При 100 — стан 3с + 0.5 урона.

### 4.3 Апгрейды

```csharp
[CreateAssetMenu(menuName="UnderDeep/Upgrade")]
class UpgradeDef : ScriptableObject {
  string id;            // "drill_mk2"
  string displayName;
  Cost[] costs;         // [{RustMetal,50}]
  StatModifier[] mods;  // [{drillSpeed, +0.25}]
  string prereqId;      // null или id предыдущего
}
```

*   Постоянные между забегами. Хранятся в `MetaSave.purchasedUpgrades`.
*   Эффект применяется в `Boot`/`Base` при загрузке.

### 4.4 Сохранения

```csharp
struct MetaSave { // сохраняется всегда (PlayerPrefs / Yandex SDK)
  int version = 1;
  Dictionary<string,int> resources; // RustMetal, Crystal, BioGel
  HashSet<string> purchasedUpgrades;
  HashSet<string> artifacts;        // "artifact_1" .. "artifact_5"
  int maxDepthReached;
}

struct RunSave { // только в памяти забега, сбрасывается при смерти
  int seed;
  Dictionary<ResourceType,int> runInventory;
  int depth;
}
```

*   **Правило рогалика:** `RunSave` НЕ пишется на диск до эвакуации. Смерть = `runInventory` обнуляется.
*   `MetaSave` пишется через `ISaveService : SaveMeta(MetaSave)` который делегирует в `IPlatformProvider`.

### 4.5 События (EventBus)

| Событие | Поля | Публикует |
|---------|------|-----------|
| `BlockMined` | pos, type, yield | World |
| `FuelChanged` | current, max, delta | Player |
| `HeatChanged` | heat, overheated | Player |
| `HealthChanged` | hp, maxHp | Player |
| `SanityChanged` | sanity | Player |
| `RunEnded` | reason (Death/Evac), depth, loot | GameStateMachine |
| `UpgradePurchased` | id | Progression |
| `DepthReached` | depth, biome | Player |

Полный список — `Docs/EVENTS.md`.

---

## 5. Контракт Web-платформ

### 5.1 Абстракция

```csharp
interface IPlatformProvider {
  PlatformId Id { get; } // Yandex, VK, Standalone
  Task InitAsync();
  Task<PlayerProfile> AuthAsync(); // может быть гость
  Task ShowInterstitialAsync();    // межстраничка
  Task<bool> ShowRewardedAsync(string placement); // true = досмотрел
  Task SaveAsync(MetaSave save);
  Task<MetaSave> LoadAsync();
  Task SetLeaderboardAsync(string board, int score);
  void GameReady();       // Yandex: ready()
  void GameplayStart();
  void GameplayStop();
  string Language { get; } // ru/en/tr
}

enum PlatformId { Standalone, YandexGames, VKPlay }
```

*   Игра вызывает `GameReady()` только после `Boot` (когда сцена загружена). Иначе Яндекс не покажет игру.
*   `ShowRewardedAsync("revive")` и `"double_loot"` — два плейсмента, обязательных для монетизации.
*   В редакторе и Standalone билде используется `StandaloneProvider` (PlayerPrefs).

### 5.2 Требования Yandex Games

*   `Plugins/WebGL/YandexSDK.jslib` — обертка над `YaGames.init()`.
*   Лидерборды: `maxDepth` и `lootPerRun`.
*   Язык: `ysdk.environment.i18n.lang`.
*   Не вызывать `auth` без клика пользователя (правило Яндекса).
*   Build: WebGL, `Compression: Brotli`, `Code Optimization: Size`, `Exception Support: None` (или ExplicitlyOnly).

### 5.3 Требования VK

*   `Plugins/WebGL/VKBridge.jslib` — `vkBridge.send(...)`.
*   Реклама: нативная VK или заглушка.
*   Хранилище: `VK Storage` или `PlayerPrefs` fallback.

### 5.4 Общие требования WebGL

*   Нет `Application.Quit()`, нет синхронного файлового IO.
*   Пауза при `visibilitychange` (вкладка скрыта) — `Time.timeScale=0` + `GameplayStop()`.
*   Курсор: поддержка мыши + тач. Клик на тайл = бурить (рейкаст). WASD = движение.
*   Разрешение: 1920x1080 canvas, `matchWebGLToCanvasSize`, `devicePixelRatio` ограничен 1.5.

---

## 6. Рабочий процесс агентов

1.  **Перед работой:** прочитай этот контракт + `Docs/ARCHITECTURE.md` + `Docs/BALANCE.md`.
2.  **Ветка:** все работают в `arena/6db305ea-underdeep`. Прямой пуш разрешен (Arena). Для внешних — PR.
3.  **Задача:** заведи запись в `Docs/TASKS.md` (`- [ ] feat(world): биом кристаллов`) или возьми существующую.
4.  **Реализация:**
    *   Код в своей папке, тесты в `Tests/`.
    *   Публичные API — через `Core` интерфейсы, не прямые ссылки.
    *   Баланс — только через SO, не хардкод.
5.  **Самопроверка:**
    *   `EditMode` тесты зеленые.
    *   WebGL билд собирается (хотя бы `Build Settings` без ошибок).
    *   Нет новых `asmdef` циклов.
6.  **Документация:** обнови `Docs/CHANGELOG.md` + `EVENTS.md` если добавил событие.
7.  **Ревью:** тегни владельца папки. Architekt проверяет контракт.

### 6.1 Запреты

*   Не удалять/переименовывать `Assets/_Project/Core/` API без ADR.
*   Не коммитить `Library/`, `Temp/`, `Logs/`, `.vs/`, `obj/`.
*   Не коммитить большие бинарники (>10 МБ) без LFS.
*   Не хардкодить цены апгрейдов — только `UpgradeDef`.
*   Не вызывать `PlayerPrefs` напрямую — только через `ISaveService`.

---

## 7. Чек-листы

### 7.1 Definition of Done (фича)

*   [ ] Код в нужной папке, `asmdef` зависимости корректны
*   [ ] SO конфиги созданы, баланс в `BALANCE.md`
*   [ ] EventBus события задокументированы
*   [ ] Тест EditMode/PlayMode покрывает happy path
*   [ ] Работает в StandaloneProvider (без Яндекса)
*   [ ] Нет аллокаций в Update (проверка Profiler)
*   [ ] Документация обновлена

### 7.2 WebGL Release Checklist

*   [ ] `GameReady()` вызывается после загрузки Boot
*   [ ] Interstitial не чаще 1 раза в 3 минуты
*   [ ] Rewarded дает награду только при `success=true`
*   [ ] Сохранение через `IPlatformProvider` работает
*   [ ] Лидерборд отправляет `maxDepth`
*   [ ] Пауза на `visibilitychange`
*   [ ] Локализация ru/en (минимум)

---

## 8. ADR (Architecture Decision Records)

| № | Дата | Решение | Обоснование |
|---|------|---------|-------------|
| 1 | 2026-10-19 | Tilemap + WorldGrid как источник истины | Проще для WebGL, чем DOTS/ECS |
| 2 | 2026-10-19 | ServiceLocator вместо Zenject | Меньше веса сборки для Web |
| 3 | 2026-10-19 | EventBus struct вместо C# event | Zero-GC для WebGL |
| 4 | 2026-10-19 | StandaloneProvider fallback | Игра должна работать без SDK |

---

## 9. Контакты

*   Владелец контракта: **Architect Agent**
*   Вопросы по балансу: **Progression Agent**
*   Вопросы по SDK: **Platform Agent**

> Нарушение этого контракта = откат PR. Давай делать Deep Lone вместе, но по правилам.

