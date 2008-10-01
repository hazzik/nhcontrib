using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Iesi.Collections.Generic;
using log4net;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using NHibernate.Search.Attributes;
using NHibernate.Search.Backend;
using NHibernate.Search.Bridge;
using NHibernate.Search.Impl;
using NHibernate.Search.Store;
using NHibernate.Search.Util;
using NHibernate.Util;
using FieldInfo = System.Reflection.FieldInfo;
using Lucene.Net.Search;

namespace NHibernate.Search.Engine
{
    /// <summary>
    /// Set up and provide a manager for indexes classes
    /// </summary>
    public class DocumentBuilder
    {
        public const string CLASS_FIELDNAME = "_hibernate_class";
        private static readonly ILog logger = LogManager.GetLogger(typeof(DocumentBuilder));

        private readonly PropertiesMetadata rootPropertiesMetadata;
        private readonly System.Type beanClass;
        private readonly InitContext context;
        private readonly IDirectoryProvider[] directoryProviders;
        private readonly IIndexShardingStrategy shardingStrategy;
        private String idKeywordName;
        private MemberInfo idGetter;
        private float? idBoost;
        private ITwoWayFieldBridge idBridge;
        private ISet<System.Type> mappedSubclasses = new HashedSet<System.Type>();
        private int level;
        private int maxLevel = int.MaxValue;
        private readonly ScopedAnalyzer analyzer;
        private readonly Similarity similarity;
        private bool isRoot;
        //if composite id, use of (a, b) in ((1,2), (3,4)) fails on most database
        private bool safeFromTupleId;
        private bool idProvided;


        #region Nested type: PropertiesMetadata

        private enum Container
        {
            Object,
            Collection,
            Map,
            Array
        }

        private class PropertiesMetadata
        {
            public readonly List<float> ClassBoosts = new List<float>();
            public readonly List<IFieldBridge> ClassBridges = new List<IFieldBridge>();
            public readonly List<Field.Index> ClassIndexes = new List<Field.Index>();
            public readonly List<string> ClassNames = new List<string>();
            public readonly List<Field.Store> ClassStores = new List<Field.Store>();
            public readonly List<TermVector> ClassTermVectors = new List<TermVector>();
            public readonly List<MemberInfo> ContainedInGetters = new List<MemberInfo>();
            public readonly List<Container> EmbeddedContainers = new List<Container>();
            public readonly List<MemberInfo> EmbeddedGetters = new List<MemberInfo>();
            public readonly List<PropertiesMetadata> EmbeddedPropertiesMetadata = new List<PropertiesMetadata>();
            public readonly List<IFieldBridge> FieldBridges = new List<IFieldBridge>();
            public readonly List<MemberInfo> FieldGetters = new List<MemberInfo>();
            public readonly List<Field.Index> FieldIndex = new List<Field.Index>();
            public readonly List<String> FieldNames = new List<String>();
            public readonly List<Field.Store> FieldStore = new List<Field.Store>();
            public readonly List<TermVector> FieldTermVectors = new List<TermVector>();
            public Analyzer Analyzer;
            public float? Boost;

            public LuceneOptions GetClassLuceneOptions(int i)
            {
                LuceneOptions options = new LuceneOptions(ClassStores[i],
                        ClassIndexes[i], ClassTermVectors[i], ClassBoosts[i]);
                return options;
            }

            public LuceneOptions GetFieldLuceneOptions(int i, float? boost)
            {
                LuceneOptions options = new LuceneOptions(FieldStore[i],
                        FieldIndex[i], FieldTermVectors[i], boost);
                return options;
            }
        }

        #endregion

        #region Constructors

        public DocumentBuilder(System.Type clazz, InitContext context, IDirectoryProvider[] directoryProviders,
                               IIndexShardingStrategy shardingStrategy)
        {
            analyzer = new ScopedAnalyzer();
            beanClass = clazz;
            this.context = context;
            this.directoryProviders = directoryProviders;
            this.shardingStrategy = shardingStrategy;
            similarity = context.DefaultSimilarity;

            if (clazz == null) throw new AssertionFailure("Unable to build a DocumemntBuilder with a null class");

            rootPropertiesMetadata = new PropertiesMetadata();
            rootPropertiesMetadata.Boost = GetBoost(clazz);
            rootPropertiesMetadata.Analyzer = context.DefaultAnalyzer;

            Set<System.Type> processedClasses = new HashedSet<System.Type>();
            processedClasses.Add(clazz);
            InitializeMembers(clazz, rootPropertiesMetadata, true, string.Empty, processedClasses);
            //processedClasses.remove( clazz ); for the sake of completness
            analyzer.GlobalAnalyzer = rootPropertiesMetadata.Analyzer;
            if (idKeywordName == null)
            {
                // if no DocumentId then check if we have a ProvidedId instead
                ProvidedIdAttribute provided = FindProvidedId(clazz);
                if (provided == null) throw new SearchException("No document id in: " + clazz.Name);

                idBridge = BridgeFactory.ExtractTwoWayType(provided.Bridge);
                idKeywordName = provided.Name;
            }
            //if composite id, use of (a, b) in ((1,2)TwoWayString2FieldBridgeAdaptor, (3,4)) fails on most database
            //a TwoWayString2FieldBridgeAdaptor is never a composite id
            safeFromTupleId = idBridge is TwoWayString2FieldBridgeAdaptor;
        }

        private static ProvidedIdAttribute FindProvidedId(ICustomAttributeProvider clazz)
        {
            object[] attributes = clazz.GetCustomAttributes(typeof(ProvidedIdAttribute), true);
            if (attributes.Length == 0)
                return null;
            return (ProvidedIdAttribute)attributes[0];
        }

        #endregion

        #region Property methods

        public Similarity Similarity
        {
            get { return similarity; }
        }

        public bool SafeFromTupleId
        {
            get { return safeFromTupleId; }
        }

        public Analyzer Analyzer
        {
            get { return analyzer; }
        }

        public IDirectoryProvider[] DirectoryProviders
        {
            get { return directoryProviders; }
        }

        public IIndexShardingStrategy DirectoryProvidersSelectionStrategy
        {
            get { return shardingStrategy; }
        }

        public ITwoWayFieldBridge IdBridge
        {
            get { return idBridge; }
        }

        public ISet<System.Type> MappedSubclasses
        {
            get { return mappedSubclasses; }
        }

        public bool IsRoot
        {
            get { return isRoot; }
        }

        public string IdentifierName
        {
            get { return idGetter.Name; }
        }

        #endregion

        #region Private methods

        private void BindClassAnnotation(string prefix, PropertiesMetadata propertiesMetadata, ClassBridgeAttribute ann)
        {
            // TODO: Name should be prefixed - NH is this still true?
            string fieldName = prefix + ann.Name;
            propertiesMetadata.ClassNames.Add(fieldName);
            propertiesMetadata.ClassStores.Add(GetStore(ann.Store));
            propertiesMetadata.ClassIndexes.Add(GetIndex(ann.Index));
            propertiesMetadata.ClassBridges.Add(BridgeFactory.ExtractType(ann));
            propertiesMetadata.ClassBoosts.Add(ann.Boost);

            Analyzer classAnalyzer = GetAnalyzer(ann.Analyzer) ?? propertiesMetadata.Analyzer;
            if (classAnalyzer == null)
                throw new NotSupportedException("Analyzer should not be undefined");

            analyzer.AddScopedAnalyzer(fieldName, classAnalyzer);
        }

        private void BindFieldAnnotation(MemberInfo member, PropertiesMetadata propertiesMetadata, string prefix,
                                         FieldAttribute fieldAnn)
        {
            SetAccessible(member);
            propertiesMetadata.FieldGetters.Add(member);
            string fieldName = prefix + BinderHelper.GetAttributeName(member, fieldAnn.Name);
            propertiesMetadata.FieldNames.Add(prefix + fieldAnn.Name);
            propertiesMetadata.FieldStore.Add(GetStore(fieldAnn.Store));
            propertiesMetadata.FieldIndex.Add(GetIndex(fieldAnn.Index));
            propertiesMetadata.FieldBridges.Add(BridgeFactory.GuessType(member));

            // Field > property > entity Analyzer
            Analyzer localAnalyzer = (GetAnalyzer(fieldAnn.Analyzer) ?? GetAnalyzer(member)) ??
                                     propertiesMetadata.Analyzer;
            if (localAnalyzer == null)
                throw new NotSupportedException("Analyzer should not be undefined");

            analyzer.AddScopedAnalyzer(fieldName, localAnalyzer);
        }

        private static void BuildDocumentFields(Object instance, Document doc, PropertiesMetadata propertiesMetadata)
        {
            if (instance == null) return;

            object unproxiedInstance = Unproxy(instance);

            for (int i = 0; i < propertiesMetadata.ClassBridges.Count; i++)
            {
                IFieldBridge fb = propertiesMetadata.ClassBridges[i];


                fb.Set(propertiesMetadata.ClassNames[i],
                       unproxiedInstance,
                       doc,
                       propertiesMetadata.GetClassLuceneOptions(i));

            }

            for (int i = 0; i < propertiesMetadata.FieldNames.Count; i++)
            {

                MemberInfo member = propertiesMetadata.FieldGetters[i];
                Object value = GetMemberValue(unproxiedInstance, member);
                propertiesMetadata.FieldBridges[i].Set(
                    propertiesMetadata.FieldNames[i],
                    value,
                    doc,
                    propertiesMetadata.GetFieldLuceneOptions(i,GetBoost(member)));
            }

            for (int i = 0; i < propertiesMetadata.EmbeddedGetters.Count; i++)
            {
                MemberInfo member = propertiesMetadata.EmbeddedGetters[i];
                Object value = GetMemberValue(unproxiedInstance, member);
                //if ( ! Hibernate.isInitialized( value ) ) continue; //this sounds like a bad idea 
                //TODO handle Boost at embedded level: already stored in propertiesMedatada.Boost

                if (value == null) continue;
                PropertiesMetadata embeddedMetadata = propertiesMetadata.EmbeddedPropertiesMetadata[i];

                switch (propertiesMetadata.EmbeddedContainers[i])
                {
                    case Container.Array:
                        foreach (object arrayValue in (Array)value)
                            BuildDocumentFields(arrayValue, doc, embeddedMetadata);
                        break;

                    case Container.Collection:
                        foreach (object collectionValue in (ICollection)value)
                            BuildDocumentFields(collectionValue, doc, embeddedMetadata);
                        break;

                    case Container.Map:
                        foreach (object collectionValue in ((IDictionary)value).Values)
                            BuildDocumentFields(collectionValue, doc, embeddedMetadata);
                        break;

                    case Container.Object:
                        BuildDocumentFields(value, doc, embeddedMetadata);
                        break;

                    default:
                        throw new NotSupportedException("Unknown embedded container: " +
                                                        propertiesMetadata.EmbeddedContainers[i]);
                }
            }
        }


        private static string BuildEmbeddedPrefix(string prefix, IndexedEmbeddedAttribute embeddedAnn, MemberInfo member)
        {
            string localPrefix = prefix;
            if (embeddedAnn.Prefix == ".")
                // Default to property name
                localPrefix += member.Name + ".";
            else
                localPrefix += embeddedAnn.Prefix;

            return localPrefix;
        }

        private Analyzer GetAnalyzer(ICustomAttributeProvider member)
        {
            AnalyzerAttribute attrib = AttributeUtil.GetAttribute<AnalyzerAttribute>(member);
            return GetAnalyzer(attrib);
        }

        private Analyzer GetAnalyzer(AnalyzerAttribute analyzerAtt)
        {
            if (analyzerAtt == null)
                return null;

            if (string.IsNullOrEmpty(analyzerAtt.Definition) == false)
                return context.BuildLazyAnalyzer(analyzerAtt.Definition);

            if (!typeof(Analyzer).IsAssignableFrom(analyzerAtt.Type))
                throw new SearchException("Lucene Analyzer not implemented by " + analyzerAtt.Type.FullName);

            try
            {
                return (Analyzer)Activator.CreateInstance(analyzerAtt.Type);
            }
            catch (Exception e)
            {
                throw new SearchException(
                    "Failed to instantiate lucene Analyzer with type  " + analyzerAtt.Type.FullName, e);
            }
        }

        private static float? GetBoost(ICustomAttributeProvider element)
        {
            if (element == null) return null;
            BoostAttribute boost = AttributeUtil.GetAttribute<BoostAttribute>(element);
            if (boost == null)
                return null;
            return boost.Value;
        }

        private static int GetFieldPosition(string[] fields, string fieldName)
        {
            int fieldNbr = fields.GetUpperBound(0);
            for (int index = 0; index < fieldNbr; index++)
            {
                if (fieldName.Equals(fields[index])) return index;
            }
            return -1;
        }

        private static Field.Index GetIndex(Index index)
        {
            switch (index)
            {
                case Index.No:
                    return Field.Index.NO;
                case Index.NoNorms:
                    return Field.Index.NO_NORMS;
                case Index.Tokenized:
                    return Field.Index.TOKENIZED;
                case Index.UnTokenized:
                    return Field.Index.UN_TOKENIZED;
                default:
                    throw new AssertionFailure("Unexpected Index: " + index);
            }
        }

        private static object GetMemberValue(Object instance, MemberInfo getter)
        {
            PropertyInfo info = getter as PropertyInfo;
            return info != null ? info.GetValue(instance, null) : ((FieldInfo)getter).GetValue(instance);
        }

        private static System.Type GetMemberType(MemberInfo member)
        {
            PropertyInfo info = member as PropertyInfo;
            return info != null ? info.PropertyType : ((FieldInfo)member).FieldType;
        }

        private static Field.Store GetStore(Attributes.Store store)
        {
            switch (store)
            {
                case Attributes.Store.No:
                    return Field.Store.NO;
                case Attributes.Store.Yes:
                    return Field.Store.YES;
                case Attributes.Store.Compress:
                    return Field.Store.COMPRESS;
                default:
                    throw new AssertionFailure("Unexpected Store: " + store);
            }
        }

        private static Field.TermVector GetTermVector(TermVector vector)
        {
            switch (vector)
            {
                case TermVector.No:
                    return Field.TermVector.NO;
                case TermVector.Yes:
                    return Field.TermVector.YES;
                case TermVector.WithOffsets:
                    return Field.TermVector.WITH_OFFSETS;
                case TermVector.WithPositions:
                    return Field.TermVector.WITH_POSITIONS;
                case TermVector.WithPositionOffsets:
                    return Field.TermVector.WITH_POSITIONS_OFFSETS;
                default:
                    throw new AssertionFailure("Unexpected TermVector: " + vector);
            }
        }

        private void InitializeMember(
            MemberInfo member, PropertiesMetadata propertiesMetadata, bool isRoot,
            String prefix, ISet<System.Type> processedClasses)
        {
            DocumentIdAttribute documentIdAnn = AttributeUtil.GetDocumentId(member);
            if (documentIdAnn != null)
            {
                if (isRoot)
                {
                    if (idKeywordName != null)
                        if (documentIdAnn.Name != null)
                            throw new AssertionFailure("Two document id assigned: "
                                                       + idKeywordName + " and " + documentIdAnn.Name);
                    idKeywordName = prefix + documentIdAnn.Name;
                    IFieldBridge fieldBridge = BridgeFactory.GuessType(member);
                    if (fieldBridge is ITwoWayFieldBridge)
                        idBridge = (ITwoWayFieldBridge)fieldBridge;
                    else
                        throw new SearchException(
                            "Bridge for document id does not implement IdFieldBridge: " + member.Name);
                    idBoost = GetBoost(member);
                    idGetter = member;
                }
                else
                {
                    // Component should index their document id
                    SetAccessible(member);
                    propertiesMetadata.FieldGetters.Add(member);
                    string fieldName = prefix + BinderHelper.GetAttributeName(member, documentIdAnn.Name);
                    propertiesMetadata.FieldNames.Add(fieldName);
                    propertiesMetadata.FieldStore.Add(GetStore(Attributes.Store.Yes));
                    propertiesMetadata.FieldIndex.Add(GetIndex(Index.UnTokenized));
                    propertiesMetadata.FieldBridges.Add(BridgeFactory.GuessType(member));

                    // Property > entity Analyzer - no field Analyzer
                    Analyzer memberAnalyzer = GetAnalyzer(member) ?? propertiesMetadata.Analyzer;
                    if (memberAnalyzer == null)
                        throw new NotSupportedException("Analyzer should not be undefined");

                    analyzer.AddScopedAnalyzer(fieldName, memberAnalyzer);
                }
            }

            List<FieldAttribute> fieldAttributes = AttributeUtil.GetFields(member);
            if (fieldAttributes != null)
            {
                foreach (FieldAttribute fieldAnn in fieldAttributes)
                    BindFieldAnnotation(member, propertiesMetadata, prefix, fieldAnn);
            }
            GetAnalyzerDefs(member);

            IndexedEmbeddedAttribute embeddedAttribute = AttributeUtil.GetAttribute<IndexedEmbeddedAttribute>(member);
            if (embeddedAttribute != null)
            {
                int oldMaxLevel = maxLevel;
                int potentialLevel = embeddedAttribute.Depth + level;
                if (potentialLevel < 0)
                    potentialLevel = int.MaxValue;

                maxLevel = potentialLevel > maxLevel ? maxLevel : potentialLevel;
                level++;

                System.Type elementType = embeddedAttribute.TargetElement ?? GetMemberType(member);

                if (maxLevel == int.MaxValue && processedClasses.Contains(elementType))
                    throw new SearchException(
                        string.Format("Circular reference, Duplicate use of {0} in root entity {1}#{2}",
                                      elementType.FullName, beanClass.FullName,
                                      BuildEmbeddedPrefix(prefix, embeddedAttribute, member)));

                if (level <= maxLevel)
                {
                    processedClasses.Add(elementType); // push

                    SetAccessible(member);
                    propertiesMetadata.EmbeddedGetters.Add(member);
                    PropertiesMetadata metadata = new PropertiesMetadata();
                    propertiesMetadata.EmbeddedPropertiesMetadata.Add(metadata);
                    metadata.Boost = GetBoost(member);
                    // property > entity Analyzer
                    metadata.Analyzer = GetAnalyzer(member) ?? propertiesMetadata.Analyzer;
                    string localPrefix = BuildEmbeddedPrefix(prefix, embeddedAttribute, member);
                    InitializeMembers(elementType, metadata, false, localPrefix, processedClasses);
                    /**
                     * We will only index the "expected" type but that's OK, HQL cannot do downcasting either
                     */
                    if (elementType.IsArray)
                        propertiesMetadata.EmbeddedContainers.Add(Container.Array);
                    else if (typeof(ICollection).IsAssignableFrom(elementType))
                    {
                        // TODO: Check this will cope with ISet and/or subclasses of IList/IDictionary correctly
                        if (typeof(IDictionary).IsAssignableFrom(elementType))
                            propertiesMetadata.EmbeddedContainers.Add(Container.Map);
                        else
                            propertiesMetadata.EmbeddedContainers.Add(Container.Collection);
                    }
                    else
                        propertiesMetadata.EmbeddedContainers.Add(Container.Object);
                }
                else if (logger.IsDebugEnabled)
                {
                    string localPrefix = BuildEmbeddedPrefix(prefix, embeddedAttribute, member);
                    logger.Debug("Depth reached, ignoring " + localPrefix);
                }

                level--;
                maxLevel = oldMaxLevel; // set back the old max level
            }

            ContainedInAttribute containedInAttribute = AttributeUtil.GetAttribute<ContainedInAttribute>(member);
            if (containedInAttribute != null)
            {
                SetAccessible(member);
                propertiesMetadata.ContainedInGetters.Add(member);
            }
        }


        private void GetAnalyzerDefs(ICustomAttributeProvider annotatedElement)
        {
            AnalyzerDefAttribute[] defs =
                (AnalyzerDefAttribute[])annotatedElement.GetCustomAttributes(typeof(AnalyzerDefAttribute), true);
            foreach (AnalyzerDefAttribute def in defs)
            {
                context.AddAnalyzerDef(def);
            }
        }

        private void InitializeMembers(
            System.Type clazz, PropertiesMetadata propertiesMetadata, bool isRoot, String prefix,
            ISet<System.Type> processedClasses)
        {
            IList<System.Type> hierarchy = new List<System.Type>();
            System.Type currClass = clazz;
            do
            {
                hierarchy.Add(currClass);
                currClass = currClass.BaseType;
                // NB Java stops at null we stop at object otherwise we process the class twice
                // We also need a null test for things like ISet which have no base class/interface
            } while (currClass != null && currClass != typeof(object));

            for (int index = hierarchy.Count - 1; index >= 0; index--)
            {
                currClass = hierarchy[index];
                /**
                 * Override the default Analyzer for the properties if the class hold one
                 * That's the reason we go down the hierarchy
                 */

                // NB Must cast here as we want to look at the type's metadata
                Analyzer localAnalyzer = GetAnalyzer(currClass as MemberInfo);
                if (localAnalyzer != null)
                    propertiesMetadata.Analyzer = localAnalyzer;

                // Check for any ClassBridges
                List<ClassBridgeAttribute> classBridgeAnn = AttributeUtil.GetClassBridges(currClass);
                if (classBridgeAnn != null)
                {
                    // Ok, pick up the parameters as well
                    AttributeUtil.GetClassBridgeParameters(currClass, classBridgeAnn);

                    // Now we can process the class bridges
                    foreach (ClassBridgeAttribute cb in classBridgeAnn)
                        BindClassAnnotation(prefix, propertiesMetadata, cb);
                }

                // NB As we are walking the hierarchy only retrieve items at this level
                PropertyInfo[] propertyInfos =
                    currClass.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public |
                                            BindingFlags.Instance);
                foreach (PropertyInfo propertyInfo in propertyInfos)
                    InitializeMember(propertyInfo, propertiesMetadata, isRoot, prefix, processedClasses);

                FieldInfo[] fields =
                    clazz.GetFields(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public |
                                    BindingFlags.Instance);
                foreach (FieldInfo fieldInfo in fields)
                    InitializeMember(fieldInfo, propertiesMetadata, isRoot, prefix, processedClasses);
            }
        }


        private static void ProcessFieldsForProjection(PropertiesMetadata metadata, String[] fields, Object[] result,
                                                       Document document)
        {
            int nbrFoEntityFields = metadata.FieldNames.Count;
            for (int index = 0; index < nbrFoEntityFields; index++)
            {
                PopulateResult(metadata.FieldNames[index],
                               metadata.FieldBridges[index],
                               metadata.FieldStore[index],
                               fields,
                               result,
                               document
                    );
            }
            int nbrOfEmbeddedObjects = metadata.EmbeddedPropertiesMetadata.Count;
            for (int index = 0; index < nbrOfEmbeddedObjects; index++)
            {
                //there is nothing we can do for collections
                if (metadata.EmbeddedContainers[index] != Container.Object)
                    continue;

                ProcessFieldsForProjection(metadata.EmbeddedPropertiesMetadata[index], fields, result, document);
            }
        }

        private static void PopulateResult(string fieldName, IFieldBridge fieldBridge, Field.Store store,
                                           string[] fields, object[] result, Document document)
        {
            int matchingPosition = GetFieldPosition(fields, fieldName);
            if (matchingPosition == -1)
                return;

            if (store == Field.Store.NO)
                throw new SearchException("Projecting an unstored field: " + fieldName);
            if ((fieldBridge is ITwoWayFieldBridge) == false)
                throw new SearchException("IFieldBridge is not a ITwoWayFieldBridge: " + fieldBridge.GetType());

            result[matchingPosition] = ((ITwoWayFieldBridge)fieldBridge).Get(fieldName, document);
            if (logger.IsInfoEnabled)
            {
                logger.InfoFormat("Field {0} projected as {1}", fieldName, result[matchingPosition]);
            }
        }

        private static void ProcessContainedIn(Object instance, List<LuceneWork> queue, PropertiesMetadata metadata,
                                               ISearchFactoryImplementor searchFactory)
        {
            for (int i = 0; i < metadata.ContainedInGetters.Count; i++)
            {
                MemberInfo member = metadata.ContainedInGetters[i];
                object value = GetMemberValue(instance, member);

                if (value == null) continue;

                Array array = value as Array;
                if (array != null)
                {
                    foreach (object arrayValue in array)
                    {
                        // Highly inneficient but safe wrt the actual targeted class, e.g. polymorphic items in the array
                        System.Type valueType = NHibernateUtil.GetClass(arrayValue);
                        if (valueType == null || !searchFactory.DocumentBuilders.ContainsKey(valueType))
                            continue;

                        ProcessContainedInValue(arrayValue, queue, valueType, searchFactory.DocumentBuilders[valueType],
                                                searchFactory);
                    }
                }
                else if (value is ICollection)
                {
                    ICollection collection = value as ICollection;
                    if (value is IDictionary)
                        collection = ((IDictionary)value).Values;

                    if (collection == null)
                        continue;

                    foreach (object collectionValue in collection)
                    {
                        // Highly inneficient but safe wrt the actual targeted class, e.g. polymorphic items in the array
                        System.Type valueType = NHibernateUtil.GetClass(collectionValue);
                        if (valueType == null || !searchFactory.DocumentBuilders.ContainsKey(valueType))
                            continue;

                        ProcessContainedInValue(collectionValue, queue, valueType,
                                                searchFactory.DocumentBuilders[valueType], searchFactory);
                    }
                }
                else
                {
                    System.Type valueType = NHibernateUtil.GetClass(value);
                    if (valueType == null || !searchFactory.DocumentBuilders.ContainsKey(valueType))
                        continue;

                    ProcessContainedInValue(value, queue, valueType, searchFactory.DocumentBuilders[valueType],
                                            searchFactory);
                }
            }
            //an embedded cannot have a useful @ContainedIn (no shared reference)
            //do not walk through them
        }

        private static void ProcessContainedInValue(object value, List<LuceneWork> queue, System.Type valueClass,
                                                    DocumentBuilder builder, ISearchFactoryImplementor searchFactory)
        {
            object id = GetMemberValue(value, builder.idGetter);
            builder.AddToWorkQueue(valueClass, value, id, WorkType.Update, queue, searchFactory);
        }

        private static void SetAccessible(MemberInfo member)
        {
            // NB Not sure we need to do anything for C#
        }

        private static object Unproxy(object value)
        {
            // NB Not sure if we need to do anything for C#
            //return NHibernateUtil.Unproxy(value);
            return value;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// This add the new work to the queue, so it can be processed in a batch fashion later
        /// </summary>
        public void AddToWorkQueue(System.Type entityClass, object entity, object id, WorkType workType,
                                   List<LuceneWork> queue,
                                   ISearchFactoryImplementor searchFactory)
        {
            //TODO with the caller loop we are in a n^2: optimize it using a HashMap for work recognition
            foreach (LuceneWork luceneWork in queue)
            {
                if (luceneWork.EntityClass == entityClass && luceneWork.Id.Equals(id))
                    return;
            }
            bool searchForContainers = false;
            string idString = idBridge.ObjectToString(id);

            switch (workType)
            {
                case WorkType.Add:
                    queue.Add(new AddLuceneWork(id, idString, entityClass, GetDocument(entity, id)));
                    searchForContainers = true;
                    break;

                case WorkType.Delete:
                case WorkType.Purge:
                    queue.Add(new DeleteLuceneWork(id, idString, entityClass));
                    break;

                case WorkType.PurgeAll:
                    queue.Add(new PurgeAllLuceneWork(entityClass));
                    break;

                case WorkType.Update:
                case WorkType.Collection:
                    /**
                     * even with Lucene 2.1, use of indexWriter to update is not an option
                     * We can only delete by term, and the index doesn't have a term that
                     * uniquely identify the entry.
                     * But essentially the optimization we are doing is the same Lucene is doing, the only extra cost is the
                     * double file opening.
                    */
                    queue.Add(new DeleteLuceneWork(id, idString, entityClass));
                    queue.Add(new AddLuceneWork(id, idString, entityClass, GetDocument(entity, id)));
                    searchForContainers = true;
                    break;

                case WorkType.Index:
                    queue.Add(new DeleteLuceneWork(id, idString, entityClass));
                    LuceneWork work = new AddLuceneWork(id, idString, entityClass, GetDocument(entity, id));
                    work.IsBatch = true;
                    queue.Add(work);
                    searchForContainers = true;
                    break;

                default:
                    throw new AssertionFailure("Unknown WorkType: " + workType);
            }

            /**
		     * When references are changed, either null or another one, we expect dirty checking to be triggered (both sides
		     * have to be updated)
		     * When the internal object is changed, we apply the {Add|Update}Work on containedIns
		    */
            if (searchForContainers)
                ProcessContainedIn(entity, queue, rootPropertiesMetadata, searchFactory);
        }

        public Document GetDocument(object instance, object id)
        {
            Document doc = new Document();
            System.Type instanceClass = instance.GetType();
            if (rootPropertiesMetadata.Boost != null)
                doc.SetBoost(rootPropertiesMetadata.Boost.Value);
            Field classField =
                new Field(CLASS_FIELDNAME, TypeHelper.LuceneTypeName(instanceClass), Field.Store.YES,
                          Field.Index.UN_TOKENIZED);
            doc.Add(classField);
            LuceneOptions options = new LuceneOptions(Field.Store.YES, Field.Index.UN_TOKENIZED, TermVector.No, idBoost);
            idBridge.Set(idKeywordName, id, doc, options);
            BuildDocumentFields(instance, doc, rootPropertiesMetadata);
            return doc;
        }

        public Term GetTerm(object id)
        {
            if (idProvided)
                return new Term(idKeywordName, (string)id);
            return new Term(idKeywordName, idBridge.ObjectToString(id));
        }

        public String GetIdKeywordName()
        {
            return idKeywordName;
        }

        public static System.Type GetDocumentClass(Document document)
        {
            String className = document.Get(CLASS_FIELDNAME);
            try
            {
                return ReflectHelper.ClassForName(className);
            }
            catch (Exception e)
            {
                throw new SearchException("Unable to load indexed class: " + className, e);
            }
        }

        public static object GetDocumentId(ISearchFactoryImplementor searchFactory, System.Type clazz, Document document)
        {
            DocumentBuilder builder; ;
            if (searchFactory.DocumentBuilders.TryGetValue(clazz, out builder) == false)
                throw new SearchException("No Lucene configuration set up for: " + clazz.Name);
            return builder.IdBridge.Get(builder.GetIdKeywordName(), document);
        }

        public static object[] GetDocumentFields(ISearchFactoryImplementor searchFactoryImplementor, System.Type clazz,
                                                 Document document, string[] fields)
        {
            DocumentBuilder builder;
            if (!searchFactoryImplementor.DocumentBuilders.TryGetValue(clazz, out builder))
                throw new SearchException("No Lucene configuration set up for: " + clazz.Name);
            int fieldNbr = fields.GetUpperBound(0);
            object[] result = new Object[fieldNbr];

            if (builder.idKeywordName != null)
            {
                PopulateResult(builder.idKeywordName, builder.idBridge, Field.Store.YES, fields, result, document);
            }

            PropertiesMetadata metadata = builder.rootPropertiesMetadata;
            ProcessFieldsForProjection(metadata, fields, result, document);

            return result;
        }

        public void PostInitialize(ISet<System.Type> indexedClasses)
        {
            //this method does not requires synchronization
            System.Type plainClass = beanClass;
            ISet<System.Type> tempMappedSubclasses = new HashedSet<System.Type>();

            //together with the caller this creates a o(2), but I think it's still faster than create the up hierarchy for each class
            foreach (System.Type currentClass in indexedClasses)
                if (plainClass.IsAssignableFrom(currentClass))
                    tempMappedSubclasses.Add(currentClass);

            mappedSubclasses = tempMappedSubclasses;

            System.Type superClass = plainClass.BaseType;
            isRoot = true;
            while (superClass != null)
            {
                if (indexedClasses.Contains(superClass))
                {
                   isRoot = false;
                    break;
                }
                superClass = superClass.BaseType;
            }
        }

        #endregion
    }
}