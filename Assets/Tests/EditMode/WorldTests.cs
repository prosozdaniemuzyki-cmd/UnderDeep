using System.Collections.Generic;
using NUnit.Framework;
using UnderDeep.Core;
using UnderDeep.Player;
using UnderDeep.World;
using UnityEngine;

namespace UnderDeep.Tests
{
    public class WorldTests
    {
        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
            ServiceLocator.Clear();
        }

        [Test]
        public void DepthToBiomeMapping()
        {
            Assert.AreEqual(BiomeId.Surface, GameStateMachine.DepthToBiome(5));
            Assert.AreEqual(BiomeId.Metal, GameStateMachine.DepthToBiome(15));
            Assert.AreEqual(BiomeId.BioCaves, GameStateMachine.DepthToBiome(30));
            Assert.AreEqual(BiomeId.Crystal, GameStateMachine.DepthToBiome(50));
            Assert.AreEqual(BiomeId.Infection, GameStateMachine.DepthToBiome(80));
            Assert.AreEqual(BiomeId.Core, GameStateMachine.DepthToBiome(95));
        }

        [Test]
        public void MetaSaveAddAndConsume()
        {
            var meta = new MetaSave();
            meta.Add(ResourceType.RustMetal, 100);
            Assert.AreEqual(100, meta.rustMetal);
            Assert.IsTrue(meta.Consume(ResourceType.RustMetal, 30));
            Assert.AreEqual(70, meta.rustMetal);
            Assert.IsFalse(meta.Consume(ResourceType.Crystal, 10));
        }

        [Test]
        public void RunSaveAccumulatesLoot()
        {
            var run = new RunSave { seed = 123 };
            run.Add(ResourceType.Crystal, 2);
            run.Add(ResourceType.Crystal, 3);
            Assert.AreEqual(5, run.loot[ResourceType.Crystal]);
        }

        [Test]
        public void WorldGenerationIsRepeatableForTheSameSeed()
        {
            var worldObject = new GameObject("World test");
            var world = worldObject.AddComponent<WorldGrid>();
            world.width = 12;
            world.height = 20;
            world.biomes = new List<BiomeDef>
            {
                CreateBiome(BiomeId.Surface, 0, 20, 0.2f, 0.2f)
            };

            world.Generate(4312);
            var first = Snapshot(world);
            world.Generate(4312);
            var second = Snapshot(world);

            CollectionAssert.AreEqual(first, second);
            Object.DestroyImmediate(worldObject);
        }

        [Test]
        public void DrillDestroysBlockAndReportsItsDrop()
        {
            var worldObject = new GameObject("World test");
            var world = worldObject.AddComponent<WorldGrid>();
            world.width = 10;
            world.height = 12;
            var dirt = ScriptableObject.CreateInstance<BlockDef>();
            dirt.type = BlockType.Dirt;
            dirt.canDrill = true;
            dirt.hardness = 0.1f;
            dirt.dropType = ResourceType.RustMetal;
            dirt.dropMin = 1;
            dirt.dropMax = 1;
            world.blockDefs = new List<BlockDef> { dirt };
            world.biomes = new List<BiomeDef>();
            world.Generate(7);

            var result = world.DrillTick(new Vector2Int(0, 4), 1f, 0.2f);

            Assert.IsTrue(result.destroyed);
            Assert.AreEqual(BlockType.Dirt, result.type);
            Assert.AreEqual(ResourceType.RustMetal, result.resource);
            Assert.AreEqual(1, result.yieldAmount);
            Assert.IsTrue(world.IsAir(new Vector2Int(0, 4)));

            Object.DestroyImmediate(worldObject);
            Object.DestroyImmediate(dirt);
        }

        [Test]
        public void FuelCanBeConsumedDownToZero()
        {
            var player = new GameObject("Fuel test");
            var fuel = player.AddComponent<FuelSystem>();
            Assert.AreEqual(100f, fuel.Current);
            Assert.IsTrue(fuel.Consume(40f));
            Assert.AreEqual(60f, fuel.Current);
            Assert.IsTrue(fuel.Consume(100f));
            Assert.IsTrue(fuel.IsEmpty);
            Object.DestroyImmediate(player);
        }

        static BiomeDef CreateBiome(BiomeId id, int minDepth, int maxDepth, float stone, float crystal)
        {
            var biome = ScriptableObject.CreateInstance<BiomeDef>();
            biome.id = id;
            biome.minDepth = minDepth;
            biome.maxDepth = maxDepth;
            biome.stoneChance = stone;
            biome.crystalChance = crystal;
            return biome;
        }

        static BlockType[] Snapshot(WorldGrid world)
        {
            var cells = new BlockType[world.width * world.height];
            for (var y = 0; y < world.height; y++)
                for (var x = 0; x < world.width; x++)
                    cells[y * world.width + x] = world.Get(new Vector2Int(x, y)).type;
            return cells;
        }
    }
}
