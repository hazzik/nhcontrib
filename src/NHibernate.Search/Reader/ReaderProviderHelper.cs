using System;
using Iesi.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace NHibernate.Search.Reader
{
    /// <summary>
    /// 
    /// </summary>
    public static class ReaderProviderHelper
    {
        public static IndexReader BuildMultiReader(int length, IndexReader[] readers)
        {
            if (length == 0)
                return null;

            try
            {
                // NB Everything should be the same so wrap in a CacheableMultiReader even if there's only one.
                return new CacheableMultiReader(readers);
            }
            catch (Exception e)
            {
                Clean(readers);
                throw new SearchException("Unable to open a MultiReader", e);
            }
        }

        public static void Clean(params IndexReader[] readers)
        {
            foreach (IndexReader reader in readers)
            {
                try
                {
                    reader.Close();
                }
                catch (Exception)
                {
                    // Swallow
                }
            }
        }

       /// <summary>
        /// Find the underlying IndexReaders for the given searchable
       /// </summary>
        public static ISet<IndexReader> GetIndexReaders(Searchable searchable)
        {
            Set<IndexReader> readers = new HashedSet<IndexReader>();
            GetIndexReadersInternal(readers, searchable);
            return readers;
        }

       /// <summary>
        /// Recursive method should identify all underlying readers for any nested structure of Lucene Searchable or IndexReader
       /// </summary>
        private static void GetIndexReadersInternal(ISet<IndexReader> readers, Object obj) {
		if ( obj is MultiSearcher ) {
		    foreach (Searchable searchable in ((MultiSearcher)obj).GetSearchables())
		    {
		        GetIndexReadersInternal(readers, searchable);
		    }
		}
		else if ( obj is IndexSearcher ) {
			GetIndexReadersInternal( readers, ( (IndexSearcher) obj ).GetIndexReader() );
		}
		else if ( obj is IndexReader ) {
			readers.Add( (IndexReader) obj );
		}
	}
    }
}