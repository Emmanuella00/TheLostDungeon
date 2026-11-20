using System;
using System.Collections.Generic;

namespace TheLostDungeon.Pooling
{
    /// <summary>
    /// OBJECT POOLING PATTERN, written as plain generic C# so its behaviour can be unit tested.
    ///
    /// Instead of creating and destroying objects (expensive, and the garbage collector causes
    /// stutters on WebGL/mobile), finished objects are put back in the pool and handed out again.
    ///
    /// Get():     reuse an inactive object, or create a new one if the pool is empty.
    /// Release(): return an object so it can be reused. Releasing twice is ignored.
    /// </summary>
    public class ObjectPool<T> where T : class
    {
        private readonly Func<T> create;
        private readonly Action<T> onGet;
        private readonly Action<T> onRelease;
        private readonly Stack<T> inactive = new Stack<T>();
        private readonly HashSet<T> inactiveSet = new HashSet<T>();

        public int CountAll { get; private set; }
        public int CountInactive => inactive.Count;
        public int CountActive => CountAll - CountInactive;

        public ObjectPool(Func<T> create, Action<T> onGet = null, Action<T> onRelease = null, int prewarm = 0)
        {
            this.create = create ?? throw new ArgumentNullException(nameof(create));
            this.onGet = onGet;
            this.onRelease = onRelease;
            Prewarm(prewarm);
        }

        /// <summary>Creates objects up front (e.g. during the loading screen) so gameplay never has to.</summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                T item = create();
                CountAll++;
                onRelease?.Invoke(item);
                inactive.Push(item);
                inactiveSet.Add(item);
            }
        }

        public T Get()
        {
            T item;
            if (inactive.Count > 0)
            {
                item = inactive.Pop();
                inactiveSet.Remove(item);
            }
            else
            {
                item = create();
                CountAll++;
            }

            onGet?.Invoke(item);
            return item;
        }

        /// <summary>Returns true if the object was accepted (false if it was already in the pool).</summary>
        public bool Release(T item)
        {
            if (item == null || inactiveSet.Contains(item)) return false;

            onRelease?.Invoke(item);
            inactive.Push(item);
            inactiveSet.Add(item);
            return true;
        }

        /// <summary>Empties the pool, calling destroy on every inactive object.</summary>
        public void Clear(Action<T> destroy = null)
        {
            while (inactive.Count > 0)
            {
                T item = inactive.Pop();
                destroy?.Invoke(item);
                CountAll--;
            }
            inactiveSet.Clear();
        }
    }
}
