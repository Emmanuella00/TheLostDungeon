using System.Collections.Generic;
using NUnit.Framework;
using TheLostDungeon.Data;
using TheLostDungeon.Items;

namespace TheLostDungeon.Tests
{
    /// <summary>Covers the brief's "Inventory logic" test area.</summary>
    public class InventoryTests
    {
        [Test]
        public void Add_IncreasesCount_AndHasReportsIt()
        {
            var inventory = new Inventory();

            int added = inventory.Add("keycard_red", 1);

            Assert.AreEqual(1, added);
            Assert.IsTrue(inventory.Has("keycard_red"));
            Assert.IsFalse(inventory.Has("keycard_blue"));
        }

        [Test]
        public void Add_RespectsMaxStack()
        {
            var inventory = new Inventory();

            inventory.Add("credits", 80, maxStack: 100);
            int added = inventory.Add("credits", 50, maxStack: 100);

            Assert.AreEqual(20, added, "Only 20 fit before the stack is full");
            Assert.AreEqual(100, inventory.Count("credits"));
        }

        [Test]
        public void Remove_FailsWithoutEnough_AndNeverGoesNegative()
        {
            var inventory = new Inventory();
            inventory.Add("credits", 10);

            Assert.IsFalse(inventory.Remove("credits", 25));
            Assert.AreEqual(10, inventory.Count("credits"));

            Assert.IsTrue(inventory.Remove("credits", 10));
            Assert.AreEqual(0, inventory.Count("credits"));
            Assert.IsFalse(inventory.Has("credits"));
        }

        [Test]
        public void SaveData_KeepsOnlyFilteredItems_AndLoadsBack()
        {
            var inventory = new Inventory();
            inventory.Add("credits", 250);
            inventory.Add("keycard_red", 1);

            List<InventoryItemData> saved = inventory.ToSaveData(id => id == "credits");   // only persistent items
            var restored = new Inventory();
            restored.LoadFrom(saved);

            Assert.AreEqual(250, restored.Count("credits"));
            Assert.AreEqual(0, restored.Count("keycard_red"), "Keycards are level-only and must not be saved");
        }

        [Test]
        public void Changed_ReportsNewCount()
        {
            var inventory = new Inventory();
            string lastId = null;
            int lastCount = -1;
            inventory.Changed += (id, count) => { lastId = id; lastCount = count; };

            inventory.Add("credits", 30);
            inventory.Remove("credits", 5);

            Assert.AreEqual("credits", lastId);
            Assert.AreEqual(25, lastCount);
        }
    }
}
