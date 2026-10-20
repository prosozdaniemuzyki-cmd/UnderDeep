using UnityEngine;
using UnityEngine.Tilemaps;
using UnderDeep.Core;
using System.Collections.Generic;

namespace UnderDeep.World
{
    public struct BlockData
    {
        public BlockType type;
        public int hp;
        public int variant;
    }

    public struct DrillResult { public bool destroyed; public BlockType type; public int yieldAmount; public ResourceType resource; }

    public interface IDrillTarget { bool CanDrill(Vector2Int pos); DrillResult DrillTick(Vector2Int pos,float drillPower,float dt); }

    public class WorldGrid : MonoBehaviour, IDrillTarget
    {
        [Header("Size")]
        public int width = 50;
        public int height = 100;
        public int seed = 127;

        [Header("Tilemap")]
        public Tilemap tilemap;
        public TileBase dirtTile, stoneTile, rustTile, crystalTile, bioTile, bedrockTile, gasTile, wireTile, crystalWallTile;
        public TileBase surfaceCrustTile, fuelCanisterTile, energyCellTile;

        [Header("Defs")]
        public List<BlockDef> blockDefs;
        public List<BiomeDef> biomes;

        private BlockData[,] _grid;
        private Dictionary<BlockType, BlockDef> _defMap;
        public int Seed => seed;

        void Awake()
        {
            RebuildDefMap();
            ServiceLocator.Register<WorldGrid>(this);
            ServiceLocator.Register<IDrillTarget>(this);
        }

        void Start() => Generate();

        void RebuildDefMap()
        {
            _defMap ??= new Dictionary<BlockType, BlockDef>();
            _defMap.Clear();
            if (blockDefs == null) return;
            foreach (var def in blockDefs)
            {
                if (def != null) _defMap[def.type] = def;
            }
        }

        [Header("Surface / Shaft (v2: дрон появляется сверху)")]
        public int surfaceHeight = 2; // 0..1 — небуримая корка вокруг шахты
        public Vector2Int ShaftOrigin => new(width/2, surfaceHeight); // центр шахты на поверхности

        public void Generate(int newSeed = -1)
        {
            if (newSeed != -1) seed = newSeed;
            RebuildDefMap();
            Random.InitState(seed);
            _grid = new BlockData[width, height];
            for(int y=0;y<height;y++) for(int x=0;x<width;x++)
            {
                var biome = GetBiome(y);
                _grid[x,y] = RollBlock(x,y,biome);
            }
            // v2: поверхность — небуримая корка, кроме шахты
            int sx = width/2;
            for(int y=0;y<surfaceHeight;y++)
                for(int x=0;x<width;x++)
                    _grid[x,y] = new BlockData{ type=BlockType.SurfaceCrust, hp=999 };
            // начатая шахта 2 тайла шириной, 6 в глубину (куда спускаться сразу)
            for(int y=0;y<6;y++)
            {
                _grid[sx,y]=new BlockData{ type=BlockType.Air };
                _grid[sx+1,y]=new BlockData{ type=BlockType.Air };
            }
            // низ — коренная порода уже в RollBlock
            SyncAllToTilemap();
            Debug.Log($"[World] Generated {width}x{height} seed={seed} shaft at x={sx}");
        }

        BiomeDef GetBiome(int depth)
        {
            if (biomes == null) return null;
            foreach (var b in biomes)
                if (b != null && depth >= b.minDepth && depth < b.maxDepth) return b;
            return biomes.Count > 0 ? biomes[0] : null;
        }

        BlockData RollBlock(int x,int y, BiomeDef biome)
        {
            if (y >= height-2) return new BlockData{ type=BlockType.Bedrock, hp=999 };
            float r = Random.value;
            if (biome==null) return new BlockData{type=BlockType.Dirt, hp=1};

            // топливные блоки — выбор вместо пополнения за кристаллы
            float fuelNoise = Mathf.PerlinNoise((x+seed)*0.09f, y*0.07f);
            if (fuelNoise > 0.88f && y>5)
            {
                if (Random.value < 0.35f) return new BlockData{ type=BlockType.FuelCanister, hp=1 };
                if (Random.value < 0.18f) return new BlockData{ type=BlockType.EnergyCell, hp=1 };
            }

            // кластеры кристаллов (шум)
            float n = Mathf.PerlinNoise(x*0.12f + seed*0.01f, y*0.08f) * 1.2f;
            if (n > 0.85f && biome.crystalChance>0) return new BlockData{type=BlockType.CrystalOre, hp=3};
            if (n > 0.88f && biome.id==BiomeId.Crystal) return new BlockData{type=BlockType.CrystalWall, hp=4};

            if (r < biome.gasChance) return new BlockData{type=BlockType.GasVein, hp=2};
            if (r < biome.gasChance + biome.wireChance) return new BlockData{type=BlockType.Wire, hp=3};
            if (r < biome.gasChance + biome.wireChance + biome.crystalChance) return new BlockData{type=BlockType.CrystalOre, hp=3};
            if (r < biome.gasChance + biome.wireChance + biome.crystalChance + biome.bioChance) return new BlockData{type=BlockType.BioGel, hp=1};
            if (r < biome.gasChance + biome.wireChance + biome.crystalChance + biome.bioChance + biome.stoneChance) return new BlockData{type=BlockType.Stone, hp=2};
            if (y>15 && y<90 && Random.value < 0.0015f) return new BlockData{type=BlockType.ArtifactChunk, hp=3};
            if (Mathf.PerlinNoise(x*0.2f, y*0.05f) > 0.82f) return new BlockData{type=BlockType.RustMetal, hp=2};
            return new BlockData{type=BlockType.Dirt, hp=1};
        }

        void SyncAllToTilemap()
        {
            if (tilemap==null) return;
            tilemap.ClearAllTiles();
            for(int y=0;y<height;y++) for(int x=0;x<width;x++)
            {
                var d = _grid[x,y];
                if (d.type==BlockType.Air) continue;
                tilemap.SetTile(new Vector3Int(x,y,0), TileFor(d.type));
            }
        }

        TileBase TileFor(BlockType t) => t switch
        {
            BlockType.Dirt => dirtTile,
            BlockType.Stone => stoneTile,
            BlockType.RustMetal => rustTile,
            BlockType.CrystalOre => crystalTile,
            BlockType.BioGel => bioTile,
            BlockType.Bedrock => bedrockTile,
            BlockType.SurfaceCrust => surfaceCrustTile != null ? surfaceCrustTile : bedrockTile,
            BlockType.FuelCanister => fuelCanisterTile != null ? fuelCanisterTile : crystalTile,
            BlockType.EnergyCell => energyCellTile != null ? energyCellTile : bioTile,
            BlockType.GasVein => gasTile,
            BlockType.Wire => wireTile,
            BlockType.CrystalWall => crystalWallTile,
            BlockType.ArtifactChunk => crystalTile,
            _ => dirtTile
        };

        public BlockData Get(Vector2Int p) => InBounds(p) ? _grid[p.x,p.y] : new BlockData{type=BlockType.Bedrock,hp=999};
        public bool InBounds(Vector2Int p)=> p.x>=0 && p.x < width && p.y>=0 && p.y < height;
        public bool IsAir(Vector2Int p)=> InBounds(p) && _grid[p.x,p.y].type==BlockType.Air;
        public bool CanPass(Vector2Int p)=> IsAir(p);

        public bool CanDrill(Vector2Int pos)
        {
            if (!InBounds(pos)) return false;
            var d = _grid[pos.x,pos.y];
            if (d.type==BlockType.Air || d.type==BlockType.Bedrock || d.type==BlockType.SurfaceCrust) return false;
            if (_defMap.TryGetValue(d.type, out var def)) return def.canDrill;
            // топливные блоки бурятся даже если нет дефа (дефолт)
            return d.type==BlockType.FuelCanister || d.type==BlockType.EnergyCell;
        }

        public bool IsSupported(Vector2Int p) => !IsAir(p + Vector2Int.down);

        // Called per tick by DrillSystem
        private readonly Dictionary<Vector2Int,float> _progress = new();
        public DrillResult DrillTick(Vector2Int pos,float drillPower,float dt)
        {
            if (!CanDrill(pos)) return default;
            var def = _defMap[_grid[pos.x,pos.y].type];
            float need = def.hardness / drillPower;
            if (!_progress.ContainsKey(pos)) _progress[pos]=0;
            _progress[pos]+= dt;
            float prog = _progress[pos]/need;
            EventBus.Publish(new BlockDamaged(pos, prog));
            if (_progress[pos] >= need)
            {
                _progress.Remove(pos);
                var destroyedType = _grid[pos.x,pos.y].type;
                var res = def.dropType;
                int yield = Random.Range(def.dropMin, def.dropMax+1);
                _grid[pos.x,pos.y] = new BlockData{ type=BlockType.Air };
                if (tilemap) tilemap.SetTile(new Vector3Int(pos.x,pos.y,0), null);
                EventBus.Publish(new BlockMined(pos, destroyedType, yield, res));
                // gas trap
                if (destroyedType==BlockType.GasVein) Debug.Log("[World] Gas vein triggered!");
                // cave-in check: if removing support
                CheckCaveIn(pos);
                return new DrillResult{ destroyed=true, type=destroyedType, yieldAmount=yield, resource=res };
            }
            return new DrillResult{ destroyed=false, type=def.type };
        }

        void CheckCaveIn(Vector2Int mined)
        {
            // simple: if block above is Stone with no side support, 5% chance fall (placeholder)
            var above = mined + Vector2Int.up;
            if (!InBounds(above)) return;
            if (_grid[above.x,above.y].type!=BlockType.Air && Random.value<0.03f)
            {
                // collapse: destroy above
                var t = _grid[above.x,above.y].type;
                _grid[above.x,above.y]=new BlockData{type=BlockType.Air};
                if (tilemap) tilemap.SetTile(new Vector3Int(above.x,above.y,0),null);
                Debug.Log($"[World] Cave-in at {above}");
            }
        }

        public Vector2Int WorldToGrid(Vector3 world) => new Vector2Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));
        public Vector3 GridToWorld(Vector2Int g) => new Vector3(g.x+0.5f, g.y+0.5f, 0);

        // for radar
        public List<Vector2Int> FindResources(ResourceType r,int max=20)
        {
            var res = new List<Vector2Int>();
            for(int y=0;y<height;y++) for(int x=0;x<width;x++)
            {
                var bd=_grid[x,y];
                if (bd.type==BlockType.CrystalOre && r==ResourceType.Crystal) res.Add(new Vector2Int(x,y));
                if (bd.type==BlockType.BioGel && r==ResourceType.BioGel) res.Add(new Vector2Int(x,y));
                if (res.Count>=max) return res;
            }
            return res;
        }
    }
}
