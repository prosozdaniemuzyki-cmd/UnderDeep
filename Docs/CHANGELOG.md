# Changelog

## 1.0.0 — 2026-10-19 — MVP Контракт + Прототип

### Добавлено
- Контракт агентов `Docs/AGENTS_CONTRACT.md` (версия 1.0)
- Архитектура `ARCHITECTURE.md`, баланс `BALANCE.md`, платформа `PLATFORM.md`, события `EVENTS.md`
- Core: ServiceLocator, EventBus, GameStateMachine, SaveService
- World: WorldGrid (50x100), генерация биомов, Tilemap sync
- Player: DroneController, DrillSystem, Fuel/Heat/Sanity/Health, Inventory
- Enemies: EnemyBase, RustWorm с движением по тайлам
- Progression: UpgradeSystem (6 апгрейдов), RunResult, MetaSave
- UI: HUD, BaseShop, Death/Evac
- Platform: IPlatformProvider + Yandex/VK/Standalone + jslib
- Scenes: Boot, Base, Run
- Тесты: EditMode базовые

