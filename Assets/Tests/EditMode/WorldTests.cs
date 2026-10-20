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

        [Test]
        public void RunCrystalRefuelsAboveReserveButCannotBuyAnEvacuationAtZeroFuel()
        {
            var player = new GameObject("Fuel and inventory test");
            var fuel = player.AddComponent<FuelSystem>();
            var inventory = player.AddComponent<PlayerInventory>();
            var meta = new MetaSave { crystal = 19 };
            ServiceLocator.Register(new SaveService(null, meta));

            fuel.Consume(89f);
            inventory.Add(ResourceType.Crystal, 2);
            Assert.IsTrue(fuel.CanRefuelFromCrystal);
            Assert.IsTrue(fuel.TryRefuelFromRunCrystal());
            Assert.AreEqual(26f, fuel.Current);
            Assert.AreEqual(1, inventory.Get(ResourceType.Crystal));

            Assert.IsTrue(fuel.TryPayEvac());
            Assert.AreEqual(16f, fuel.Current);
            Assert.AreEqual(19, meta.crystal);
            fuel.Consume(7f);
            Assert.IsFalse(fuel.CanRefuelFromCrystal);
            Assert.IsFalse(fuel.TryRefuelFromRunCrystal());
            Assert.IsFalse(fuel.TryPayEvac());

            fuel.Consume(9f);
            Assert.IsTrue(fuel.IsDepleted);
            Assert.IsFalse(fuel.TryRefuelFromRunCrystal());
            Assert.IsFalse(fuel.TryPayEvac());
            meta.crystal = 20;
            Assert.IsTrue(fuel.CanPayDepletedRescue);
            Assert.IsTrue(fuel.TryPayEvac());
            Assert.AreEqual(0, meta.crystal);
            Assert.AreEqual(1, inventory.Get(ResourceType.Crystal));
            Object.DestroyImmediate(player);
        }

        [Test]
        public void PaidEvacuationBanksRunLootAndArtifacts()
        {
            var player = new GameObject("Inventory evacuation test");
            var inventory = player.AddComponent<PlayerInventory>();
            var meta = new MetaSave();
            ServiceLocator.Register(new SaveService(null, meta));
            inventory.Add(ResourceType.RustMetal, 5);
            inventory.Add(ResourceType.Artifact, 1);

            inventory.ApplyToMeta(true);

            Assert.AreEqual(5, meta.rustMetal);
            Assert.AreEqual(1, meta.artifacts.Count);
            Assert.AreEqual(0, inventory.Get(ResourceType.RustMetal));
            Object.DestroyImmediate(player);
        }

        [Test]
        public void DeathDiscardsAllRunLootIncludingArtifacts()
        {
            var player = new GameObject("Inventory death test");
            var inventory = player.AddComponent<PlayerInventory>();
            var meta = new MetaSave();
            ServiceLocator.Register(new SaveService(null, meta));
            inventory.Add(ResourceType.RustMetal, 5);
            inventory.Add(ResourceType.Crystal, 2);
            inventory.Add(ResourceType.Artifact, 1);

            inventory.ApplyToMeta(false);

            Assert.AreEqual(0, meta.rustMetal);
            Assert.AreEqual(0, meta.crystal);
            Assert.AreEqual(0, meta.artifacts.Count);
            Assert.AreEqual(0, inventory.Total());
            Assert.AreEqual(0, inventory.Get(ResourceType.Artifact));
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
