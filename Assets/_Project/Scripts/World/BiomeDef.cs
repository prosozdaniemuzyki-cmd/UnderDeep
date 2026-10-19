using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.World
{
    [CreateAssetMenu(menuName="UnderDeep/BiomeDef")]
    public class BiomeDef : ScriptableObject
    {
        public BiomeId id;
        public string displayName;
        public int minDepth;
        public int maxDepth;
        [Range(0,1)] public float stoneChance = 0.3f;
        [Range(0,1)] public float crystalChance = 0.05f;
        [Range(0,1)] public float bioChance = 0.03f;
        [Range(0,1)] public float wireChance = 0.04f;
        [Range(0,1)] public float gasChance = 0.02f;
        public Color fogColor = Color.black;
        public float wormSpawnChance = 0.02f;
    }
}
