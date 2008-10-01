using System;
using System.Collections;
using System.Collections.Generic;
using NHibernate.Search.Engine;

namespace NHibernate.Search.Query
{
    //TODO load the next batch-size elements to benefit from batch-size 
    public class IteratorImpl<T> : IEnumerable<T>, IEnumerator<T>
    {
        private IList<EntityInfo> entityInfos;
        private int size;
        private ILoader loader;
        private int nextObjectIndex=-1;
        private int index;
        private object next;

        public IteratorImpl(IList<EntityInfo> entityInfos, ILoader loader)
        {
            this.entityInfos = entityInfos;
            this.loader = loader;
            size = entityInfos.Count;
        }
        public IEnumerator<T> GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            if (nextObjectIndex == index) 
                return next != null;
            next = null;
            nextObjectIndex = -1;
            do
            {
                if (index >= size)
                {
                    nextObjectIndex = index;
                    next = null;
                    return false;
                }
                next = loader.Load(entityInfos[index]);
                if (next == null)
                {
                    index++;
                }
                else
                {
                    nextObjectIndex = index;
                }
            } while (next == null);
            index++;
            return true;
        }

        public void Reset()
        {
            throw new NotSupportedException("Cannot remove from a lucene query iterator");

        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public T Current
        {
            get { return (T)next; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
        }
    }
}