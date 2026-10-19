using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.Progression
{
    public enum StatType { DrillSpeed, MaxFuel, Magnet, Radar, Shield }

    [System.Serializable]
    public struct Cost { public ResourceType type; public int amount; }

    [System.Serializable]
    public struct StatMod { public StatType stat; public float value; }

    [CreateAssetMenu(menuName="UnderDeep/UpgradeDef")]
    public class UpgradeDef : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Cost[] costs;
        public StatMod[] mods;
        public string prereqId;
        public Sprite icon;
    }
}
