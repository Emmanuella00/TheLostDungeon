using NUnit.Framework;
using TheLostDungeon.Weapons;

namespace TheLostDungeon.Tests
{
    public class HeatGaugeTests
    {
        [Test]
        public void Shots_AddHeat_AndCoolingReducesIt()
        {
            var gauge = new HeatGauge(coolPerSecond: 0.5f, overheatLockSeconds: 1f);

            gauge.AddHeat(0.4f);
            Assert.AreEqual(0.4f, gauge.Heat, 0.001f);

            gauge.Cool(0.5f);   // 0.5/s for 0.5s = -0.25
            Assert.AreEqual(0.15f, gauge.Heat, 0.001f);
        }

        [Test]
        public void ReachingFullHeat_Overheats_AndBlocksFiring()
        {
            var gauge = new HeatGauge(0.5f, 1.5f);

            bool overheatedNow = false;
            for (int i = 0; i < 20 && !overheatedNow; i++) overheatedNow = gauge.AddHeat(0.1f);

            Assert.IsTrue(overheatedNow);
            Assert.IsTrue(gauge.IsOverheated);
            Assert.IsFalse(gauge.CanFire);
            Assert.IsFalse(gauge.AddHeat(0.1f), "Can't add heat while locked");
        }

        [Test]
        public void Overheat_Unlocks_AfterLockTime()
        {
            var gauge = new HeatGauge(0.5f, 1.5f);
            gauge.AddHeat(1f);

            gauge.Cool(1f);
            Assert.IsTrue(gauge.IsOverheated, "Still locked after 1s of a 1.5s lock");

            gauge.Cool(0.6f);
            Assert.IsFalse(gauge.IsOverheated);
            Assert.IsTrue(gauge.CanFire);
            Assert.AreEqual(0f, gauge.Heat, 0.001f);
        }
    }
}
