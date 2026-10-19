using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnderDeep.Core
{
    [Serializable]
    public class MetaSave
    {
        public int version = 1;
        public int rustMetal = 0;
        public int crystal = 0;
        public int bioGel = 0;
        public List<string> purchasedUpgrades = new();
        public List<string> artifacts = new();
        public int maxDepthReached = 0;
        public int totalRuns = 0;

        public int Get(ResourceType t) => t switch { ResourceType.RustMetal => rustMetal, ResourceType.Crystal => crystal, ResourceType.BioGel => bioGel, _ => 0 };
        public void Add(ResourceType t,int amt){ if(t==ResourceType.RustMetal) rustMetal+=amt; else if(t==ResourceType.Crystal) crystal+=amt; else if(t==ResourceType.BioGel) bioGel+=amt; }
        public bool Consume(ResourceType t,int amt){ if(Get(t)<amt) return false; Add(t,-amt); return true; }
    }

    [Serializable]
    public class RunSave
    {
        public int seed;
        public int depth;
        public Dictionary<ResourceType,int> loot = new();
        public void Add(ResourceType t,int a){ if(!loot.ContainsKey(t)) loot[t]=0; loot[t]+=a; }
        public void Clear(){ loot.Clear(); depth=0; }
    }

    public interface IPlatformProvider
    {
        PlatformId Id { get; }
        System.Threading.Tasks.Task InitAsync();
        System.Threading.Tasks.Task SaveAsync(string json);
        System.Threading.Tasks.Task<string> LoadAsync();
        System.Threading.Tasks.Task ShowInterstitialAsync();
        System.Threading.Tasks.Task<bool> ShowRewardedAsync(string placement);
        System.Threading.Tasks.Task SetLeaderboardAsync(string board,int score);
        void GameReady();
        void GameplayStart();
        void GameplayStop();
        string Language { get; }
    }
    public enum PlatformId { Standalone, YandexGames, VKPlay }

    public class SaveService
    {
        private readonly IPlatformProvider _platform;
        public MetaSave Meta { get; private set; }
        public RunSave Run { get; private set; } = new();

        public SaveService(IPlatformProvider platform, MetaSave meta) { _platform = platform; Meta = meta; }

        public void NewRun(int seed){ Run = new RunSave{ seed=seed }; }

        public async System.Threading.Tasks.Task PersistMetaAsync()
        {
            var json = JsonUtility.ToJson(Meta, false);
            await _platform.SaveAsync(json);
            Debug.Log($"[Save] Persisted {json}");
        }

        public async System.Threading.Tasks.Task SubmitDepthAsync(int depth)
        {
            if (depth > Meta.maxDepthReached) Meta.maxDepthReached = depth;
            await _platform.SetLeaderboardAsync("maxDepth", Meta.maxDepthReached);
        }
    }
}
