using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.common {
    public class WeakList<T> : IList<T> where T:class {
        private List<WeakReference<T>> list;
        public WeakList() {
            list = new List<WeakReference<T>>();
        }
        public WeakList(int capacity) {
            list = new List<WeakReference<T>>(capacity);
        }
        public WeakList(IEnumerable<T> source) {
            list = new List<WeakReference<T>>(source.Select((c)=>new WeakReference<T>(c)));
        }

        public T this[int index] {
            get => list[index].GetValue();
            set => list[index] = new WeakReference<T>(value);
        }

        public int Count => list.Count;

        public bool IsReadOnly => false;

        public void Add(T item) {
            list.Add(new WeakReference<T>(item));
        }

        public void Clear() {
            list.Clear();
        }

        public bool Contains(T item) {
            return list.Find((c) => c.GetValue() == item) != null;
        }

        public void CopyTo(T[] array, int arrayIndex) {
            list.Select((c) => c.GetValue()).ToList().CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator() {
            return list.Select((c) => c.GetValue()).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return list.Select((c) => c.GetValue()).GetEnumerator();
        }


        public int IndexOf(T item) {
            for(int i=0; i<list.Count; i++) {
                if(list[i].GetValue() == item) {
                    return i;
                }
            }
            return -1;
        }

        public void Insert(int index, T item) {
            list.Insert(index, new WeakReference<T>(item));
        }

        public bool Remove(T item) {
            return list.RemoveAll((c) => c.GetValue() == item) >= 0;
        }

        public void RemoveAt(int index) {
            list.RemoveAt(index);
        }

        public void Trim() {
            list = list.Where((c) => c.GetValue() != null).ToList();
        }
    }
}
