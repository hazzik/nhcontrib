using System;
using System.Collections;
using Lucene.Net.Index;

namespace NHibernate.Search.Backend.Configuration
{

    /// <summary>
    /// Represents possible options to be applied to an
    /// <seealso cref="IndexWriter"/>
    /// </summary>
    public class IndexWriterSetting
    {
        public delegate void ApplySettingDelegate(IndexWriter writer, int value);

        private readonly string cfgKey;
        private readonly ApplySettingDelegate applySettingDelegate;

        private IndexWriterSetting(string cfgKey, ApplySettingDelegate applySettingDelegate)
        {
            this.cfgKey = cfgKey;
            this.applySettingDelegate = applySettingDelegate;
        }

        public string Key
        {
            get { return cfgKey; }
        }

        public static IndexWriterSetting[] Values
        {
            get
            {
                return new IndexWriterSetting[]
                           {
                               MaxBufferedDocs,
                               MaxFieldLength,
                               MaxMergeDocs,
                               MergeFactor,
                               UseCompoundFile,
                               TermIndexInterval
                           };
            }
        }

        public void ApplySetting(IndexWriter writer, int value)
        {
            applySettingDelegate(writer, value);
        }

        // SetMaxBufferedDeleteTerms is not implemented in lucene
        // TODO: see if we can fix this on next lucene update
        //public static IndexWriterSetting MaxBufferedDeleteTerms =
        //    new IndexWriterSetting(delegate(IndexWriter writer, int value)
        //    {
        //        writer.SetMaxBufferedDeleteTerms(value);
        //    });
        //
        //public static IndexWriterSetting RamBufferSize =
        //    new IndexWriterSetting(delegate(IndexWriter writer, int value)
        //    {
        //        writer.SetRamBufferSize(value);
        //    });

        /// <summary>
        /// <seealso cref="IndexWriter.SetMaxBufferedDocs"/>
        /// </summary>
        public static IndexWriterSetting MaxBufferedDocs =
            new IndexWriterSetting("max_buffered_docs", delegate(IndexWriter writer, int value)
            {
                writer.SetMaxBufferedDocs(value);
            });

        /// <summary>
        /// <seealso cref="IndexWriter.SetMaxFieldLength"/>
        /// </summary>
        public static IndexWriterSetting MaxFieldLength =
            new IndexWriterSetting("max_field_length", delegate(IndexWriter writer, int value)
            {
                writer.SetMaxFieldLength(value);
            });

        /// <summary>
        /// <seealso cref="IndexWriter.SetMaxMergeDocs"/>
        /// </summary>
        public static IndexWriterSetting MaxMergeDocs =
            new IndexWriterSetting("max_merge_docs", delegate(IndexWriter writer, int value)
            {
                writer.SetMaxMergeDocs(value);
            });

        /// <summary>
        /// <seealso cref="IndexWriter.SetMergeFactor"/>
        /// </summary>
        public static IndexWriterSetting MergeFactor =
            new IndexWriterSetting("merge_factor", delegate(IndexWriter writer, int value)
            {
                writer.SetMergeFactor(value);
            });

        /// <summary>
        /// <seealso cref="IndexWriter.SetUseCompoundFile"/>
        /// </summary>
        public static IndexWriterSetting UseCompoundFile =
            new IndexWriterSetting("use_compound_file", delegate(IndexWriter writer, int value)
            {
                writer.SetUseCompoundFile(value == 1);
            });

        /// <summary>
        /// <seealso cref="IndexWriter.SetTermIndexInterval"/>
        /// </summary>
        public static IndexWriterSetting TermIndexInterval =
            new IndexWriterSetting("term_index_interval", delegate(IndexWriter writer, int value)
            {
                writer.SetTermIndexInterval(value);
            });

        /// <summary>
        /// Specific parameters may override to provide additional keywords support.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int ParseValue(String value)
        {
            bool? boolean = ConfigurationParseHelper.ParseBoolean(value,
                                                                  "Invalid value for " + cfgKey + ": " + value);
            if (boolean != null)
                return boolean.Value ? 1 : 0;
            return ConfigurationParseHelper.ParseInt(value,
                    "Invalid value for " + cfgKey + ": " + value);
        }
    }
}