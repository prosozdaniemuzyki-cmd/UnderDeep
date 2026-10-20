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
- [x] vertical-slice: кристалл текущего забега меняется на топливо; добавлен таймер топливного кризиса и платное спасение за 20 мета-кристаллов
- [x] test: тесты перенесены в `Assets/Tests/` и добавлены EditMode/PlayMode asmdef
- [x] vertical-slice: пользователь подтвердил базовый smoke-тест (база → забег → бурение → лут → эвакуация), обмен кристалла на топливо и смерть от перегрева с потерей лута/возвратом
- [x] gameplay v2: авто-бурение в 8-направлении ввода, поверхность-корка SurfaceCrust небуримая, шахта 2×6, спавн на поверхности y=1
- [x] gameplay v2: гравитация 18/терминал 10, проваливание при бурении вниз, бонус бурения вниз ×1.2
- [x] gameplay v2: топливные блоки FuelCanister (+25) / EnergyCell (+15) вместо обмена кристаллов, выбор лут vs топливо
- [x] gameplay v2: база под анимации DroneAnimator (Idle/Move/DrillSide/DrillDown/Fall/Damage/Death/Evacuate) + события
- [x] gameplay v2: InputReader (WASD + геймпад leftStick/Dpad, deadZone 0.2), Dash (Shift/B) 2 тайла + i-frame, Shield (Q/LT) 6 топлива/с
- [x] gameplay v2: AbilitySystem + DroneAnimator заглушки, геймпад Start эвак, тач/мышь остаётся
- [x] docs: GAMEPLAY_V2.md, BALANCE v0.2, VERTICAL_SLICE обновлён
- [ ] vertical-slice: проверить новый геймплей v2 в Unity (авто-бурение, гравитация, топливные блоки, геймпад, рывок/щит, корка)
- [ ] test: обновить WorldTests под FuelCanister (кристалл больше не даёт топливо)
- [ ] economy: переработка ржавого металла в сталь и цепочки переработки для будущих улучшений базы/магазина
- [ ] polish: заменить runtime-заготовки на сериализованные сцены/префабы, пиксель-арт тайлы 16×32, анимации дроида, SFX и UI
