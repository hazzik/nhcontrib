using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Search;
using NHibernate.Search.Attributes;
using NHibernate.Search.Cfg;
using NHibernate.Search.Util;
using NHibernate.Util;

namespace NHibernate.Search.Impl
{
    public class InitContext {
	private readonly IDictionary<String, AnalyzerDefAttribute> analyzerDefs = new Dictionary<String, AnalyzerDefAttribute>();
	private readonly IList<DelegateNamedAnalyzer> lazyAnalyzers = new List<DelegateNamedAnalyzer>();
	private readonly Analyzer defaultAnalyzer;
	private readonly Similarity defaultSimilarity;

	public InitContext(INHSConfiguration cfg) {
		defaultAnalyzer = InitAnalyzer(cfg);
		defaultSimilarity = InitSimilarity(cfg);
	}

	public void AddAnalyzerDef(AnalyzerDefAttribute ann) {
		//FIXME somehow remember where the analyzerDef comes from and raise an exception if an analyzerDef
		//with the same name from two different places are added
		//multiple adding from the same place is required to deal with inheritance hierarchy processed multiple times
		if ( ann != null ) {
		    analyzerDefs.Add(ann.Name, ann);
		}
	}

	public Analyzer BuildLazyAnalyzer(String name) {
		 DelegateNamedAnalyzer delegateNamedAnalyzer = new DelegateNamedAnalyzer( name );
		lazyAnalyzers.Add(delegateNamedAnalyzer);
		return delegateNamedAnalyzer;
	}

        public IList<DelegateNamedAnalyzer> LazyAnalyzers
        {
            get { return lazyAnalyzers; }
        }

        /**
	 * Initializes the Lucene analyzer to use by reading the analyzer class from the configuration and instantiating it.
	 *
	 * @param cfg
	 *            The current configuration.
	 * @return The Lucene analyzer to use for tokenisation.
	 */
	private Analyzer InitAnalyzer(INHSConfiguration cfg) {
		System.Type analyzerClass;
		String analyzerClassName = cfg.GetProperty( Environment.AnalyzerClass);
		if (analyzerClassName != null) {
			try {
				analyzerClass = ReflectHelper.ClassForName(analyzerClassName);
			} catch (Exception )
			{
			    return BuildLazyAnalyzer(analyzerClassName);
			}
		} else {
			analyzerClass = typeof(StandardAnalyzer);
		}
		// Initialize analyzer
		Analyzer defaultAnalyzer;
		try {
		    defaultAnalyzer = (Analyzer) Activator.CreateInstance(analyzerClass);
		
		} catch (Exception e) {
			throw new SearchException("Failed to instantiate lucene analyzer with type " + analyzerClassName, e);
		}
		return defaultAnalyzer;
	}

	/**
	 * Initializes the Lucene similarity to use
	 */
	private Similarity InitSimilarity(INHSConfiguration cfg) {
		System.Type similarityClass;
		String similarityClassName = cfg.GetProperty(Environment.SimilarityClass);
		if (similarityClassName != null) {
			try {
				similarityClass = ReflectHelper.ClassForName(similarityClassName);
			} catch (Exception e) {
				throw new SearchException("Lucene Similarity class '" + similarityClassName + "' defined in property '"
						+ Environment.SimilarityClass+ "' could not be found.", e);
			}
		}
		else {
			similarityClass = null;
		}

		// Initialize similarity
		if ( similarityClass == null ) {
		    return new DefaultSimilarity();
		}
	    Similarity defaultSimilarity;
	    try {
	        defaultSimilarity = (Similarity) Activator.CreateInstance(similarityClass);
	    } catch (Exception e) {
	        throw new SearchException("Failed to instantiate lucene similarity with type " + similarityClassName, e);
	    }
	    return defaultSimilarity;
	}

        public Analyzer DefaultAnalyzer
        {
            get { return defaultAnalyzer; }
        }

        public Similarity DefaultSimilarity
        {
            get { return defaultSimilarity; }
        }

        public IDictionary<String, Analyzer> InitLazyAnalyzers() {
		IDictionary<String, Analyzer> initializedAnalyzers = new Dictionary<String, Analyzer>( analyzerDefs.Count );


            foreach (DelegateNamedAnalyzer namedAnalyzer in lazyAnalyzers)
            {
                String name = namedAnalyzer.Name;
			if ( initializedAnalyzers.ContainsKey( name ) ) {
				namedAnalyzer.Inner =  initializedAnalyzers[ name ];
			}
			else {
				if ( analyzerDefs.ContainsKey( name ) ) {
					Analyzer analyzer = BuildAnalyzer( analyzerDefs[ name ]);
					namedAnalyzer.Inner=  analyzer ;
					initializedAnalyzers.Add( name, analyzer );
				}
				else {
					throw new SearchException("Analyzer found with an unknown definition: " + name);
				}
			}
            }
            foreach (KeyValuePair<string, AnalyzerDefAttribute> entry in analyzerDefs)
            {
                if ( ! initializedAnalyzers.ContainsKey( entry.Key ) ) {
				Analyzer analyzer = BuildAnalyzer( entry.Value );
				initializedAnalyzers.Add( entry.Key, analyzer );
			}
            }
		return initializedAnalyzers;
	}

	private Analyzer BuildAnalyzer(AnalyzerDefAttribute analyzerDef) 
    {
	    throw new NotSupportedException("solr analyzers are not supported");
	}

	private bool IsPresent(String classname) {
		try {
            ReflectHelper.ClassForName(classname);
			return true;
		}
		catch ( Exception e ) {
			return false;
		}
	}
}

}