using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using UnderDeep.Core;
using UnderDeep.World;
using UnderDeep.Player;

namespace UnderDeep.EditorTools
{
    /// <summary>
    /// Генератор префабов и ассетов для настройки тайлов/игрока в инспекторе.
    /// Запускается из меню UnderDeep/Generate Prefabs — создаёт префабы, BlockDef и Tile ассеты.
    /// </summary>
    public static class PrefabGenerator
    {
        const string PrefabFolder = "Assets/_Project/Prefabs";
        const string TilesFolder = "Assets/_Project/Prefabs/Tiles";
        const string BlocksFolder = "Assets/_Project/ScriptableObjects/Blocks";
        const string BiomesFolder = "Assets/_Project/ScriptableObjects/Biomes";

        [MenuItem("UnderDeep/Generate All Prefabs + Tiles")]
        public static void GenerateAll()
        {
            GenerateBlockDefs();
            GenerateTiles();
            GenerateWorldGridPrefab();
            GenerateDronePrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PrefabGenerator] All prefabs and tiles generated.");
        }

        [MenuItem("UnderDeep/Generate BlockDef Assets")]
        public static void GenerateBlockDefs()
        {
            EnsureFolder(BlocksFolder);
            // Если уже есть — не перезаписываем, только создаём недостающие
            var existing = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:BlockDef", new[] { BlocksFolder }))
                existing.Add(Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)));

            var defs = new (BlockType type, string name, string display, float hardness, ResourceType drop, int min, int max, Color color, bool canDrill, bool crust, float fuelReward, int hp)[]
            {
                (BlockType.Dirt, "Dirt", "Грунт", 0.45f, ResourceType.RustMetal, 1,1, new Color(0.48f,0.31f,0.18f), true, false, 0, 1),
                (BlockType.Stone, "Stone", "Камень", 0.8f, ResourceType.RustMetal, 1,1, new Color(0.38f,0.43f,0.45f), true, false, 0, 2),
                (BlockType.RustMetal, "RustMetal", "Ржавый металл", 0.55f, ResourceType.RustMetal, 2,2, new Color(0.72f,0.34f,0.16f), true, false, 0, 2),
                (BlockType.CrystalOre, "CrystalOre", "Кристалл", 0.8f, ResourceType.Crystal, 1,2, new Color(0.18f,0.79f,0.91f), true, false, 0, 3),
                (BlockType.BioGel, "BioGel", "Био-гель", 0.4f, ResourceType.BioGel, 1,1, new Color(0.31f,0.87f,0.42f), true, false, 0, 1),
                (BlockType.GasVein, "GasVein", "Газовая жила", 0.6f, ResourceType.RustMetal, 0,0, new Color(0.60f,0.81f,0.33f), true, false, 0, 2),
                (BlockType.Wire, "Wire", "Провод", 0.7f, ResourceType.RustMetal, 1,1, new Color(0.91f,0.66f,0.22f), true, false, 0, 3),
                (BlockType.CrystalWall, "CrystalWall", "Кристальная стена", 1.1f, ResourceType.Crystal, 2,2, new Color(0.32f,0.61f,0.96f), true, false, 0, 4),
                (BlockType.ArtifactChunk, "ArtifactChunk", "Фрагмент артефакта", 0.9f, ResourceType.Artifact, 0,0, new Color(0.84f,0.40f,0.89f), true, false, 0, 3),
                (BlockType.SurfaceCrust, "SurfaceCrust", "Корка поверхности", 999f, ResourceType.RustMetal, 0,0, new Color(0.22f,0.24f,0.28f), false, true, 0, 999),
                (BlockType.FuelCanister, "FuelCanister", "Топливная канистра", 0.5f, ResourceType.RustMetal, 0,0, new Color(0.95f,0.75f,0.15f), true, false, 25f, 1),
                (BlockType.EnergyCell, "EnergyCell", "Энергоячейка", 1.0f, ResourceType.Crystal, 0,0, new Color(0.20f,0.95f,0.45f), true, false, 15f, 1),
                (BlockType.Bedrock, "Bedrock", "Коренная порода", 999f, ResourceType.RustMetal, 0,0, new Color(0.13f,0.14f,0.17f), false, false, 0, 999),
            };

            foreach (var d in defs)
            {
                if (existing.Contains(d.name)) continue;
                var asset = ScriptableObject.CreateInstance<BlockDef>();
                asset.type = d.type;
                asset.displayName = d.display;
                asset.hardness = d.hardness;
                asset.dropType = d.drop;
                asset.dropMin = d.min;
                asset.dropMax = d.max;
                asset.tileColor = d.color;
                asset.canDrill = d.canDrill;
                asset.isSurfaceCrust = d.crust;
                asset.fuelReward = d.fuelReward;
                asset.hp = d.hp;
                asset.name = d.name;
                string path = $"{BlocksFolder}/{d.name}.asset";
                AssetDatabase.CreateAsset(asset, path);
                Debug.Log($"[PrefabGenerator] Created BlockDef {path}");
            }
        }

        [MenuItem("UnderDeep/Generate Tile Assets")]
        public static void GenerateTiles()
        {
            EnsureFolder(TilesFolder);
            var blockDefs = LoadAllBlockDefs();
            if (blockDefs.Count == 0)
            {
                Debug.LogWarning("[PrefabGenerator] No BlockDefs found, run Generate BlockDefs first.");
                return;
            }

            // Создаём белый пиксель-спрайт если нет
            string spritePath = "Assets/_Project/Prefabs/Tiles/WhitePixel.png";
            Sprite whiteSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (whiteSprite == null)
            {
                // Создаём текстуру 16x16 белую
                var tex = new Texture2D(16,16, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                var pixels = new Color[256];
                for(int i=0;i<pixels.Length;i++) pixels[i]=Color.white;
                tex.SetPixels(pixels);
                tex.Apply();
                byte[] png = tex.EncodeToPNG();
                File.WriteAllBytes(spritePath, png);
                AssetDatabase.ImportAsset(spritePath);
                var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
                if(importer!=null){ importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit=16; importer.filterMode=FilterMode.Point; importer.SaveAndReimport(); }
                whiteSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            }

            foreach(var def in blockDefs)
            {
                string tilePath = $"{TilesFolder}/{def.type}Tile.asset";
                if (AssetDatabase.LoadAssetAtPath<Tile>(tilePath) != null) continue;
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = def.type + "Tile";
                tile.sprite = whiteSprite;
                tile.color = def.tileColor;
                tile.colliderType = Tile.ColliderType.None;
                AssetDatabase.CreateAsset(tile, tilePath);
                Debug.Log($"[PrefabGenerator] Created Tile {tilePath}");
            }
        }

        static List<BlockDef> LoadAllBlockDefs()
        {
            var list = new List<BlockDef>();
            foreach(var guid in AssetDatabase.FindAssets("t:BlockDef"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<BlockDef>(path);
                if(asset!=null) list.Add(asset);
            }
            return list;
        }

        [MenuItem("UnderDeep/Generate WorldGrid Prefab")]
        public static void GenerateWorldGridPrefab()
        {
            EnsureFolder(PrefabFolder);
            string prefabPath = $"{PrefabFolder}/WorldGrid.prefab";

            // Загружаем дефы и биомы для префаба
            var blockDefs = LoadAllBlockDefs();
            var biomes = new List<BiomeDef>();
            foreach(var guid in AssetDatabase.FindAssets("t:BiomeDef"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var b = AssetDatabase.LoadAssetAtPath<BiomeDef>(path);
                if(b!=null) biomes.Add(b);
            }
            // Загружаем тайлы
            var tileMap = new Dictionary<BlockType, TileBase>();
            foreach (var guid in AssetDatabase.FindAssets("t:Tile", new[] { TilesFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
                var name = Path.GetFileNameWithoutExtension(path); // DirtTile
                foreach (BlockType bt in System.Enum.GetValues(typeof(BlockType)))
                    if (name.StartsWith(bt.ToString())) tileMap[bt]=tile;
            }

            // Создаём временный объект для префаба
            var root = new GameObject("WorldGrid");
            root.AddComponent<Grid>().cellSize = new Vector3(1,1,0);
            var world = root.AddComponent<WorldGrid>();
            world.width = 50;
            world.height = 100;
            world.seed = 127;
            world.surfaceHeight = 2;
            world.blockDefs = blockDefs;
            world.biomes = biomes;
            // Назначаем тайлы если есть
            if(tileMap.TryGetValue(BlockType.Dirt, out var t)) world.dirtTile = t;
            if(tileMap.TryGetValue(BlockType.Stone, out t)) world.stoneTile = t;
            if(tileMap.TryGetValue(BlockType.RustMetal, out t)) world.rustTile = t;
            if(tileMap.TryGetValue(BlockType.CrystalOre, out t)) world.crystalTile = t;
            if(tileMap.TryGetValue(BlockType.BioGel, out t)) world.bioTile = t;
            if(tileMap.TryGetValue(BlockType.Bedrock, out t)) world.bedrockTile = t;
            if(tileMap.TryGetValue(BlockType.GasVein, out t)) world.gasTile = t;
            if(tileMap.TryGetValue(BlockType.Wire, out t)) world.wireTile = t;
            if(tileMap.TryGetValue(BlockType.CrystalWall, out t)) world.crystalWallTile = t;
            if(tileMap.TryGetValue(BlockType.SurfaceCrust, out t)) world.surfaceCrustTile = t;
            if(tileMap.TryGetValue(BlockType.FuelCanister, out t)) world.fuelCanisterTile = t;
            if(tileMap.TryGetValue(BlockType.EnergyCell, out t)) world.energyCellTile = t;

            // Дочерний Tilemap
            var ground = new GameObject("Ground");
            ground.transform.SetParent(root.transform, false);
            var tilemap = ground.AddComponent<Tilemap>();
            tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0);
            ground.AddComponent<TilemapRenderer>().sortingOrder = 0;
            world.tilemap = tilemap;

            // Сохраняем префаб
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"[PrefabGenerator] Saved {prefabPath} with {blockDefs.Count} blockDefs, {biomes.Count} biomes", prefab);
            Object.DestroyImmediate(root);
        }

        [MenuItem("UnderDeep/Generate Drone Prefab")]
        public static void GenerateDronePrefab()
        {
            EnsureFolder(PrefabFolder);
            string prefabPath = $"{PrefabFolder}/Drone.prefab";

            var go = new GameObject("Drone");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.35f, 0.86f, 0.62f);
            sr.sortingOrder = 5;
            // спрайт назначит пользователь или оставим белый пиксель
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.8f);

            var drone = go.AddComponent<DroneController>();
            drone.moveSpeed = 5f;
            drone.gravity = 18f;
            drone.terminalVelocity = 10f;
            drone.bodyRenderer = sr;

            // Детский глаз
            var eye = new GameObject("EyeDisplay");
            eye.transform.SetParent(go.transform, false);
            eye.transform.localPosition = new Vector3(0.13f, 0.08f, -0.1f);
            eye.transform.localScale = new Vector3(0.28f, 0.28f, 1f);
            var eyeSr = eye.AddComponent<SpriteRenderer>();
            eyeSr.color = new Color(0.1f, 0.98f, 0.72f);
            eyeSr.sortingOrder = 6;
            drone.eyeDisplay = eye.transform;

            // Остальные системы — добавит Bootstrap если отсутствуют, но добавляем для удобства инспектора
            go.AddComponent<InputReader>();
            var fuel = go.AddComponent<FuelSystem>();
            fuel.maxFuel = 100f;
            fuel.fuelPerCanister = 25f;
            fuel.fuelPerEnergyCell = 15f;
            go.AddComponent<OverheatSystem>();
            go.AddComponent<HealthSystem>();
            go.AddComponent<PlayerInventory>();
            go.AddComponent<SanitySystem>();
            go.AddComponent<DrillSystem>();
            var ability = go.AddComponent<AbilitySystem>();
            ability.dashDistance = 2f;
            ability.dashFuelCost = 8f;
            var animator = go.AddComponent<DroneAnimator>();
            animator.bodyRenderer = sr;
            animator.eyeDisplay = eye.transform;
            go.AddComponent<PlayerLight>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Debug.Log($"[PrefabGenerator] Saved {prefabPath}", prefab);
            Object.DestroyImmediate(go);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace("\\","/");
            string leaf = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
