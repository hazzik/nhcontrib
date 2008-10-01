using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using log4net;
using Lucene.Net.Index;
using NHibernate.Search.Backend.Configuration;

namespace NHibernate.Search.Backend
{
    /// <summary>
    /// Wrapper class around the Lucene indexing parameters <i>mergeFactor</i>, <i>maxMergeDocs</i> and
    /// <i>maxBufferedDocs</i>.
    /// <p>
    /// There are two sets of these parameters. One is for regular indexing the other is for batch indexing
    /// triggered by <code>FullTextSessoin.Index(Object entity)</code>
    /// </summary>
    public class LuceneIndexingParameters
    {
        // property path keywords
        public const string BATCH = "batch";
        public const string ExplicitDefaultValue = "default";
        public const string PROP_GROUP = "indexwriter";
        private const long serialVersionUID = 5424606407623591663L;
        public const string TRANSACTION = "transaction";

        private readonly ParameterSet batchIndexParameters;
        private readonly ParameterSet transactionIndexParameters;

        public LuceneIndexingParameters(NameValueCollection sourceProps)
        {
            //prefer keys under "indexwriter" but fallback for backwards compatibility:
            MaskedProperty indexingParameters = new MaskedProperty(sourceProps, PROP_GROUP, sourceProps);
            //get keys for "transaction"
            MaskedProperty transactionProps = new MaskedProperty(indexingParameters.ToProperties(), TRANSACTION);
            //get keys for "batch" (defaulting to transaction)

            //TODO to close HSEARCH-201 just remove 3° parameter
            MaskedProperty batchProps = new MaskedProperty(indexingParameters.ToProperties(), BATCH,
                                                           transactionProps.ToProperties());
            //logger only used during object construction: (logger not serializable).
            ILog log = LogManager.GetLogger(typeof(LuceneIndexingParameters));
            transactionIndexParameters = new ParameterSet(transactionProps.ToProperties(), TRANSACTION, log);
            batchIndexParameters = new ParameterSet(batchProps.ToProperties(), BATCH, log);
            DoSanityChecks(transactionIndexParameters, batchIndexParameters, log);
        }

        public ParameterSet TransactionIndexParameters
        {
            get { return transactionIndexParameters; }
        }

        public ParameterSet BatchIndexParameters
        {
            get { return batchIndexParameters; }
        }

        private static void DoSanityChecks(ParameterSet transParams, ParameterSet batchParams, ILog log)
        {
            if (!log.IsWarnEnabled)
                return;
            int? maxFieldLengthTransaction = transParams[IndexWriterSetting.MaxFieldLength];
            int? maxFieldLengthBatch = batchParams[IndexWriterSetting.MaxFieldLength];
            if (maxFieldLengthTransaction != maxFieldLengthBatch)
            {
                log.Warn(
                    "The max_field_length value configured for transaction is different than the value configured for batch.");
            }
        }

        public void ApplyToWriter(IndexWriter writer, bool batch)
        {
            if (batch)
            {
                BatchIndexParameters.ApplyToWriter(writer);
            }
            else
            {
                TransactionIndexParameters.ApplyToWriter(writer);
            }
        }

        #region Nested type: ParameterSet

        public class ParameterSet
        {
            private const long serialVersionUID = -6121723702279869524L;

            private readonly Dictionary<IndexWriterSetting, int> parameters = new Dictionary<IndexWriterSetting, int>();


            public ParameterSet(NameValueCollection prop, String paramName, ILog log)
            {
                //don't iterate on property entries as we know all the keys:
                foreach (IndexWriterSetting indexWriterSetting in IndexWriterSetting.Values)
                {
                    string key = indexWriterSetting.Key;
                    string value = prop[key];
                    if (value == null || ExplicitDefaultValue.Equals(value, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (log.IsDebugEnabled)
                        {
                            //TODO add DirectoryProvider name when available to log message
                            log.Debug("Set indexwriter parameter " + paramName + "." + key + " to value : " + value);
                        }
                        parameters.Add(indexWriterSetting, indexWriterSetting.ParseValue(value));
                    }
                }
            }

            /**
         * Applies the parameters represented by this to a writer.
         * Undefined parameters are not set, leaving the lucene default.
         * @param writer the IndexWriter whereto the parameters will be applied.
         */

            public int? this[IndexWriterSetting ws]
            {
                get { return parameters[ws]; }
                set
                {
                    if (value == null)
                    {
                        parameters.Remove(ws);
                    }
                    else
                    {
                        parameters[ws] = value.Value;
                    }
                }
            }

            public void ApplyToWriter(IndexWriter writer)
            {
                foreach (KeyValuePair<IndexWriterSetting, int> entry in parameters)
                {
                    try
                    {
                        entry.Key.ApplySetting(writer, entry.Value);
                    }
                    catch (ArgumentException e)
                    {
                        //TODO if DirectoryProvider had getDirectoryName() exceptions could tell better
                        throw new SearchException("Illegal IndexWriter setting "
                                                  + entry.Key.Key + " " + e.Message, e);
                    }
                }
            }

            public override int GetHashCode()
            {
                const int prime = 31;
                int result = 1;
                result = prime * result
                         + ((parameters == null) ? 0 : parameters.GetHashCode());
                return result;
            }

            public override bool Equals(object obj)
            {
                if (this == obj)
                    return true;
                if (obj == null)
                    return false;
                if (GetType() != obj.GetType())
                    return false;
                ParameterSet other = (ParameterSet)obj;
                if (parameters == null)
                {
                    if (other.parameters != null)
                        return false;
                }
                else if (!parameters.Equals(other.parameters))
                    return false;
                return true;
            }
        }

        #endregion
    }
}