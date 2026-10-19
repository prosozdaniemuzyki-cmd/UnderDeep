# Задачи — бэклог

> Отмеченные ранее игровые системы описывают наличие кода, но не подтверждают, что они подключены к сценам или проверены в Unity. Для статуса vertical slice см. нижние пункты и `VERTICAL_SLICE.md`.

- [x] Контракт агентов (Architect) — v1.0
- [x] Архитектура + баланс + платформа доки
- [x] feat(core): ServiceLocator, EventBus, GameStateMachine, SaveService, Boot, Pool, Audio, VFX
- [x] feat(world): WorldGrid 50×100, BlockDef/BiomeDef, генерация кластерами/перлин, Tilemap sync, TrapSystem (газ/провода), CaveInSystem
- [x] feat(player): DroneController, DrillSystem, Fuel/Heat/Sanity/Health, Inventory, PlayerLight (Point Light2D), SanityEffects
- [x] feat(enemies): EnemyBase, RustWorm, FireflyGuardian, CrystalSpider, Hallucination, EnemySpawner по биому
- [x] feat(progression): UpgradeSystem 6 апгрейдов, ArtifactManager 5/5, EndingManager 2 финала + InfiniteMode
- [x] feat(ui): HUD (fuel/heat/sanity/whispers), BaseShop, Death/Evac, RadarVision, ScreenShake
- [x] feat(platform): IPlatformProvider, Yandex/VK/Standalone, jslib, BootWebGLHooks
- [x] feat(lighting): URP 2D Global+Point Light, VFX_MANAGER, AudioManager
- [x] feat(input): WASD + мышь/тач, Space/E/B; в vertical slice добавлен R для пополнения топлива
- [x] feat(scenes): Boot/Base/Run сцены + URP 2D свет + камера
- [x] test: исходные тесты World/Depth/MetaSave и генерации добавлены (до 1.3.0 не были обнаружимы Unity)
- [x] docs: VFX_LIGHTING, YANDEX_INTEGRATION, SCENES, QUICKSTART, BUILD
- [x] art: drone_concept, promo_banner, icon_512/120, biomes_preview
- [x] build: WebGLBuilder (3 цели), PostBuildHook, Yandex/VK templates (index.html), YANDEX_CHECKLIST/WEBGL_OPTIMIZATION/STORE_LISTING
- [x] fix(asmdef): аудит сборок 1.2.1 — убраны обратные зависимости World→Player, Player→Enemies/Progression, Progression→World; проект компилируется
- [x] vertical-slice: runtime-связка Boot → Base → Run; временные шахта, игрок, UI, магазин, эвакуация/смерть
- [x] vertical-slice: кристалл текущего забега меняется на топливо; аварийная эвакуация предотвращает софтлок
- [x] test: тесты перенесены в `Assets/Tests/` и добавлены EditMode/PlayMode asmdef
- [ ] vertical-slice: открыть в Unity 6000.3.7f1, проверить Play Mode и исправить ошибки импорта/сцен
- [ ] vertical-slice: проверить полный цикл добычи, сохранения эвакуации и потери лута при смерти в редакторе
- [ ] polish: заменить runtime-заготовки на сериализованные сцены/префабы, пиксель-арт тайлы 16×32, анимации дроида, SFX и UI
- [x] test: исходные тесты World/Depth/MetaSave и генерации перенесены в Unity test assemblies; запуск ещё не подтверждён
