using System;
using System.Collections.Generic;
using System.Linq;

namespace Opus.DataStructure
{
    [Serializable]
    public class SearchableList<T> : List<T>
    {
        private List<T> cachedList = null;
        private List<T> CachedList
        {
            get
            {
                return cachedList ?? this;
            }
            set
            {
                cachedList = value; 
            }
        }

        private Func<T, bool> filter = x => true;

        public SearchableList() { }
        public SearchableList(IEnumerable<T> collection) : base(collection) { AddRange(collection); }

        public void SetFilter(Func<T, bool> filter)
        {
            this.filter = filter;
            cachedList = this.Where(filter).ToList();
        }


        public new T this[int index]
        {
            get
            {
                return CachedList[index];
            }
        }

        public new int Count
        {
            get
            {
                return CachedList.Count;
            }
        }

        public void Invalidate()
        {
            cachedList = this.Where(filter).ToList();
        }
    }
}