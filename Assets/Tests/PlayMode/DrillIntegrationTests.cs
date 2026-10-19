using System.Collections;
using System.Collections.Generic;
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
        public IEnumerator WorldGridGeneratesSurfaceAndDirtBelowShaft()
        {
            var go = new GameObject("World");
            var world = go.AddComponent<WorldGrid>();
            world.width = 10;
            world.height = 12;
            world.blockDefs = new List<BlockDef>();
            world.biomes = new List<BiomeDef>();
            world.Generate(42);

            Assert.AreEqual(BlockType.Air, world.Get(new Vector2Int(5, 1)).type);
            Assert.AreEqual(BlockType.Air, world.Get(new Vector2Int(5, 5)).type);
            Assert.AreEqual(BlockType.Dirt, world.Get(new Vector2Int(0, 4)).type);
            Assert.IsTrue(world.InBounds(new Vector2Int(9, 11)));
            Assert.IsFalse(world.InBounds(new Vector2Int(10, 11)));

            Object.Destroy(go);
            yield return null;
        }
    }
}
