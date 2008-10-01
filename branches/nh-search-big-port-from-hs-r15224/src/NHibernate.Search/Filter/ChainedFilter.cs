using System.Collections;
using Lucene.Net.Index;

namespace NHibernate.Search.Filter
{
    using System.Collections.Generic;

    public class ChainedFilter : Lucene.Net.Search.Filter
    {
        private const long serialVersionUID = -6153052295766531920L;

        private readonly List<Lucene.Net.Search.Filter> chainedFilters = new List<Lucene.Net.Search.Filter>();

        public virtual void AddFilter(Lucene.Net.Search.Filter filter)
        {
            this.chainedFilters.Add(filter);
        }

        public override BitArray Bits(IndexReader reader)
        {
            if (chainedFilters.Count == 0)
                throw new AssertionFailure("Chainedfilter has no filters to chain for");
            //we need to copy the first BitSet because BitSet is modified by .logicalOp
            Lucene.Net.Search.Filter filter = chainedFilters[0];
            BitArray result = (BitArray)filter.Bits(reader).Clone();
            for (int index = 1; index < chainedFilters.Count; index++)
            {
                result.And(chainedFilters[index].Bits(reader));
            }
            return result;
        }

        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder("ChainedFilter [");
            foreach (IFilter filter in chainedFilters)
            {
                sb.Append("\n  ").Append(filter.ToString());
            }
            return sb.Append("\n]").ToString();
        }
    }
}