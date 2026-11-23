using NUnit.Framework;
using TheLostDungeon.Abilities;

namespace TheLostDungeon.Tests
{
    /// <summary>Ability cooldown timing.</summary>
    public class CooldownTests
    {
        [Test]
        public void NewCooldown_IsReady()
        {
            Assert.IsTrue(new Cooldown(5f).IsReady);
        }

        [Test]
        public void AfterStart_NotReadyUntilDurationPasses()
        {
            var cooldown = new Cooldown(3f);
            cooldown.Start();

            Assert.IsFalse(cooldown.IsReady);
            Assert.IsFalse(cooldown.Tick(2f));
            Assert.AreEqual(1f, cooldown.Remaining, 0.001f);

            Assert.IsTrue(cooldown.Tick(1.5f), "Tick reports the frame it becomes ready");
            Assert.IsTrue(cooldown.IsReady);
            Assert.AreEqual(0f, cooldown.Remaining, 0.001f, "Never goes negative");
        }

        [Test]
        public void Normalized_GoesFromOneToZero()
        {
            var cooldown = new Cooldown(10f);
            cooldown.Start();
            Assert.AreEqual(1f, cooldown.Normalized, 0.001f);

            cooldown.Tick(2.5f);
            Assert.AreEqual(0.75f, cooldown.Normalized, 0.001f);
        }
    }
}
