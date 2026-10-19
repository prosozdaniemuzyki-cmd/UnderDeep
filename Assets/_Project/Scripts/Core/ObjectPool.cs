using System.Collections.Generic;
using UnityEngine;

namespace UnderDeep.Core
{
    public interface IPoolable { void OnSpawn(); void OnDespawn(); }

    public class ObjectPool<T> where T : Component, IPoolable
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Stack<T> _pool = new();
        public ObjectPool(T prefab, Transform parent,int prewarm=8){ _prefab=prefab; _parent=parent; for(int i=0;i<prewarm;i++){ var o=Object.Instantiate(prefab,parent); o.gameObject.SetActive(false); _pool.Push(o);} }
        public T Get(){ T o = _pool.Count>0 ? _pool.Pop() : Object.Instantiate(_prefab,_parent); o.gameObject.SetActive(true); o.OnSpawn(); return o; }
        public void Release(T o){ o.OnDespawn(); o.gameObject.SetActive(false); _pool.Push(o); }
    }
}
