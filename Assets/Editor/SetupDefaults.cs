#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnderDeep.Progression;
using UnderDeep.Core;
using UnderDeep.World;

namespace UnderDeep.EditorTools
{
    public static class SetupDefaults
    {
        [MenuItem("UnderDeep/Create Default Upgrades")]
        static void CreateUpgrades()
        {
            CreateUpgrade("drill_mk2", "Бур MK2", "Скорость бурения +25%", new Cost[]{new(){type=ResourceType.RustMetal, amount=50}}, new StatMod[]{new(){stat=StatType.DrillSpeed, value=0.25f}}, null);
            CreateUpgrade("drill_mk3", "Бур MK3", "Скорость бурения +50%", new Cost[]{new(){type=ResourceType.RustMetal, amount=150}, new(){type=ResourceType.Crystal, amount=30}}, new StatMod[]{new(){stat=StatType.DrillSpeed, value=0.5f}}, "drill_mk2");
            CreateUpgrade("battery_1", "Аккумулятор I", "Макс. топливо +20", new Cost[]{new(){type=ResourceType.Crystal, amount=40}}, new StatMod[]{new(){stat=StatType.MaxFuel, value=20}}, null);
            CreateUpgrade("battery_2", "Аккумулятор II", "Макс. топливо +20", new Cost[]{new(){type=ResourceType.Crystal, amount=60}}, new StatMod[]{new(){stat=StatType.MaxFuel, value=20}}, "battery_1");
            CreateUpgrade("radar", "Радар", "Видит редкие ресурсы сквозь стены", new Cost[]{new(){type=ResourceType.RustMetal, amount=80}}, new StatMod[]{new(){stat=StatType.Radar, value=1}}, null);
            CreateUpgrade("magnet", "Магнитный захват", "Авто-подбор в радиусе 3 клеток", new Cost[]{new(){type=ResourceType.RustMetal, amount=100}, new(){type=ResourceType.BioGel, amount=10}}, new StatMod[]{new(){stat=StatType.Magnet, value=3}}, null);
            CreateUpgrade("shield", "Аварийный щит", "Спасает от смертельного урона 1 раз", new Cost[]{new(){type=ResourceType.RustMetal, amount=200}, new(){type=ResourceType.Crystal, amount=50}}, new StatMod[]{new(){stat=StatType.Shield, value=1}}, null);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Upgrades created");
        }

        static void CreateUpgrade(string id, string name, string desc, Cost[] costs, StatMod[] mods, string prereq)
        {
            var path = $"Assets/_Project/ScriptableObjects/Upgrades/{id}.asset";
            var so = ScriptableObject.CreateInstance<UpgradeDef>();
            so.id = id; so.displayName = name; so.description = desc; so.costs=costs; so.mods=mods; so.prereqId=prereq;
            System.IO.Directory.CreateDirectory("Assets/_Project/ScriptableObjects/Upgrades");
            AssetDatabase.CreateAsset(so, path);
        }

        [MenuItem("UnderDeep/Create Default Biomes")]
        static void CreateBiomes()
        {
            CreateBiome(BiomeId.Surface, "Ржавые пустоши", 0,10, 0.1f,0,0,0,0);
            CreateBiome(BiomeId.Metal, "Металлический слой", 10,20, 0.4f,0.05f,0,0.04f,0.02f);
            CreateBiome(BiomeId.BioCaves, "Био-пещеры", 20,40, 0.25f,0.08f,0.06f,0,0.02f);
            CreateBiome(BiomeId.Crystal, "Кристаллы", 40,70, 0.2f,0.15f,0.03f,0,0.01f);
            CreateBiome(BiomeId.Infection, "Зона заражения", 70,90, 0.3f,0.1f,0.02f,0.01f,0.05f);
            CreateBiome(BiomeId.Core, "Ядро", 90,100, 0.1f,0.02f,0,0,0);
            AssetDatabase.SaveAssets();
        }
        static void CreateBiome(BiomeId id, string name,int min,int max,float stone,float crystal,float bio,float wire,float gas)
        {
            var so = ScriptableObject.CreateInstance<BiomeDef>();
            so.id=id; so.displayName=name; so.minDepth=min; so.maxDepth=max; so.stoneChance=stone; so.crystalChance=crystal; so.bioChance=bio; so.wireChance=wire; so.gasChance=gas;
            System.IO.Directory.CreateDirectory("Assets/_Project/ScriptableObjects/Biomes");
            AssetDatabase.CreateAsset(so, $"Assets/_Project/ScriptableObjects/Biomes/{id}.asset");
        }
    }
}
#endif
