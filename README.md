# UnderDeep — Deep Lone

**2D Drill Roguelite про маленького гусеничного дрона LONE-1.** 
Двигатель: Unity 6 (6000.3.7f1) URP 2D. Платформы: WebGL — Яндекс Игры, VK Play, Standalone.

> 🎮 Логлайн: Ты — последний буровой дроид на заброшенной колонии Кора-7. Добудь ресурсы, обменивай на апгрейды на базе и спустись к ядру (1000м), где тебя ждут голоса в голове и два финала.

![Unity 6000.3](https://img.shields.io/badge/Unity-6000.3-black)
![Platform WebGL](https://img.shields.io/badge/WebGL-Yandex%20%7C%20VK-blue)
![License MIT](https://img.shields.io/badge/license-MIT-green)

---

## Быстрый старт

1. Открой проект в **Unity 6000.3.7f1** (URP 2D template).
2. Сцены в Build Settings по порядку:
   - `Assets/_Project/Scenes/Boot.unity` — вход, инициализация SDK
   - `Assets/_Project/Scenes/Base.unity` — база/магазин
   - `Assets/_Project/Scenes/Run.unity` — забег (шахта 50x100)
3. Нажми Play на `Boot`. В редакторе работает `StandaloneProvider` (PlayerPrefs).
4. Для генерации дефолтных SO: `UnderDeep → Create Default Upgrades / Biomes`.

### Управление

| Действие | Клавиатура | Мышь/Тач |
|----------|------------|----------|
| Движение | WASD / стрелки | — |
| Бурить | Space (в сторону взгляда) | Клик по соседнему тайлу |
| Эвакуация | Кнопка ЭВАКУАЦИЯ | — |
| Био-гель (лечение+санити) | E | — |

---

## Контракт агентов

Главный документ: **[Docs/AGENTS_CONTRACT.md](Docs/AGENTS_CONTRACT.md)**

Коротко:
- 9 ролей (Architect, World, Player, Enemies, Progression, UI, Platform, TechArt, QA) — каждая владеет своей папкой.
- Все платформенные вызовы только через `IPlatformProvider` (Yandex/VK/Standalone).
- Мир: `WorldGrid` — источник истины, `Tilemap` — вью.
- Сохранения: `MetaSave` (постоянное) + `RunSave` (в памяти, сбрасывается при смерти).
- События через `EventBus` (struct, zero-GC).

Другие доки:
- [ARCHITECTURE.md](Docs/ARCHITECTURE.md) — диаграммы, поток бурения
- [BALANCE.md](Docs/BALANCE.md) — числа добычи, биомы, цены
- [PLATFORM.md](Docs/PLATFORM.md) — Yandex SDK / VK Bridge / WebGL требования
- [EVENTS.md](Docs/EVENTS.md) — все ивенты

---

## Структура проекта

```
Assets/_Project/
├── Core/           EventBus, GameStateMachine, ServiceLocator, SaveService
├── Scripts/
│   ├── World/      WorldGrid, BiomeDef, BlockDef, генерация
│   ├── Player/     DroneController, DrillSystem, Fuel/Heat/Sanity/Health, Inventory
│   ├── Enemies/    EnemyBase, RustWorm, Spawner
│   ├── Progression/UpgradeSystem, RunResultHandler
│   ├── UI/         HUD, BaseShop, DeathScreen
│   └── Platform/   IPlatformProvider, Yandex/VK/Standalone
├── ScriptableObjects/Blocks, Biomes, Upgrades
├── Scenes/         Boot, Base, Run
└── Plugins/WebGL/  YandexSDK.jslib, VKBridge.jslib
```

---

## Платформенный слой (важно для Web)

```csharp
IPlatformProvider provider = PlatformFactory.Create();
await provider.InitAsync();
provider.GameReady(); // вызывать ПОСЛЕ загрузки Boot, иначе Яндекс не пустит
await provider.ShowRewardedAsync("revive"); // 1 раз/забег
await provider.ShowRewardedAsync("double_loot");
await provider.SetLeaderboardAsync("maxDepth", depth);
```

- В редакторе — заглушка `StandaloneProvider` (PlayerPrefs, реклама = true).
- В билде WebGL — автодетект по `Application.absoluteURL`.
- Сохранение <4 КБ JSON, подходит под лимит Yandex Storage.

Сборка WebGL: `Brotli`, `Code Opt: Size`, `Exceptions: None`, `Decompression Fallback: true`.

---

## MVP Чек-лист

- [x] Генерация мира 50x100, 6 биомов, детерминизм по seed
- [x] Дрон: движение по сетке, бурение кликом/Space, топливо, перегрев
- [x] 3+ типа блоков (Dirt/Stone/Crystal/BioGas/Wire/Bedrock)
- [x] Сбор ресурсов, инвентарь забега + мета
- [x] Враг: RustWorm (уничтожает блоки, движется по прямой)
- [x] Эвакуация на базу с сохранением лута / смерть с потерей
- [x] 6 апгрейдов (MK2/MK3, батареи, радар, магнит, щит)
- [x] HUD + магазин + экран смерти с Rewarded revive
- [x] Платформенный слой + jslib для Яндекса/VK

Дальше: Светляк/Паук/Галлюцинация, газовые жилы, обвалы, провода, шкала безумия до 100%, 5 артефактов + финалы + бесконечный режим.

---

## Тесты

- `Tests/EditMode/WorldTests.cs` — биомы, баланс, сохранения
- `Tests/PlayMode/DrillIntegrationTests.cs` — генерация поверхности

Запуск: `Window → Analysis → Test Runner`.

---

## Лицензия

MIT — делай что хочешь, но укажи авторов.

---

## Обратная связь

Нашёл баг в бурении? Сначала проверь `Docs/AGENTS_CONTRACT.md` → заведи задачу в `Docs/TASKS.md`.
