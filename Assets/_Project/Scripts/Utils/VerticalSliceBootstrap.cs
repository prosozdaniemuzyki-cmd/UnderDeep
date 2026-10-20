using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnderDeep.Core;
using UnderDeep.Player;
using UnderDeep.Progression;
using UnderDeep.Platform;
using UnderDeep.UI;
using UnderDeep.World;

namespace UnderDeep.Utils
{
    /// <summary>
    /// Temporary, explicit scene wiring for the first playable vertical slice.
    /// It creates the missing runtime scene objects until authored prefabs/scenes replace it.
    /// </summary>
    public static class VerticalSliceBootstrap
    {
        static Sprite _pixelSprite;

        public static void Configure(Scene scene)
        {
            if (!scene.IsValid()) return;
            if (scene.name == "Base") ConfigureBase(scene);
            else if (scene.name == "Run") ConfigureRun(scene);
        }

        static void ConfigureBase(Scene scene)
        {
            EnsureCoreServices();
            var canvas = Find<Canvas>(scene);
            if (canvas == null) canvas = CreateCanvas("BaseSliceCanvas", scene);
            EnsureEventSystem(scene);

            var shop = Find<BaseShopUI>(scene);
            if (shop == null)
            {
                var shopObject = new GameObject("BaseShop");
                SceneManager.MoveGameObjectToScene(shopObject, scene);
                shop = shopObject.AddComponent<BaseShopUI>();
            }

            var upgradeSystem = Find<UpgradeSystem>(scene);
            if (upgradeSystem == null)
            {
                var serviceObject = new GameObject("SliceUpgradeSystem");
                SceneManager.MoveGameObjectToScene(serviceObject, scene);
                upgradeSystem = serviceObject.AddComponent<UpgradeSystem>();
            }
            if (upgradeSystem.allUpgrades == null || upgradeSystem.allUpgrades.Count == 0)
                upgradeSystem.allUpgrades = CreateSliceUpgrades();

            var panel = EnsurePanel(canvas.transform, "BaseSlicePanel", new Color(0.025f, 0.055f, 0.07f, 0.92f));
            CreateText(panel, "Title", "UNDERDEEP  /  LONE-1", 28, new Vector2(0.06f, 0.84f), new Vector2(0.94f, 0.97f), TextAnchor.MiddleLeft);
            CreateText(panel, "Subtitle", "Подготовь дроида и спустись в шахту. Бурение: W + Space или клик по соседнему блоку.", 15,
                new Vector2(0.06f, 0.76f), new Vector2(0.94f, 0.84f), TextAnchor.MiddleLeft);

            var storedResources = Find<TMP_Text>(scene);
            if (storedResources != null) storedResources.gameObject.SetActive(false);
            var resourcesLabel = CreateText(panel, "Resources", "РЕСУРСЫ", 17,
                new Vector2(0.06f, 0.68f), new Vector2(0.94f, 0.75f), TextAnchor.MiddleLeft);
            shop.resourcesText = null;
            shop.resourcesLegacyText = resourcesLabel;

            var list = FindByName<Transform>(scene, "SliceUpgradeList");
            if (list == null)
            {
                var listObject = new GameObject("SliceUpgradeList", typeof(RectTransform), typeof(VerticalLayoutGroup));
                listObject.transform.SetParent(panel, false);
                list = listObject.transform;
                var rect = (RectTransform)list;
                rect.anchorMin = new Vector2(0.06f, 0.22f);
                rect.anchorMax = new Vector2(0.94f, 0.66f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                var layout = listObject.GetComponent<VerticalLayoutGroup>();
                layout.spacing = 7;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }
            shop.listRoot = list;
            shop.rowPrefab = null;

            var startButton = CreateButton(panel, "StartRunButton", "НАЧАТЬ ЗАБЕГ", 20,
                new Vector2(0.28f, 0.07f), new Vector2(0.72f, 0.17f));
            shop.startRunButton = startButton;
            // подсказка про управление на базе
            CreateText(panel, "ControlHint", "Управление: WASD/стрелки или геймпад левый стик — движение и авто-бурение. Shift/B — рывок, Q/LT — щит. Поверхность вокруг шахты не бурится.", 12,
                new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.06f), TextAnchor.MiddleCenter);
            shop.enabled = true;
        }

        static void ConfigureRun(Scene scene)
        {
            EnsureCoreServices();
            if (!ServiceLocator.TryGet<GameStateMachine>(out var stateMachine))
            {
                var stateObject = new GameObject("GameStateMachine");
                SceneManager.MoveGameObjectToScene(stateObject, scene);
                stateMachine = stateObject.AddComponent<GameStateMachine>();
            }
            stateMachine.SetState(GameState.Run);

            var world = Find<WorldGrid>(scene);
            if (world == null)
            {
                var gridObject = new GameObject("WorldGrid", typeof(Grid));
                SceneManager.MoveGameObjectToScene(gridObject, scene);
                var tilemapObject = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer));
                tilemapObject.transform.SetParent(gridObject.transform, false);
                var tilemap = tilemapObject.GetComponent<Tilemap>();
                ConfigureGridTiles(tilemap);
                world = gridObject.AddComponent<WorldGrid>();
                world.tilemap = tilemap;
            }
            else
            {
                // Префаб найден — добиваем недостающее (Grid/Tilemap) чтобы не падало без настроек
                if (world.GetComponent<Grid>() == null) world.gameObject.AddComponent<Grid>();
                if (world.tilemap == null)
                {
                    world.tilemap = world.GetComponentInChildren<Tilemap>(true);
                    if (world.tilemap == null)
                    {
                        var tilemapObject = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer));
                        tilemapObject.transform.SetParent(world.transform, false);
                        world.tilemap = tilemapObject.GetComponent<Tilemap>();
                    }
                }
                // Grid размер по умолчанию если не задан в префабе
                var gridComp = world.GetComponent<Grid>();
                if (gridComp != null && gridComp.cellSize == Vector3.zero) gridComp.cellSize = new Vector3(1,1,0);
            }
            // Prefab-respecting: не затирать значения из инспектора/префаба
            if (world.width <= 0) world.width = 50;
            if (world.height <= 0) world.height = 100;
            if (world.biomes == null || world.biomes.Count == 0)
                world.biomes = LoadBiomesOrCreateDefaults();
            if (world.blockDefs == null || world.blockDefs.Count == 0)
                world.blockDefs = LoadBlockDefsOrCreateDefaults();
            // Тайлы — если префаб уже настроен, не перетираем
            bool hasTiles = world.dirtTile != null || world.stoneTile != null || world.surfaceCrustTile != null;
            if (!hasTiles)
            {
                if (world.tilemap != null && _dirtTile == null) ConfigureGridTiles(world.tilemap);
                AssignTiles(world);
            }
            else
            {
                CacheTilesFromWorld(world);
            }
            if (ServiceLocator.TryGet<SaveService>(out var save) && save.Run != null && save.Run.seed != 0)
                world.seed = save.Run.seed;
            else if (world.seed == 0)
                world.seed = 127;

            var player = Find<DroneController>(scene);
            if (player == null)
            {
                var playerObject = new GameObject("LONE-1", typeof(Rigidbody2D), typeof(SpriteRenderer));
                SceneManager.MoveGameObjectToScene(playerObject, scene);
                var body = playerObject.GetComponent<SpriteRenderer>();
                body.sprite = PixelSprite;
                body.color = new Color(0.35f, 0.86f, 0.62f);
                body.sortingOrder = 5;
                var bodyCollider = playerObject.AddComponent<BoxCollider2D>();
                bodyCollider.size = new Vector2(0.8f, 0.8f);
                var rigidbody = playerObject.GetComponent<Rigidbody2D>();
                rigidbody.bodyType = RigidbodyType2D.Kinematic;
                rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
                player = playerObject.AddComponent<DroneController>();
                player.bodyRenderer = body;
                // v2: ввод, анимация, способности, свет
                var inputReader = playerObject.AddComponent<InputReader>();
                playerObject.AddComponent<FuelSystem>();
                playerObject.AddComponent<OverheatSystem>();
                playerObject.AddComponent<HealthSystem>();
                playerObject.AddComponent<PlayerInventory>();
                playerObject.AddComponent<SanitySystem>();
                playerObject.AddComponent<DrillSystem>();
                playerObject.AddComponent<AbilitySystem>();
                var animator = playerObject.AddComponent<DroneAnimator>();
                animator.bodyRenderer = body;
                // eye will be assigned after CreateEye
                CreateEye(playerObject.transform);
                var eye = playerObject.transform.Find("EyeDisplay");
                if (eye != null) animator.eyeDisplay = eye;
                // свет дрона
                playerObject.AddComponent<PlayerLight>();

                // лог для проверки геймпада
                if (UnityEngine.InputSystem.Gamepad.current != null)
                    Debug.Log("[VerticalSlice] Gamepad detected: " + UnityEngine.InputSystem.Gamepad.current.displayName);
            }
            else
            {
                // Префаб найден — добиваем недостающие компоненты (чтобы префаб мог быть минимальным)
                EnsurePlayerComponents(player.gameObject);
            }

            if (Camera.main != null)
            {
                var camera = Camera.main;
                camera.orthographic = true;
                camera.orthographicSize = 7f;
                // v2: камера на поверхности у шахты
                camera.transform.position = new Vector3(world.width * 0.5f, 1f, -10f);
                var follow = camera.GetComponent<CameraFollow>();
                if (follow == null) follow = camera.gameObject.AddComponent<CameraFollow>();
                follow.target = player.transform;
                follow.offset = new Vector2(0, 1f);
                follow.minY = 0f;
                follow.maxY = world.height - 3f;
                follow.smooth = 8f;
            }

            if (Find<RunResultHandler>(scene) == null)
            {
                var results = new GameObject("RunResultHandler");
                SceneManager.MoveGameObjectToScene(results, scene);
                results.AddComponent<RunResultHandler>();
            }

            ConfigureRunHud(scene);
        }

        static void ConfigureRunHud(Scene scene)
        {
            var canvas = Find<Canvas>(scene);
            if (canvas == null) canvas = CreateCanvas("RunSliceCanvas", scene);
            EnsureEventSystem(scene);

            var panel = EnsurePanel(canvas.transform, "RunSliceHUD", new Color(0.015f, 0.035f, 0.045f, 0.08f));
            var depth = CreateText(panel, "DepthText", "ГЛУБИНА 0", 20,
                new Vector2(0.03f, 0.86f), new Vector2(0.42f, 0.98f), TextAnchor.MiddleLeft);
            var resources = CreateText(panel, "LootText", "ЛУТ  —", 16,
                new Vector2(0.03f, 0.73f), new Vector2(0.42f, 0.85f), TextAnchor.MiddleLeft);
            var fuel = CreateText(panel, "FuelText", "ТОПЛИВО 100", 16,
                new Vector2(0.03f, 0.61f), new Vector2(0.42f, 0.72f), TextAnchor.MiddleLeft);
            var health = CreateText(panel, "HealthText", "СОСТОЯНИЕ 3/3", 16,
                new Vector2(0.03f, 0.49f), new Vector2(0.42f, 0.60f), TextAnchor.MiddleLeft);
            var heat = CreateText(panel, "HeatText", "ТЕПЛО 0%", 16,
                new Vector2(0.03f, 0.37f), new Vector2(0.42f, 0.48f), TextAnchor.MiddleLeft);
            var status = CreateText(panel, "StatusText", "Движение = авто-бурение в сторону ввода (WASD/стрелки/левый стик геймпада). ЛКМ/тап тоже бурит. B/Start — эвакуация 10 топлива. Ищи жёлтые топливные канистры! Shift/B — рывок, Q/LT — щит.",
                12, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.10f), TextAnchor.MiddleLeft);

            var evacButton = CreateButton(panel, "EvacuateButton", "ЭВАКУАЦИЯ · −10 ТОПЛИВА", 13,
                new Vector2(0.67f, 0.86f), new Vector2(0.97f, 0.96f));
            // v2: пополнения за кристаллы нет — топливо только блоками в шахте
            var emergencyButton = CreateButton(panel, "EmergencyEvacuateButton", "СПАСЕНИЕ · −20 МЕТА-КРИСТАЛЛОВ", 12,
                new Vector2(0.67f, 0.60f), new Vector2(0.97f, 0.70f));
            emergencyButton.gameObject.SetActive(false);
            var returnButton = CreateButton(panel, "ReturnToBaseButton", "НАЗАД НА БАЗУ", 16,
                new Vector2(0.67f, 0.47f), new Vector2(0.97f, 0.57f));
            returnButton.gameObject.SetActive(false);

            var hudObject = new GameObject("VerticalSliceRunHUD");
            hudObject.SetActive(false);
            hudObject.transform.SetParent(panel, false);
            var hud = hudObject.AddComponent<VerticalSliceRunHUD>();
            hud.depthText = depth;
            hud.lootText = resources;
            hud.fuelText = fuel;
            hud.healthText = health;
            hud.heatText = heat;
            hud.statusText = status;
            hud.evacuateButton = evacButton;
            hud.refuelButton = null; // v2: убран, топливо только блоками
            hud.emergencyButton = emergencyButton;
            hud.returnButton = returnButton;
            hudObject.SetActive(true);
        }

        static void EnsureCoreServices()
        {
            if (!ServiceLocator.TryGet<SaveService>(out _))
            {
                var platform = new StandaloneProvider();
                ServiceLocator.Register<IPlatformProvider>(platform);
                ServiceLocator.Register(new SaveService(platform, new MetaSave()));
            }
        }

        static List<UpgradeDef> CreateSliceUpgrades()
        {
            return new List<UpgradeDef>
            {
                CreateUpgrade("drill_mk2", "Бур MK2", "Бурит быстрее", ResourceType.RustMetal, 20, StatType.DrillSpeed, 0.25f),
                CreateUpgrade("battery_1", "Аккумулятор I", "+20 к запасу топлива", ResourceType.RustMetal, 15, StatType.MaxFuel, 20),
            };
        }

        static UpgradeDef CreateUpgrade(string id, string title, string description, ResourceType resource, int cost, StatType stat, float value)
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeDef>();
            upgrade.name = id;
            upgrade.id = id;
            upgrade.displayName = title;
            upgrade.description = description;
            upgrade.costs = new[] { new Cost { type = resource, amount = cost } };
            upgrade.mods = new[] { new StatMod { stat = stat, value = value } };
            return upgrade;
        }

        static List<BiomeDef> LoadBiomesOrCreateDefaults()
        {
            var loaded = Resources.LoadAll<BiomeDef>("");
            if (loaded != null && loaded.Length > 0) return new List<BiomeDef>(loaded);
            // также пробуем загрузить из ScriptableObjects/Biomes (прямой путь для префаб-настройки)
            var fromSO = new List<BiomeDef>();
            foreach (var b in Resources.LoadAll<BiomeDef>("_Project/ScriptableObjects/Biomes"))
                if (b != null) fromSO.Add(b);
            if (fromSO.Count > 0) return fromSO;
            return new List<BiomeDef>
            {
                CreateBiome(BiomeId.Surface, 0, 10, 0.10f, 0, 0, 0, 0),
                CreateBiome(BiomeId.Metal, 10, 20, 0.40f, 0.05f, 0, 0.04f, 0.02f),
                CreateBiome(BiomeId.BioCaves, 20, 40, 0.25f, 0.08f, 0.06f, 0, 0.02f),
                CreateBiome(BiomeId.Crystal, 40, 70, 0.20f, 0.15f, 0.03f, 0, 0.01f),
                CreateBiome(BiomeId.Infection, 70, 90, 0.30f, 0.10f, 0.02f, 0.01f, 0.05f),
                CreateBiome(BiomeId.Core, 90, 100, 0.10f, 0.02f, 0, 0, 0),
            };
        }

        static List<BlockDef> LoadBlockDefsOrCreateDefaults()
        {
            // Если дизайнер создал BlockDef ассеты в ScriptableObjects/Blocks — используем их
            var loaded = Resources.LoadAll<BlockDef>("");
            // Unity Resources.LoadAll ищет в Resources/, но наши SO лежат в Assets/_Project/ScriptableObjects — 
            // при генерации префабов они будут скопированы или добавлены в список вручную. Пробуем также Addressables путь.
            if (loaded != null && loaded.Length >= 5) return new List<BlockDef>(loaded);
            // fallback — создаём из кода как раньше (для vertical slice без ассетов)
            return CreateSliceBlockDefs();
        }

        static void CacheTilesFromWorld(WorldGrid world)
        {
            _dirtTile = world.dirtTile ?? _dirtTile;
            _stoneTile = world.stoneTile ?? _stoneTile;
            _rustTile = world.rustTile ?? _rustTile;
            _crystalTile = world.crystalTile ?? _crystalTile;
            _bioTile = world.bioTile ?? _bioTile;
            _bedrockTile = world.bedrockTile ?? _bedrockTile;
            _gasTile = world.gasTile ?? _gasTile;
            _wireTile = world.wireTile ?? _wireTile;
            _crystalWallTile = world.crystalWallTile ?? _crystalWallTile;
            _surfaceCrustTile = world.surfaceCrustTile ?? _surfaceCrustTile;
            _fuelCanisterTile = world.fuelCanisterTile ?? _fuelCanisterTile;
            _energyCellTile = world.energyCellTile ?? _energyCellTile;
        }

        static BiomeDef CreateBiome(BiomeId id, int min, int max, float stone, float crystal, float bio, float wire, float gas)
        {
            var biome = ScriptableObject.CreateInstance<BiomeDef>();
            biome.name = id.ToString();
            biome.id = id;
            biome.displayName = id.ToString();
            biome.minDepth = min;
            biome.maxDepth = max;
            biome.stoneChance = stone;
            biome.crystalChance = crystal;
            biome.bioChance = bio;
            biome.wireChance = wire;
            biome.gasChance = gas;
            return biome;
        }

        static List<BlockDef> CreateSliceBlockDefs()
        {
            return new List<BlockDef>
            {
                CreateBlock(BlockType.Dirt, "Грунт", 0.45f, ResourceType.RustMetal, 1, 1, new Color(0.48f, 0.31f, 0.18f)),
                CreateBlock(BlockType.Stone, "Камень", 0.8f, ResourceType.RustMetal, 1, 1, new Color(0.38f, 0.43f, 0.45f)),
                CreateBlock(BlockType.RustMetal, "Ржавый металл", 0.55f, ResourceType.RustMetal, 2, 2, new Color(0.72f, 0.34f, 0.16f)),
                CreateBlock(BlockType.CrystalOre, "Кристалл", 0.8f, ResourceType.Crystal, 1, 2, new Color(0.18f, 0.79f, 0.91f)),
                CreateBlock(BlockType.BioGel, "Био-гель", 0.4f, ResourceType.BioGel, 1, 1, new Color(0.31f, 0.87f, 0.42f)),
                CreateBlock(BlockType.GasVein, "Газовая жила", 0.6f, ResourceType.RustMetal, 0, 0, new Color(0.60f, 0.81f, 0.33f)),
                CreateBlock(BlockType.Wire, "Провод", 0.7f, ResourceType.RustMetal, 1, 1, new Color(0.91f, 0.66f, 0.22f)),
                CreateBlock(BlockType.CrystalWall, "Кристальная стена", 1.1f, ResourceType.Crystal, 2, 2, new Color(0.32f, 0.61f, 0.96f)),
                CreateBlock(BlockType.ArtifactChunk, "Фрагмент артефакта", 0.9f, ResourceType.Artifact, 0, 0, new Color(0.84f, 0.40f, 0.89f)),
                CreateBlock(BlockType.SurfaceCrust, "Корка поверхности", 999f, ResourceType.RustMetal, 0, 0, new Color(0.22f, 0.24f, 0.28f), canDrill:false),
                CreateBlock(BlockType.FuelCanister, "Топливная канистра", 0.5f, ResourceType.RustMetal, 0, 0, new Color(0.95f, 0.75f, 0.15f), fuelReward:25f),
                CreateBlock(BlockType.EnergyCell, "Энергоячейка", 1.0f, ResourceType.Crystal, 0, 0, new Color(0.20f, 0.95f, 0.45f), fuelReward:15f),
                CreateBlock(BlockType.Bedrock, "Коренная порода", 999f, ResourceType.RustMetal, 0, 0, new Color(0.13f, 0.14f, 0.17f)),
            };
        }

        static BlockDef CreateBlock(BlockType type, string title, float hardness, ResourceType drop, int min, int max, Color color, bool canDrill = true, float fuelReward = 0)
        {
            var def = ScriptableObject.CreateInstance<BlockDef>();
            def.name = type.ToString();
            def.type = type;
            def.displayName = title;
            def.hardness = hardness;
            def.dropType = drop;
            def.dropMin = min;
            def.dropMax = max;
            def.canDrill = canDrill && type != BlockType.Bedrock && type != BlockType.SurfaceCrust;
            def.isSurfaceCrust = type == BlockType.SurfaceCrust;
            def.fuelReward = fuelReward;
            def.tileColor = color;
            return def;
        }

        static void ConfigureGridTiles(Tilemap tilemap)
        {
            tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0);
            var renderer = tilemap.GetComponent<TilemapRenderer>();
            if (renderer != null) renderer.sortingOrder = 0;
            var defs = CreateSliceBlockDefs();
            foreach (var def in defs)
            {
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = def.type + "Tile";
                tile.sprite = PixelSprite;
                tile.color = def.tileColor;
                tile.colliderType = Tile.ColliderType.None;
                switch (def.type)
                {
                    case BlockType.Dirt: _dirtTile = tile; break;
                    case BlockType.Stone: _stoneTile = tile; break;
                    case BlockType.RustMetal: _rustTile = tile; break;
                    case BlockType.CrystalOre: _crystalTile = tile; break;
                    case BlockType.BioGel: _bioTile = tile; break;
                    case BlockType.GasVein: _gasTile = tile; break;
                    case BlockType.Wire: _wireTile = tile; break;
                    case BlockType.CrystalWall: _crystalWallTile = tile; break;
                    case BlockType.Bedrock: _bedrockTile = tile; break;
                    case BlockType.SurfaceCrust: _surfaceCrustTile = tile; break;
                    case BlockType.FuelCanister: _fuelCanisterTile = tile; break;
                    case BlockType.EnergyCell: _energyCellTile = tile; break;
                }
            }
        }

        static TileBase _dirtTile, _stoneTile, _rustTile, _crystalTile, _bioTile, _bedrockTile, _gasTile, _wireTile, _crystalWallTile, _surfaceCrustTile, _fuelCanisterTile, _energyCellTile;

        static void AssignTiles(WorldGrid world)
        {
            world.dirtTile = _dirtTile;
            world.stoneTile = _stoneTile;
            world.rustTile = _rustTile;
            world.crystalTile = _crystalTile;
            world.bioTile = _bioTile;
            world.bedrockTile = _bedrockTile;
            world.gasTile = _gasTile;
            world.wireTile = _wireTile;
            world.crystalWallTile = _crystalWallTile;
            world.surfaceCrustTile = _surfaceCrustTile;
            world.fuelCanisterTile = _fuelCanisterTile;
            world.energyCellTile = _energyCellTile;
        }

        static Sprite PixelSprite
        {
            get
            {
                if (_pixelSprite != null) return _pixelSprite;
                var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "VerticalSlicePixel"
                };
                var pixels = new Color[16 * 16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                texture.SetPixels(pixels);
                texture.Apply();
                _pixelSprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
                return _pixelSprite;
            }
        }

        static T Find<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }

        static T FindByName<T>(Scene scene, string objectName) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == objectName)
                {
                    var rootComponent = root.GetComponent<T>();
                    if (rootComponent != null) return rootComponent;
                }
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == objectName && child.TryGetComponent<T>(out var matchingComponent))
                        return matchingComponent;
                }
            }
            return null;
        }

        static Canvas CreateCanvas(string objectName, Scene scene)
        {
            var canvasObject = new GameObject(objectName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 600);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        static void EnsureEventSystem(Scene scene)
        {
            if (Find<EventSystem>(scene) != null) return;
            var eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(eventObject, scene);
            eventObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        static RectTransform EnsurePanel(Transform parent, string name, Color color)
        {
            var existing = parent.Find(name) as RectTransform;
            if (existing != null) return existing;
            var panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            var rect = (RectTransform)panelObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panelObject.GetComponent<Image>().color = color;
            panelObject.GetComponent<Image>().raycastTarget = false;
            return rect;
        }

        static Text CreateText(Transform parent, string name, string value, int size, Vector2 anchorMin, Vector2 anchorMax, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rect = (RectTransform)textObject.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(8, 2);
            rect.offsetMax = new Vector2(-8, -2);
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = new Color(0.76f, 0.93f, 0.85f);
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.text = value;
            return text;
        }

        static Button CreateButton(Transform parent, string name, string label, int fontSize, Vector2 anchorMin, Vector2 anchorMax)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.12f, 0.42f, 0.34f, 0.98f);
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.18f, 0.58f, 0.44f, 1);
            colors.pressedColor = new Color(0.08f, 0.28f, 0.24f, 1);
            button.colors = colors;
            var text = CreateText(buttonObject.transform, "Label", label, fontSize, Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
            text.color = Color.white;
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            return button;
        }

        static void CreateEye(Transform parent)
        {
            if (parent.Find("EyeDisplay") != null) return; // префаб уже содержит глаз
            var eyeObject = new GameObject("EyeDisplay", typeof(SpriteRenderer));
            eyeObject.transform.SetParent(parent, false);
            eyeObject.transform.localPosition = new Vector3(0.13f, 0.08f, -0.1f);
            eyeObject.transform.localScale = new Vector3(0.28f, 0.28f, 1);
            var eye = eyeObject.GetComponent<SpriteRenderer>();
            eye.sprite = PixelSprite;
            eye.color = new Color(0.1f, 0.98f, 0.72f);
            eye.sortingOrder = 6;
        }

        static void EnsurePlayerComponents(GameObject go)
        {
            // Добиваем недостающие компоненты, чтобы префаб мог быть минимальным (только Transform+DroneController)
            if (go.GetComponent<Rigidbody2D>() == null)
            {
                var rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            }
            if (go.GetComponent<BoxCollider2D>() == null)
            {
                var col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.8f, 0.8f);
            }
            if (go.GetComponent<SpriteRenderer>() == null)
            {
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = PixelSprite;
                sr.color = new Color(0.35f, 0.86f, 0.62f);
                sr.sortingOrder = 5;
            }
            if (go.GetComponent<DroneController>() == null) go.AddComponent<DroneController>();
            if (go.GetComponent<InputReader>() == null) go.AddComponent<InputReader>();
            if (go.GetComponent<FuelSystem>() == null) go.AddComponent<FuelSystem>();
            if (go.GetComponent<OverheatSystem>() == null) go.AddComponent<OverheatSystem>();
            if (go.GetComponent<HealthSystem>() == null) go.AddComponent<HealthSystem>();
            if (go.GetComponent<PlayerInventory>() == null) go.AddComponent<PlayerInventory>();
            if (go.GetComponent<SanitySystem>() == null) go.AddComponent<SanitySystem>();
            if (go.GetComponent<DrillSystem>() == null) go.AddComponent<DrillSystem>();
            if (go.GetComponent<AbilitySystem>() == null) go.AddComponent<AbilitySystem>();
            if (go.GetComponent<DroneAnimator>() == null)
            {
                var anim = go.AddComponent<DroneAnimator>();
                anim.bodyRenderer = go.GetComponent<SpriteRenderer>();
                var eye = go.transform.Find("EyeDisplay");
                if (eye != null) anim.eyeDisplay = eye;
            }
            if (go.GetComponent<PlayerLight>() == null) go.AddComponent<PlayerLight>();
            // глаз
            CreateEye(go.transform);
            // связи аниматора
            var da = go.GetComponent<DroneAnimator>();
            if (da != null)
            {
                if (da.bodyRenderer == null) da.bodyRenderer = go.GetComponent<SpriteRenderer>();
                if (da.eyeDisplay == null)
                {
                    var eye = go.transform.Find("EyeDisplay");
                    if (eye != null) da.eyeDisplay = eye;
                }
            }
            var dc = go.GetComponent<DroneController>();
            if (dc != null)
            {
                if (dc.bodyRenderer == null) dc.bodyRenderer = go.GetComponent<SpriteRenderer>();
                if (dc.eyeDisplay == null)
                {
                    var eye = go.transform.Find("EyeDisplay");
                    if (eye != null) dc.eyeDisplay = eye;
                }
            }
        }
    }

    /// <summary>Event-driven HUD for the first playable run.</summary>
    public sealed class VerticalSliceRunHUD : MonoBehaviour
    {
        const string ControlHint = "WASD — движение  •  W + Space — бурить вверх  •  R — кристалл в топливо  •  B — эвакуация за 10 топлива";

        public Text depthText;
        public Text lootText;
        public Text fuelText;
        public Text healthText;
        public Text heatText;
        public Text statusText;
        public Button evacuateButton;
        public Button refuelButton;
        public Button emergencyButton;
        public Button returnButton;

        void OnEnable()
        {
            EventBus.Subscribe<FuelChanged>(OnFuel);
            EventBus.Subscribe<FuelRescueCountdownChanged>(OnFuelRescueCountdown);
            EventBus.Subscribe<ResourceCollected>(OnLoot);
            EventBus.Subscribe<DepthChanged>(OnDepth);
            EventBus.Subscribe<HealthChanged>(OnHealth);
            EventBus.Subscribe<HeatChanged>(OnHeat);
            EventBus.Subscribe<RunEnded>(OnRunEnded);
            if (evacuateButton != null) evacuateButton.onClick.AddListener(OnEvacuate);
            if (refuelButton != null) refuelButton.onClick.AddListener(OnRefuel);
            if (emergencyButton != null) emergencyButton.onClick.AddListener(OnEmergencyEvacuate);
            if (returnButton != null) returnButton.onClick.AddListener(OnReturn);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<FuelChanged>(OnFuel);
            EventBus.Unsubscribe<FuelRescueCountdownChanged>(OnFuelRescueCountdown);
            EventBus.Unsubscribe<ResourceCollected>(OnLoot);
            EventBus.Unsubscribe<DepthChanged>(OnDepth);
            EventBus.Unsubscribe<HealthChanged>(OnHealth);
            EventBus.Unsubscribe<HeatChanged>(OnHeat);
            EventBus.Unsubscribe<RunEnded>(OnRunEnded);
            if (evacuateButton != null) evacuateButton.onClick.RemoveListener(OnEvacuate);
            if (refuelButton != null) refuelButton.onClick.RemoveListener(OnRefuel);
            if (emergencyButton != null) emergencyButton.onClick.RemoveListener(OnEmergencyEvacuate);
            if (returnButton != null) returnButton.onClick.RemoveListener(OnReturn);
        }

        void Start()
        {
            if (ServiceLocator.TryGet<FuelSystem>(out var fuel)) OnFuel(new FuelChanged(fuel.Current, fuel.maxFuel, 0));
            if (ServiceLocator.TryGet<HealthSystem>(out var health)) OnHealth(new HealthChanged(health.hp, health.maxHp));
            if (ServiceLocator.TryGet<DroneController>(out var drone))
            {
                OnDepth(new DepthChanged(drone.Depth, 0));
                if (drone.TryGetComponent<OverheatSystem>(out var overheat)) OnHeat(new HeatChanged(overheat.Heat, overheat.IsStunned));
            }
            if (ServiceLocator.TryGet<PlayerInventory>(out var inventory))
                OnLoot(new ResourceCollected(ResourceType.RustMetal, 0, inventory.Get(ResourceType.RustMetal)));
            UpdateActionButtons();
            UpdateRescueButtonLabel();
        }

        void OnFuel(FuelChanged evt)
        {
            fuelText.text = $"ТОПЛИВО  {evt.current:0}/{evt.max:0}";
            bool countdownActive = ServiceLocator.TryGet<RunResultHandler>(out var results) && results.FuelCountdownActive;
            if (evt.current > 0 && evt.current < 10 && !countdownActive)
                statusText.text = "МАЛО ТОПЛИВА: обычная эвакуация требует 10. Критический резерв нельзя пополнить за run-кристалл.";
            else if (evt.current >= 10 && statusText.text.StartsWith("МАЛО ТОПЛИВА:"))
                statusText.text = ControlHint;
            UpdateActionButtons();
        }

        void OnFuelRescueCountdown(FuelRescueCountdownChanged evt)
        {
            if (!evt.active) return;
            int cost = GetRescueCrystalCost();
            statusText.text = $"ТОПЛИВО НИЗКО: {evt.secondsRemaining} сек. B/кнопка — спасение за {cost} мета-кристаллов; иначе смерть и потеря лута.";
            if (emergencyButton != null)
            {
                emergencyButton.gameObject.SetActive(true);
                emergencyButton.interactable = ServiceLocator.TryGet<FuelSystem>(out var fuel) && fuel.CanPayDepletedRescue;
                UpdateRescueButtonLabel();
            }
        }

        int GetRescueCrystalCost() => ServiceLocator.TryGet<FuelSystem>(out var fuel) ? fuel.depletedRescueCrystalCost : 20;

        void UpdateRescueButtonLabel()
        {
            if (emergencyButton == null) return;
            var label = emergencyButton.GetComponentInChildren<Text>(true);
            int balance = ServiceLocator.TryGet<SaveService>(out var save) ? save.Meta.crystal : 0;
            if (label != null) label.text = $"СПАСЕНИЕ · −{GetRescueCrystalCost()} МЕТА ({balance}/{GetRescueCrystalCost()})";
        }

        void OnLoot(ResourceCollected evt)
        {
            if (ServiceLocator.TryGet<PlayerInventory>(out var inventory))
                lootText.text = $"ЛУТ  М:{inventory.Get(ResourceType.RustMetal)}  К:{inventory.Get(ResourceType.Crystal)}  Б:{inventory.Get(ResourceType.BioGel)}";
            UpdateActionButtons();
        }

        void OnDepth(DepthChanged evt) => depthText.text = $"ГЛУБИНА  {evt.depth} м";
        void OnHealth(HealthChanged evt) => healthText.text = $"СОСТОЯНИЕ  {evt.hp}/{evt.maxHp}";
        void OnHeat(HeatChanged evt) => heatText.text = evt.overheated ? "ТЕПЛО  ПЕРЕГРЕВ" : $"ТЕПЛО  {evt.heat:0}%";

        void UpdateActionButtons()
        {
            if (!ServiceLocator.TryGet<FuelSystem>(out var fuel)) return;
            if (evacuateButton != null) evacuateButton.interactable = fuel.CanPayEvac;
            // Keep clickable below max capacity to explain the low-fuel reserve or missing crystal.
            if (refuelButton != null) refuelButton.interactable = fuel.Current >= fuel.evacCost && fuel.Current < fuel.maxFuel - 0.01f;
            if (emergencyButton != null)
            {
                bool countdownActive = ServiceLocator.TryGet<RunResultHandler>(out var results) && results.FuelCountdownActive;
                emergencyButton.gameObject.SetActive(countdownActive);
                emergencyButton.interactable = countdownActive && fuel.CanPayDepletedRescue;
            }
        }

        void OnEvacuate()
        {
            if (ServiceLocator.TryGet<RunResultHandler>(out var results)) results.Evacuate();
        }

        void OnRefuel()
        {
            // v2: пополнение только топливными блоками в шахте (жёлтые канистры/энергоячейки), кристаллы больше не конвертятся
            statusText.text = "ТОПЛИВО — ТОЛЬКО В ШАХТЕ: ИЩИ ЖЁЛТЫЕ КАНИСТРЫ (+25) И ЗЕЛЁНЫЕ ЯЧЕЙКИ (+15). ВЫБИРАЙ: ЛУТ ИЛИ ТОПЛИВО.";
        }

        void OnEmergencyEvacuate()
        {
            if (ServiceLocator.TryGet<RunResultHandler>(out var results)) results.EmergencyEvacuate();
        }

        void OnRunEnded(RunEnded evt)
        {
            if (evacuateButton != null) evacuateButton.interactable = false;
            if (refuelButton != null) refuelButton.interactable = false;
            if (emergencyButton != null) emergencyButton.gameObject.SetActive(false);
            if (returnButton != null) returnButton.gameObject.SetActive(true);
            if (evt.reason == RunEndReason.Death)
            {
                lootText.text = "ЛУТ  М:0  К:0  Б:0";
                statusText.text = $"ДРОИД ПОТЕРЯН НА ГЛУБИНЕ {evt.depth} м — ВЕСЬ ЛУТ ЗАБЕГА УТРАЧЕН";
            }
            else if (evt.reason == RunEndReason.EmergencyEvac)
            {
                lootText.text = "ЛУТ СОХРАНЁН НА БАЗЕ";
                statusText.text = $"СПАСЕНИЕ ОПЛАЧЕНО: {GetRescueCrystalCost()} МЕТА-КРИСТАЛЛОВ — ЛУТ СОХРАНЁН";
            }
            else
            {
                lootText.text = "ЛУТ СОХРАНЁН НА БАЗЕ";
                statusText.text = "ЭВАКУАЦИЯ ОПЛАЧЕНА 10 ТОПЛИВА — РЕСУРСЫ НА БАЗЕ";
            }
        }

        void OnReturn()
        {
            Time.timeScale = 1f;
            if (ServiceLocator.TryGet<GameStateMachine>(out var state)) state.SetState(GameState.Base);
            SceneManager.LoadScene("Base");
        }
    }
}
