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

        [Header("Defs")]
        public List<BlockDef> blockDefs;
        public List<BiomeDef> biomes;

        private BlockData[,] _grid;
        private Dictionary<BlockType, BlockDef> _defMap;
        public int Seed => seed;

        void Awake()
        {
            _defMap = new Dictionary<BlockType, BlockDef>();
            foreach (var d in blockDefs) _defMap[d.type]=d;
            ServiceLocator.Register<WorldGrid>(this);
            ServiceLocator.Register<IDrillTarget>(this);
        }

        void Start() => Generate();

        public void Generate(int newSeed = -1)
        {
            if (newSeed != -1) seed = newSeed;
            Random.InitState(seed);
            _grid = new BlockData[width, height];
            for(int y=0;y<height;y++) for(int x=0;x<width;x++)
            {
                var biome = GetBiome(y);
                _grid[x,y] = RollBlock(x,y,biome);
            }
            // surface air, base corridor
            for(int y=0;y<3;y++) for(int x=0;x<width;x++) _grid[x,y]=new BlockData{ type=BlockType.Air };
            // spawn point
            int sx = width/2;
            for(int y=0;y<6;y++) _grid[sx,y]=new BlockData{ type=BlockType.Air };
            for(int y=0;y<6;y++) _grid[sx+1,y]=new BlockData{ type=BlockType.Air };
            SyncAllToTilemap();
            Debug.Log($"[World] Generated {width}x{height} seed={seed}");
        }

        BiomeDef GetBiome(int depth)
        {
            foreach(var b in biomes) if(depth>=b.minDepth && depth<b.maxDepth) return b;
            return biomes.Count>0?biomes[0]:null;
        }

        BlockData RollBlock(int x,int y, BiomeDef biome)
        {
            if (y >= height-2) return new BlockData{ type=BlockType.Bedrock, hp=999 };
            float r = Random.value;
            if (biome==null) return new BlockData{type=BlockType.Dirt, hp=1};

            // order by rarity
            if (r < biome.gasChance) return new BlockData{type=BlockType.GasVein, hp=2};
            if (r < biome.gasChance + biome.wireChance) return new BlockData{type=BlockType.Wire, hp=3};
            if (r < biome.gasChance + biome.wireChance + biome.crystalChance) return new BlockData{type=BlockType.CrystalOre, hp=3};
            if (r < biome.gasChance + biome.wireChance + biome.crystalChance + biome.bioChance) return new BlockData{type=BlockType.BioGel, hp=1};
            if (r < biome.gasChance + biome.wireChance + biome.crystalChance + biome.bioChance + biome.stoneChance) return new BlockData{type=BlockType.Stone, hp=2};
            // artifact deep
            if (y>70 && Random.value < 0.002f) return new BlockData{type=BlockType.ArtifactChunk, hp=2};
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
            if (d.type==BlockType.Air || d.type==BlockType.Bedrock) return false;
            return _defMap.TryGetValue(d.type, out var def) && def.canDrill;
        }

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
