using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.World
{
    [CreateAssetMenu(menuName="UnderDeep/BlockDef")]
    public class BlockDef : ScriptableObject
    {
        public BlockType type;
        public string displayName;
        public int hp = 1;
        public float hardness = 1f; // seconds at drill 1.0
        public float fuelCostPerSec = 3f;
        public ResourceType dropType;
        public int dropMin = 1;
        public int dropMax = 1;
        public Color tileColor = Color.white;
        public bool solid = true;
        public bool canDrill = true;
        public Sprite sprite;
    }
}
