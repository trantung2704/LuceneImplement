using Lucene.Net.Documents;

namespace LuceneConsole.Models
{
    public interface IIndexable
    {
        Document ToDocument();
    }
}