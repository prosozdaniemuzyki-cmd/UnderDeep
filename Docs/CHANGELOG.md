# Changelog

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
