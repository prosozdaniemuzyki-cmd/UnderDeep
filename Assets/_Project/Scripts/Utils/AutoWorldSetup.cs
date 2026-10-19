using UnityEngine;
using UnityEngine.Tilemaps;
using UnderDeep.World;
using UnderDeep.Core;

namespace UnderDeep.Utils
{
    /// <summary>Helper to auto-setup WorldGrid in scene if Tilemap missing (for quick prototype).</summary>
    public class AutoWorldSetup : MonoBehaviour
    {
        public Grid grid;
        public Tilemap tilemap;
        public WorldGrid world;

        void Awake()
        {
            if (world==null) world=FindFirstObjectByType<WorldGrid>();
            if (grid==null) grid=FindFirstObjectByType<Grid>();
            if (grid==null) { var g=new GameObject("Grid"); grid=g.AddComponent<Grid>(); grid.cellSize=new Vector3(1,1,0); }
            if (tilemap==null) tilemap=FindFirstObjectByType<Tilemap>();
            if (tilemap==null)
            {
                var tmGO=new GameObject("Tilemap");
                tmGO.transform.SetParent(grid.transform);
                tilemap=tmGO.AddComponent<Tilemap>();
                tmGO.AddComponent<TilemapRenderer>();
                tmGO.AddComponent<TilemapCollider2D>();
                tmGO.AddComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Static;
                tmGO.AddComponent<CompositeCollider2D>();
                tilemap.GetComponent<TilemapCollider2D>().usedByComposite=true;
            }
            if (world) world.tilemap=tilemap;
        }
    }
}
