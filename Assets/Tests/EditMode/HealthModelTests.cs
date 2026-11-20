using NUnit.Framework;
using TheLostDungeon.Combat;

namespace TheLostDungeon.Tests
{
    public class HealthModelTests
    {
        [Test]
        public void Damage_ReducesHealth_AndNeverGoesBelowZero()
        {
            var health = new HealthModel(100f);

            health.Damage(30f);
            Assert.AreEqual(70f, health.Current, 0.001f);

            health.Damage(500f);
            Assert.AreEqual(0f, health.Current, 0.001f);
            Assert.IsFalse(health.IsAlive);
        }

        [Test]
        public void Died_FiresExactlyOnce()
        {
            var health = new HealthModel(50f);
            int deaths = 0;
            health.Died += () => deaths++;

            health.Damage(60f);
            health.Damage(10f);   // already dead

            Assert.AreEqual(1, deaths);
        }

        [Test]
        public void Heal_IsCappedAtMax_AndDoesNothingWhenDead()
        {
            var health = new HealthModel(100f);
            health.Damage(20f);

            health.Heal(500f);
            Assert.AreEqual(100f, health.Current, 0.001f);

            health.Damage(100f);
            health.Heal(50f);
            Assert.AreEqual(0f, health.Current, 0.001f, "Dead targets can't be healed (use Revive)");
        }

        [Test]
        public void Changed_ReportsCurrentAndMax()
        {
            var health = new HealthModel(80f);
            float reportedCurrent = -1f, reportedMax = -1f;
            health.Changed += (c, m) => { reportedCurrent = c; reportedMax = m; };

            health.Damage(30f);

            Assert.AreEqual(50f, reportedCurrent, 0.001f);
            Assert.AreEqual(80f, reportedMax, 0.001f);
        }
    }
}
