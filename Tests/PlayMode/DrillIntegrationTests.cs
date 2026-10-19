using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnderDeep.Core;
using UnderDeep.World;

namespace UnderDeep.Tests
{
    public class DrillIntegrationTests
    {
        [UnityTest]
        public IEnumerator WorldGridGeneratesSurfaceAir()
        {
            var go = new GameObject("World");
            var world = go.AddComponent<WorldGrid>();
            world.width=10; world.height=10;
            world.Generate(42);
            Assert.AreEqual(BlockType.Air, world.Get(new Vector2Int(5,1)).type);
            Object.DestroyImmediate(go);
            yield return null;
        }
    }
}
