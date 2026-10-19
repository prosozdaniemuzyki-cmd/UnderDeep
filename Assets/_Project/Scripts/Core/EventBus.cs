using System;
using System.Collections.Generic;

namespace UnderDeep.Core
{
    /// <summary>Zero-GC EventBus using struct events. Subscribe/unsubscribe required.</summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _subs = new();

        public static void Subscribe<T>(Action<T> handler)
        {
            var t = typeof(T);
            if (!_subs.TryGetValue(t, out var list)) { list = new List<Delegate>(4); _subs[t]=list; }
            if (!list.Contains(handler)) list.Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (_subs.TryGetValue(typeof(T), out var list)) list.Remove(handler);
        }

        public static void Publish<T>(T evt)
        {
            if (!_subs.TryGetValue(typeof(T), out var list)) return;
            // copy to avoid mutation during publish
            var copy = list.ToArray();
            foreach (var d in copy) ((Action<T>)d).Invoke(evt);
        }

        public static void Clear() => _subs.Clear();
    }

    // ---- Events (lightweight) ----
    public readonly struct BlockMined { public readonly UnityEngine.Vector2Int pos; public readonly BlockType type; public readonly int yield; public readonly ResourceType resource; public BlockMined(UnityEngine.Vector2Int p, BlockType t,int y, ResourceType r){pos=p;type=t;yield=y;resource=r;} }
    public readonly struct BlockDamaged { public readonly UnityEngine.Vector2Int pos; public readonly float progress; public BlockDamaged(UnityEngine.Vector2Int p,float pr){pos=p;progress=pr;} }
    public readonly struct BiomeEntered { public readonly BiomeId biome; public readonly int depth; public BiomeEntered(BiomeId b,int d){biome=b;depth=d;} }

    public readonly struct FuelChanged { public readonly float current; public readonly float max; public readonly float delta; public FuelChanged(float c,float m,float d){current=c;max=m;delta=d;} }
    public readonly struct HeatChanged { public readonly float heat; public readonly bool overheated; public HeatChanged(float h,bool o){heat=h;overheated=o;} }
    public readonly struct HealthChanged { public readonly int hp; public readonly int maxHp; public HealthChanged(int h,int m){hp=h;maxHp=m;} }
    public readonly struct SanityChanged { public readonly float sanity; public readonly SanityStage stage; public SanityChanged(float s,SanityStage st){sanity=s;stage=st;} }
    public readonly struct DepthChanged { public readonly int depth; public readonly int maxDepthThisRun; public DepthChanged(int d,int m){depth=d;maxDepthThisRun=m;} }
    public readonly struct PlayerMoved { public readonly UnityEngine.Vector2Int from; public readonly UnityEngine.Vector2Int to; public PlayerMoved(UnityEngine.Vector2Int f, UnityEngine.Vector2Int t){from=f;to=t;} }
    public readonly struct DrillStarted { public readonly UnityEngine.Vector2Int target; public DrillStarted(UnityEngine.Vector2Int t){target=t;} }
    public readonly struct DrillEnded { public readonly UnityEngine.Vector2Int target; public readonly bool success; public DrillEnded(UnityEngine.Vector2Int t,bool s){target=t;success=s;} }

    public readonly struct ResourceCollected { public readonly ResourceType type; public readonly int amount; public readonly int totalRun; public ResourceCollected(ResourceType t,int a,int tot){type=t;amount=a;totalRun=tot;} }
    public readonly struct RunEnded { public readonly RunEndReason reason; public readonly int depth; public RunEnded(RunEndReason r,int d){reason=r;depth=d;} }
    public readonly struct UpgradePurchased { public readonly string id; public UpgradePurchased(string i){id=i;} }
    public readonly struct ArtifactFound { public readonly string artifactId; public readonly int count; public ArtifactFound(string a,int c){artifactId=a;count=c;} }
    public readonly struct GameStateChanged { public readonly GameState from; public readonly GameState to; public GameStateChanged(GameState f,GameState t){from=f;to=t;} }
    public readonly struct RewardedResult { public readonly string placement; public readonly bool success; public RewardedResult(string p,bool s){placement=p;success=s;} }

    // Ловушки (газ/провода/обвал). Раньше жило в World/TrapSystem.cs — перенесено в Core, чтобы событие видели все сборки (asmdef-аудит 1.2.1)
    public readonly struct TrapTriggered { public readonly UnityEngine.Vector2Int pos; public readonly TrapType type; public TrapTriggered(UnityEngine.Vector2Int p, TrapType t){pos=p;type=t;} }
    // Запрос эвакуации (хоткей B в Player/DrillSystem) → ловит Progression/RunResultHandler. Развязка Player → Progression
    public readonly struct EvacRequested { }

    public enum BlockType : byte { Air=0, Dirt, Stone, RustMetal, CrystalOre, BioGel, GasVein, Wire, CrystalWall, Bedrock, ArtifactChunk }
    public enum ResourceType : byte { RustMetal, Crystal, BioGel, Artifact }
    public enum BiomeId : byte { Surface, Metal, BioCaves, Crystal, Infection, Core }
    public enum SanityStage : byte { Clear, LightDistortion, FakeResources, FakeEnemies, Blackout }
    public enum RunEndReason : byte { Death, Evac, CoreActivate, Escape, EmergencyEvac }
    public enum GameState { Boot, Base, Run, Paused, Death, Evac, Ending }
    public enum TrapType { Gas, Wire, GasTick, CaveIn }
}
