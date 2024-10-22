using System.Collections;

namespace YukiPastebin {
    public class BiMap<T1, T2> : ICollection<(T1, T2)> where T1 : notnull where T2 : notnull {
        private readonly Dictionary<T1, T2> forward = new();
        private readonly Dictionary<T2, T1> backward = new();
        public IReadOnlyDictionary<T1, T2> Forward => forward;
        public IReadOnlyDictionary<T2, T1> Backward => backward;

        public int Count => forward.Count;

        public bool IsReadOnly => false;

        public void Add((T1, T2) item) {
            Add(item.Item1, item.Item2);
        }

        public void Clear() {
            forward.Clear();
            backward.Clear();
        }

        public bool Contains((T1, T2) item) {
            return EqualityComparer<T2>.Default.Equals(forward[item.Item1], item.Item2);
        }

        public void CopyTo((T1, T2)[] array, int arrayIndex) {
            int i = 0;
            foreach (var (k, v) in forward) {
                array[arrayIndex + i] = (k, v);
                i++;
            }
        }

        public IEnumerator<(T1, T2)> GetEnumerator() {
            foreach (var (k, v) in forward) {
                yield return (k, v);
            }
        }

        public bool Remove((T1, T2) item) {
            if (!Contains(item)) {
                return false;
            }
            forward.Remove(item.Item1);
            backward.Remove(item.Item2);
            return true;
        }

        public bool Remove(T1 item1) {
            if (forward.Remove(item1, out T2? item2)) {
                backward.Remove(item2);
                return true;
            }
            return false;
        }
        public bool Remove(T2 item2) {
            if (backward.Remove(item2, out T1? item1)) {
                forward.Remove(item1);
                return true;
            }
            return false;
        }

        public void AddOrReplace(T1 item1, T2 item2) {
            Remove(item1);
            Remove(item2);
            Add(item1, item2);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public void Add(T1 item1, T2 item2) {
            forward.Add(item1, item2);
            backward.Add(item2, item1);
        }
    }
}
