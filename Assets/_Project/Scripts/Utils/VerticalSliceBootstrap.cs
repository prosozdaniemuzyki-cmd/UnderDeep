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
            world.width = 50;
            world.height = 100;
            world.biomes = LoadBiomesOrCreateDefaults();
            world.blockDefs = CreateSliceBlockDefs();
            if (world.tilemap != null && _dirtTile == null) ConfigureGridTiles(world.tilemap);
            AssignTiles(world);
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
                playerObject.AddComponent<FuelSystem>();
                playerObject.AddComponent<OverheatSystem>();
                playerObject.AddComponent<HealthSystem>();
                playerObject.AddComponent<PlayerInventory>();
                playerObject.AddComponent<SanitySystem>();
                playerObject.AddComponent<DrillSystem>();
                CreateEye(playerObject.transform);
            }

            if (Camera.main != null)
            {
                var camera = Camera.main;
                camera.orthographic = true;
                camera.orthographicSize = 7f;
                camera.transform.position = new Vector3(world.width * 0.5f, 3f, -10f);
                var follow = camera.GetComponent<CameraFollow>();
                if (follow == null) follow = camera.gameObject.AddComponent<CameraFollow>();
                follow.target = player.transform;
                follow.offset = Vector2.zero;
                follow.minY = 3f;
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
            var status = CreateText(panel, "StatusText", "WASD — движение  •  W + Space — бурить вверх  •  B — эвакуация",
                14, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.10f), TextAnchor.MiddleLeft);

            var evacButton = CreateButton(panel, "EvacuateButton", "ЭВАКУАЦИЯ", 16,
                new Vector2(0.75f, 0.86f), new Vector2(0.97f, 0.96f));
            var returnButton = CreateButton(panel, "ReturnToBaseButton", "НАЗАД НА БАЗУ", 16,
                new Vector2(0.75f, 0.73f), new Vector2(0.97f, 0.83f));
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
                CreateBlock(BlockType.Bedrock, "Коренная порода", 999f, ResourceType.RustMetal, 0, 0, new Color(0.13f, 0.14f, 0.17f)),
            };
        }

        static BlockDef CreateBlock(BlockType type, string title, float hardness, ResourceType drop, int min, int max, Color color)
        {
            var def = ScriptableObject.CreateInstance<BlockDef>();
            def.name = type.ToString();
            def.type = type;
            def.displayName = title;
            def.hardness = hardness;
            def.dropType = drop;
            def.dropMin = min;
            def.dropMax = max;
            def.canDrill = type != BlockType.Bedrock;
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
                }
            }
        }

        static TileBase _dirtTile, _stoneTile, _rustTile, _crystalTile, _bioTile, _bedrockTile, _gasTile, _wireTile, _crystalWallTile;

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
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
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
            var eyeObject = new GameObject("EyeDisplay", typeof(SpriteRenderer));
            eyeObject.transform.SetParent(parent, false);
            eyeObject.transform.localPosition = new Vector3(0.13f, 0.08f, -0.1f);
            eyeObject.transform.localScale = new Vector3(0.28f, 0.28f, 1);
            var eye = eyeObject.GetComponent<SpriteRenderer>();
            eye.sprite = PixelSprite;
            eye.color = new Color(0.1f, 0.98f, 0.72f);
            eye.sortingOrder = 6;
        }
    }

    /// <summary>Event-driven HUD for the first playable run.</summary>
    public sealed class VerticalSliceRunHUD : MonoBehaviour
    {
        public Text depthText;
        public Text lootText;
        public Text fuelText;
        public Text healthText;
        public Text heatText;
        public Text statusText;
        public Button evacuateButton;
        public Button returnButton;

        void OnEnable()
        {
            EventBus.Subscribe<FuelChanged>(OnFuel);
            EventBus.Subscribe<ResourceCollected>(OnLoot);
            EventBus.Subscribe<DepthChanged>(OnDepth);
            EventBus.Subscribe<HealthChanged>(OnHealth);
            EventBus.Subscribe<HeatChanged>(OnHeat);
            EventBus.Subscribe<RunEnded>(OnRunEnded);
            if (evacuateButton != null) evacuateButton.onClick.AddListener(OnEvacuate);
            if (returnButton != null) returnButton.onClick.AddListener(OnReturn);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<FuelChanged>(OnFuel);
            EventBus.Unsubscribe<ResourceCollected>(OnLoot);
            EventBus.Unsubscribe<DepthChanged>(OnDepth);
            EventBus.Unsubscribe<HealthChanged>(OnHealth);
            EventBus.Unsubscribe<HeatChanged>(OnHeat);
            EventBus.Unsubscribe<RunEnded>(OnRunEnded);
            if (evacuateButton != null) evacuateButton.onClick.RemoveListener(OnEvacuate);
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
            {
                foreach (var pair in inventory.RunLoot)
                    OnLoot(new ResourceCollected(pair.Key, pair.Value, pair.Value));
            }
        }

        void OnFuel(FuelChanged evt) => fuelText.text = $"ТОПЛИВО  {evt.current:0}/{evt.max:0}";
        void OnLoot(ResourceCollected evt) => lootText.text = $"ЛУТ  {evt.type}: {evt.totalRun}";
        void OnDepth(DepthChanged evt) => depthText.text = $"ГЛУБИНА  {evt.depth} м";
        void OnHealth(HealthChanged evt) => healthText.text = $"СОСТОЯНИЕ  {evt.hp}/{evt.maxHp}";
        void OnHeat(HeatChanged evt) => heatText.text = evt.overheated ? "ТЕПЛО  ПЕРЕГРЕВ" : $"ТЕПЛО  {evt.heat:0}%";

        void OnEvacuate()
        {
            if (ServiceLocator.TryGet<RunResultHandler>(out var results)) results.Evacuate();
        }

        void OnRunEnded(RunEnded evt)
        {
            evacuateButton.interactable = false;
            returnButton.gameObject.SetActive(true);
            statusText.text = evt.reason == RunEndReason.Death
                ? $"ДРОИД ПОТЕРЯН НА ГЛУБИНЕ {evt.depth} м — ЛУТ ЗАБЕГА УТРАЧЕН"
                : "ЭВАКУАЦИЯ — РЕСУРСЫ ДОБАВЛЕНЫ НА БАЗЕ";
        }

        void OnReturn()
        {
            Time.timeScale = 1f;
            if (ServiceLocator.TryGet<GameStateMachine>(out var state)) state.SetState(GameState.Base);
            SceneManager.LoadScene("Base");
        }
    }
}
