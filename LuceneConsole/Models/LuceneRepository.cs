using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Version = Lucene.Net.Util.Version;

namespace LuceneConsole.Models
{
    public static class LuceneRepository<T> where T : class, IIndexable
    {
        private static readonly string LuceneDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lucene_index");

        private static FSDirectory DirectoryTemp;

        private static FSDirectory Directory
        {
            get
            {
                if (DirectoryTemp == null)
                {
                    DirectoryTemp = FSDirectory.Open(new DirectoryInfo(Path.Combine(LuceneDir, typeof(T).ToString())));
                }
                if (IndexWriter.IsLocked(DirectoryTemp))
                {
                    IndexWriter.Unlock(DirectoryTemp);
                }
                var lockFilePath = Path.Combine(LuceneDir, "write.lock");
                if (File.Exists(lockFilePath))
                {
                    File.Delete(lockFilePath);
                }
                return DirectoryTemp;
            }
        }

        public static IEnumerable<T> Search(string input, string fieldName = "")
        {
            if (string.IsNullOrEmpty(input))
            {
                return new List<T>();
            }

            var terms = input.Trim()
                             .Replace("-", " ")
                             .Split(' ')
                             .Where(x => !string.IsNullOrEmpty(x))
                             .Select(x => x.Trim());

            input = string.Join(" ", terms);

            return _search(input, fieldName).Select(i => (Activator.CreateInstance(typeof(T), i)) as T);
        }

        public static void AddUpdateLuceneIndex(T item)
        {
            var objType = typeof(T);
            var properties = objType.GetProperties();
            if (!properties.Any(i => i.Name.Equals("Id")))
            {
                throw new Exception("Object cannot index because it dosen't have Id field");
            }

            var analyzer = new StandardAnalyzer(Version.LUCENE_30);
            try
            {
                using (var writer = new IndexWriter(Directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    DeleteOldDocument(item, properties, writer);

                    var doc = item.ToDocument();

                    writer.AddDocument(doc);


                    analyzer.Close();
                    writer.Dispose();
                }
            }
            catch (Exception ex)
            {

                throw new Exception(item.GetType().ToString(), ex);
            }
        }

        public static bool ClearLuceneIndex()
        {
            try
            {
                var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                using (var writer = new IndexWriter(Directory, analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    // remove older index entries
                    writer.DeleteAll();

                    // close handles
                    analyzer.Close();
                    writer.Dispose();
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public static void ClearLuceneIndexRecord(int recordId)
        {
            // init lucene
            var analyzer = new StandardAnalyzer(Version.LUCENE_30);
            using (var writer = new IndexWriter(Directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                // remove older index entry
                var searchQuery = new TermQuery(new Term("Id", recordId.ToString()));
                writer.DeleteDocuments(searchQuery);

                // close handles
                analyzer.Close();
                writer.Dispose();
            }
        }
        public static IEnumerable<T> SearchById(string recordId)
        {
            using (var searcher = new IndexSearcher(Directory, false))
            {
                var searchQuery = new TermQuery(new Term("Id", recordId.ToLower()));
                var hits = searcher.Search(searchQuery, null, Int32.MaxValue, Sort.RELEVANCE).ScoreDocs;
                var result = hits.Select(i => searcher.Doc(i.Doc)).Select(i => (Activator.CreateInstance(typeof(T), i)) as T).ToList();                
                return result;
            }
        }

        public static void Optimize()
        {
            var analyzer = new StandardAnalyzer(Version.LUCENE_30);
            using (var writer = new IndexWriter(Directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                analyzer.Close();
                writer.Optimize();
                writer.Dispose();
            }
        }

        private static IEnumerable<Document> _search(string searchQuery, string searchField = "")
        {
            // validation
            if (string.IsNullOrEmpty(searchQuery.Replace("*", "").Replace("?", "")))
            {
                return new List<Document>();
            }

            // set up lucene searcher
            using (var searcher = new IndexSearcher(Directory, false))
            {
                const int hitsLimit = Int32.MaxValue;
                var analyzer = new StandardAnalyzer(Version.LUCENE_30);

                // search by single field
                if (!string.IsNullOrEmpty(searchField))
                {
                    var parser = new QueryParser(Version.LUCENE_30, searchField, analyzer);
                    parser.DefaultOperator = QueryParser.Operator.AND;

                    var query = ParseQuery(searchQuery, parser);
                    var hits = searcher.Search(query, hitsLimit).ScoreDocs;
                    var results = _mapLuceneToDataList(hits, searcher);

                    analyzer.Close();
                    searcher.Dispose();
                    return results;
                }
                // search by multiple fields (ordered by RELEVANCE)
                else
                {
                    var propertyFields = GetPropertyFields();
                    var parser = new MultiFieldQueryParser(Version.LUCENE_30, propertyFields, analyzer);
                    var query = ParseQuery(searchQuery, parser);
                    var hits = searcher.Search(query, null, hitsLimit, Sort.RELEVANCE).ScoreDocs;
                    var results = _mapLuceneToDataList(hits, searcher);
                    analyzer.Close();
                    searcher.Dispose();
                    return results;
                }
            }
        }

        private static string[] GetPropertyFields()
        {
            var type = typeof(T);
            return type.GetProperties().Select(i => i.Name).ToArray();
        }

        private static Query ParseQuery(string searchQuery, QueryParser parser)
        {
            Query query;
            try
            {
                query = parser.Parse(searchQuery.Trim());
            }
            catch (ParseException)
            {
                query = parser.Parse(QueryParser.Escape(searchQuery.Trim()));
            }
            return query;
        }

        private static IEnumerable<Document> _mapLuceneToDataList(IEnumerable<ScoreDoc> hits, IndexSearcher searcher)
        {
            return hits.Select(hit => searcher.Doc(hit.Doc)).ToList();
        }

        private static void DeleteOldDocument(T item, PropertyInfo[] properties, IndexWriter writer)
        {
            var id = properties.First(i => i.Name.Equals("Id")).GetValue(item, null).ToString();
            var searchQuery = new TermQuery(new Term("Id", id));
            writer.DeleteDocuments(searchQuery);
        }
    }
}