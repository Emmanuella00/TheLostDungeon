using System.IO;
using NUnit.Framework;
using TheLostDungeon.Data;
using TheLostDungeon.Levels;

namespace TheLostDungeon.Tests
{
    /// <summary>Covers the brief's "Save/load functionality" test area.</summary>
    public class SaveSystemTests
    {
        private string tempFile;

        [SetUp]
        public void SetUp() => tempFile = Path.Combine(Path.GetTempPath(), $"tld_test_{System.Guid.NewGuid():N}.json");

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }

        [Test]
        public void JsonRoundTrip_PreservesProgressStatsAndInventory()
        {
            var original = new SaveData();
            new LevelProgression(original, 5).CompleteLevel(0, 95.5f, 3, 1200, out _);
            original.stats.enemiesDefeated = 42;
            original.stats.credits = 300;
            original.inventory.Add(new InventoryItemData { itemId = "keycard_red", amount = 1 });
            original.unlockedAbilities.Add("dash");

            SaveData loaded = SaveSerializer.FromJson(SaveSerializer.ToJson(original));

            Assert.IsNotNull(loaded);
            var progression = new LevelProgression(loaded, 5);
            Assert.AreEqual(LevelStatus.Completed, progression.GetStatus(0));
            Assert.AreEqual(LevelStatus.Available, progression.GetStatus(1));
            Assert.AreEqual(95.5f, progression.GetRecord(0).bestTimeSeconds, 0.001f);
            Assert.AreEqual(42, loaded.stats.enemiesDefeated);
            Assert.AreEqual(300, loaded.stats.credits);
            Assert.AreEqual("keycard_red", loaded.inventory[0].itemId);
            CollectionAssert.Contains(loaded.unlockedAbilities, "dash");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("{ this is not json")]
        public void CorruptOrEmptyJson_ReturnsNull_InsteadOfThrowing(string json)
        {
            Assert.IsNull(SaveSerializer.FromJson(json));
        }

        [Test]
        public void FileStorage_WriteReadDelete_Works()
        {
            var storage = new FileSaveStorage(tempFile);

            Assert.IsFalse(storage.Exists());
            storage.Write("{\"version\":1}");
            Assert.IsTrue(storage.Exists());
            Assert.AreEqual("{\"version\":1}", storage.Read());

            storage.Write("{\"version\":2}");   // overwrite via temp-file swap
            Assert.AreEqual("{\"version\":2}", storage.Read());

            storage.Delete();
            Assert.IsFalse(storage.Exists());
        }
    }
}
