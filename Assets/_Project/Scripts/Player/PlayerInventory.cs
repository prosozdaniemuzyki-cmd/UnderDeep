using System.Collections.Generic;
using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.Player
{
    public class PlayerInventory : MonoBehaviour
    {
        private readonly Dictionary<ResourceType,int> _run = new();
        public IReadOnlyDictionary<ResourceType,int> RunLoot => _run;

        void Awake() => ServiceLocator.Register(this);

        void OnEnable(){ EventBus.Subscribe<BlockMined>(OnMined); }
        void OnDisable(){ EventBus.Unsubscribe<BlockMined>(OnMined); }

        void OnMined(BlockMined e)
        {
            Add(e.resource, e.yield);
        }

        public void Add(ResourceType t,int amt)
        {
            if (amt<=0) return;
            if (!_run.ContainsKey(t)) _run[t]=0;
            _run[t]+=amt;
            // also sync to SaveService Run
            if (ServiceLocator.TryGet<SaveService>(out var save)) save.Run.Add(t,amt);
            EventBus.Publish(new ResourceCollected(t,amt,_run[t]));
        }

        public int Get(ResourceType t)=> _run.TryGetValue(t,out var v)?v:0;
        public int Total()=> Get(ResourceType.RustMetal)+Get(ResourceType.Crystal)*3+Get(ResourceType.BioGel)*5;
        public void Clear(){ _run.Clear(); }
        public Dictionary<ResourceType,int> Snapshot()=> new(_run);

        // Called on evac/death
        public void ApplyToMeta(bool keep)
        {
            if (!ServiceLocator.TryGet<SaveService>(out var save)) return;
            if (keep) foreach(var kv in _run) save.Meta.Add(kv.Key, kv.Value);
            // artifact special
            if (_run.TryGetValue(ResourceType.Artifact, out var art) && art>0)
                for(int i=0;i<art;i++) save.Meta.artifacts.Add($"artifact_{save.Meta.artifacts.Count+1}");
            Clear();
        }
    }
}
