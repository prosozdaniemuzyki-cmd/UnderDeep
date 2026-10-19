using NUnit.Framework;
using UnderDeep.Core;
using UnderDeep.World;

namespace UnderDeep.Tests
{
    public class WorldTests
    {
        [Test]
        public void BlockTypeNotAirCanBeDrilled()
        {
            // smoke test for enum values
            Assert.AreNotEqual(BlockType.Air, BlockType.Dirt);
            Assert.AreNotEqual(BlockType.Bedrock, BlockType.Air);
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
        public void RunSaveLoot()
        {
            var run = new RunSave{ seed=123 };
            run.Add(ResourceType.Crystal, 2);
            run.Add(ResourceType.Crystal, 3);
            Assert.AreEqual(5, run.loot[ResourceType.Crystal]);
        }
    }
}
