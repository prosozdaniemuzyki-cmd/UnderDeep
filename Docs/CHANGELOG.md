# Changelog

## 1.6.0 — 2026-10-20 — gameplay v2: auto-drill, surface shaft, gravity, fuel blocks, gamepad, animation base

### Добавлено
- Управление: авто-бурение в 8-направлении ввода (WASD/стрелки/левый стик геймпада, мёртвая зона 0.2), клик/тап остаётся. Отдельной кнопки бурения нет.
- Мир: поверхность `SurfaceCrust` небуримая наверху (`y=height-1..height-2`), шахта 2×6 вниз от `width/2`, спавн дрона `y=height-2` (depth=1), `Depth=(height-1)-y` растёт вниз, `Bedrock` внизу `y<=1`, проваливание гравитацией (18→10) при бурении вниз, бонус бурения вниз ×1.2.
- Топливо: блоки `FuelCanister (+25)` / `EnergyCell (+15)` кластерами Perlin — единственный способ пополнения, убран обмен кристаллов (`R`). Выбор лут vs топливо.
- Геймпад: `InputReader` (Move + Dash `Shift/B` + Shield `Q/LT` + Evac `Start`), 8-way квантование, deadZone 0.2, кривая стика.
- Способности: `AbilitySystem` — рывок 2 тайла + i-frame 0.3с (8 топлива/25 тепла, кд 1.5с), щит удержанием 6 топлива/с.
- Анимации: `DroneAnimator` база (Idle/Move/DrillSide/DrillDown/Fall/Damage/Death/Evacuate) через `DroneAnimationRequested` + события `PlayerFalling/Landed/AbilityTriggered/FuelCanisterCollected`.
- Core: новые `BlockType` SurfaceCrust/FuelCanister/EnergyCell, события анимации/падения/способностей.
- Bootstrap: шахта, камера `minY 0`, игрок с `InputReader/AbilitySystem/DroneAnimator/PlayerLight`, HUD без кнопки пополнения.
- Доки: `GAMEPLAY_V2.md`, `BALANCE v0.2`, `VERTICAL_SLICE` обновлён, тесты `WorldTests` под FuelCanister.
- Предложение 3 вариантов взаимодействия — реализованы Dash+Shield, прыжок отложен.

### Проверено
- `git diff --check`, синтаксический скан; Unity PlayMode/TestRunner требуют ручной проверки (авто-бурение, гравитация, топливные блоки, геймпад).

## 1.5.0 — 2026-10-20 — timed fuel rescue and evacuation economy

### Изменено
- Обычная эвакуация теперь стоит 10 топлива; кристаллы не являются её дешёвой заменой.
- Когда топлива остаётся меньше 10 (цены обычной эвакуации), запускается настраиваемое 10-секундное окно: B/кнопка спасения расходует 20 мета-кристаллов и сохраняет лут; без оплаты дроид гарантированно погибает.
- Пополнение за run-кристалл (+15 топлива) отключено при запасе ниже стоимости обычной эвакуации и после нуля топлива, чтобы не превращать 1 кристалл в дешёвый выкуп.
- Смерть теряет весь run-лут, включая найденные артефакты; интерфейс показывает нули. Добавлены тесты платежа/резерва и потери лута.
- В бэклог добавлена переработка ржавого металла в сталь.

### Проверка
- `git diff --check` и синтаксический скан C#; Unity Play Mode и Test Runner требуют проверки в редакторе.

## 1.4.0 — 2026-10-20 — fuel recovery and safe evacuation

### Добавлено
- Пополнение топлива за кристалл забега: клавиша `R` или кнопка HUD расходует 1 Crystal и даёт до 15 топлива.
- Эвакуация теперь принимает кристалл текущего забега до расходования мета-кристалла.
- Аварийный возврат, когда не хватает топлива и кристаллов: возвращает на базу с потерей run-лута, предотвращая софтлок.
- HUD показывает остатки ресурсов и доступность пополнения/обоих вариантов эвакуации; добавлен EditMode-тест на обмен/оплату эвакуации кристаллами.
- После смерти или аварийного возврата HUD явно обнуляет отображение потерянного run-лута.

### Проверка
- Ручной smoke-тест пользователя пройден: база → забег → бурение → добыча лута → эвакуация; обмен кристалла на топливо также работает.
- Пользователь проверил смерть от перегрева: RunEnded=Death, лут не сохранён, кнопка возврата работает. Отображение нулевого лута после потери добавлено вследствие проверки и требует повторного визуального подтверждения.
- Проверки Unity Test Runner отдельно не подтверждены; Unity Editor в среде подготовки отсутствует.

## 1.3.0 — 2026-10-19 — vertical slice wiring (Unity validation pending)

### Добавлено
- `VerticalSliceBootstrap`: временно связывает Boot, базу и сцену забега; добавляет минимальные UI, блоки, шахту и дроида при загрузке сцен.
- Рабочий по коду путь эвакуации/смерти с сохранением или потерей лута забега; добавлен event-driven HUD с глубиной, ресурсами, топливом, теплом и здоровьем.
- EditMode/PlayMode test assemblies; тесты перенесены под `Assets/Tests`, CI запускает оба режима отдельной матрицей.
- `Docs/VERTICAL_SLICE.md` с критерием среза и инструкциями ручной проверки.

### Исправлено
- `WorldGrid` допускает создание компонента до назначения block definitions и работает при пустом списке биомов.
- Дрон появляется у прохода, камера следует за ним по горизонтали и вертикали, изменение глубины публикуется только при смене ряда.
- Перегрев больше не охлаждается одновременно с бурением; смерть обрабатывается один раз, результат забега сохраняет глубину.
- README и Quickstart приведены к текущему статусу прототипа, убраны утверждения о Full MVP.

### Проверка
- JSON asmdef-файлов валиден; `git diff --check` проходит.
- Unity Editor в среде подготовки отсутствует: импорт, компиляция Unity, Test Runner, Play Mode и WebGL билд не запускались. Это остаётся обязательным следующим шагом.

## 1.2.1 — 2026-10-19 — asmdef audit

### Исправлено
- Проект снова компилируется: устранены все CS0234/CS0246/CS0103, а также ошибки, которые всплыли бы следующей волной (CS0191, CS0426, CS0117, CS0138 и синтаксические CS1056/CS1003).
- Убраны все запрещённые зависимости между сборками (граф — `AGENTS_CONTRACT.md` §3.2):
  - **World → Player**: `TrapSystem` (+ `GasPoison`) и `CaveInSystem` (+ `FallingRock`) перенесены в `Scripts/Utils/` (namespace `UnderDeep.Utils`).
  - **Player → Enemies**: `SanityEffects` перенесён в `Scripts/Utils/`.
  - **Player → Progression**: хоткей эвакуации (B) в `DrillSystem` публикует `EvacRequested`, на него подписан `RunResultHandler` (было `FindFirstObjectByType<RunResultHandler>()`).
  - **Progression → World**: `InfiniteMode` вынесен из `EndingManager.cs` в `Scripts/Utils/InfiniteMode.cs`.
- `TrapTriggered` / `TrapType` перенесены из World в `Core/EventBus.cs`. Событие создаётся через конструктор (раньше был инициализатор у readonly struct → CS0191). `CaveInSystem` больше не обращается к ним как к вложенным типам `TrapSystem` (CS0426).
- `VFXManager`: удалён неверный `using UnderDeep.World.TrapSystem;` (CS0138). `VFXManager` и `ScreenShake` подписаны на `TrapTriggered` из Core.
- `DrillSystem`: добавлен `using UnityEngine.InputSystem;` (CS0103 `Mouse`/`Keyboard`).
- Синтаксис: экранированные кавычки внутри интерполяции в `ArtifactManager.cs:29` и `BaseShopUI.cs:39`.
- asmdef — добавлены только ссылки на пакеты:
  - `UnderDeep.Player` → `Unity.InputSystem`, `Unity.RenderPipelines.Universal.2D.Runtime` (в URP 17 `Light2D` лежит в отдельной 2D-сборке);
  - `UnderDeep.UI` → `UnityEngine.UI`, `Unity.TextMeshPro`.
  
  Обратных и циклических ссылок нет, у `UnderDeep.Core` по-прежнему `"references": []`.
- `CaveInSystem` / `FallingRock`: `GetComponent<HealthSystem>()?.TakeDamage()` заменён на `TryGetComponent`, потому что `?.` обходит Unity null-check.

### Добавлено
- Core: события `TrapTriggered`, `EvacRequested`, enum `TrapType` (изменение аддитивное, существующий API не тронут).
- Контракт: §3.2 синхронизирован с фактическим графом сборок, обновлён §4.5, добавлен ADR-5.

### Проверено
- Core не ссылается ни на кого, World — только на Core, Player — на Core + World, Progression — на Core + Player, Platform — на Core.
- Компиляция Roslyn (C# 9) с эмуляцией asmdef-графа Unity 6000.3: Editor (WebGL / Standalone) и Player WebGL — 0 ошибок. EditMode `WorldTests` — 4/4.

### Известно
- `Tests/` лежит вне `Assets/`, поэтому Unity эти тесты не компилирует (Test Runner покажет 0 тестов). Чтобы их запустить, нужны тестовые asmdef — требуется согласование контракта.

## 1.2.0 — 2026-10-19 — WebGL билд-пайплайн для Yandex/VK

### Добавлено
- **WebGLBuilder**: 3 цели (Yandex в `Build/Yandex`, VK в `Build/VK`, Standalone в `Build/Standalone`), автонастройка PlayerSettings (Brotli, 960×600, Wasm, no logo)
- **PostBuildHook**: копирует icon→thumbnail, пишет yandex-manifest.json, проверяет размер <30 MB
- **Шаблоны**: `Assets/WebGLTemplates/YandexGames/index.html` (YSdk v2, прогресс-бар, LoadingAPI.ready, visibilitychange) и `VKPlay/index.html` (vk-bridge)
- **Иконки**: icon_512.png, icon_120.png + promo_banner/biomes_preview для стора
- **Доки сборки**: `Docs/Build/YANDEX_CHECKLIST.md`, `WEBGL_OPTIMIZATION.md`, `STORE_LISTING.md`, `README.md`
- Обновлены TASKS (билд помечен done)

## 1.1.0 — 2026-10-19 — Полный MVP: свет, все враги, ловушки, артефакты

### Добавлено
- **Враги**: FireflyGuardian (орбита+отбрасывание), CrystalSpider (засада из кристаллов), Hallucination (сквозь стены, +санити)
- EnemySpawner теперь по биому (Metal→Worm, Bio→Firefly, Crystal→Spider, Infection→Hallucination)
- **Ловушки**: TrapSystem (газ — яд 3с/тик, провода — стан 0.6с), CaveInSystem (обвалы, FallingRock), Tilemap sync
- **Свет**: PlayerLight (URP Point Light2D, flicker, влияние на безумие, радар +40% радиуса)
- **Безумие**: SanitySystem переписан (bright снижает набор, deep ×1.5), SanityEffects (vignette, хроматика, фейк-руды, фейк-враги, blackout 5с, шёпоты раз в 12с)
- **Артефакты**: ArtifactManager 5/5 с перками, EndingManager 2 финала (жертва/побег) + InfiniteMode
- **VFX/Audio**: VFXManager (частицы бурения/взрыва, дрожь), AudioManager (loop/ваншоты), ScreenShake, RadarVision, BootWebGLHooks (пауза вкладки)
- WorldGrid: кластеры через Perlin, жилы RustMetal, улучшенная генерация
- PlayerInventory: Consume(), DrillSystem: E (био-гель) / B (эвак), HealthSystem Heal()
- Доки: VFX_LIGHTING.md, YANDEX_INTEGRATION.md, обновлены TASKS/BALANCE/SCENES
- Арт: drone_concept.png (пиксель-арт дрона)

### Исправлено
- HealthSystem щит теперь 1 раз и только от урона (не от хила), добавлен Heal()
- Sanity Panic корректно ресторится через Invoke, SanityEffects Blackout фиксит Invoke
- PlayerInventory поддерживает отрицательный Add для расхода био-геля

## 1.0.0 — 2026-10-19 — MVP Контракт + Прототип
- Контракт агентов v1.0 + архитектура/баланс/платформа/события
- Core, World 50×100, Player, Enemies (Worm), Progression 6 апгрейдов, UI, Platform, Scenes, тесты
