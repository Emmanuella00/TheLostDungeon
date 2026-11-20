using NUnit.Framework;
using TheLostDungeon.Pooling;

namespace TheLostDungeon.Tests
{
    /// <summary>Covers the brief's "Object pooling behaviour" test area.</summary>
    public class ObjectPoolTests
    {
        private class Bolt
        {
            public bool Active;
        }

        private static ObjectPool<Bolt> NewPool(int prewarm, out int created)
        {
            int count = 0;
            var pool = new ObjectPool<Bolt>(
                create: () => { count++; return new Bolt(); },
                onGet: b => b.Active = true,
                onRelease: b => b.Active = false,
                prewarm: prewarm);
            created = count;
            return pool;
        }

        [Test]
        public void ReleasedObject_IsReused_InsteadOfCreatingNew()
        {
            ObjectPool<Bolt> pool = NewPool(0, out _);

            Bolt first = pool.Get();
            pool.Release(first);
            Bolt second = pool.Get();

            Assert.AreSame(first, second);
            Assert.AreEqual(1, pool.CountAll);
        }

        [Test]
        public void Prewarm_CreatesObjectsUpFront_AsInactive()
        {
            ObjectPool<Bolt> pool = NewPool(5, out int created);

            Assert.AreEqual(5, created);
            Assert.AreEqual(5, pool.CountInactive);
            Assert.AreEqual(0, pool.CountActive);
        }

        [Test]
        public void GetAndRelease_UpdateCounts_AndCallbacks()
        {
            ObjectPool<Bolt> pool = NewPool(2, out _);

            Bolt a = pool.Get();
            Bolt b = pool.Get();
            Bolt c = pool.Get();   // pool was empty, so this one is new

            Assert.AreEqual(3, pool.CountActive);
            Assert.IsTrue(a.Active && b.Active && c.Active);

            pool.Release(b);
            Assert.AreEqual(2, pool.CountActive);
            Assert.AreEqual(1, pool.CountInactive);
            Assert.IsFalse(b.Active);
        }

        [Test]
        public void ReleasingTwice_IsIgnored()
        {
            ObjectPool<Bolt> pool = NewPool(0, out _);
            Bolt bolt = pool.Get();

            Assert.IsTrue(pool.Release(bolt));
            Assert.IsFalse(pool.Release(bolt));
            Assert.AreEqual(1, pool.CountInactive, "A double release must not add the object twice");
        }
    }
}
