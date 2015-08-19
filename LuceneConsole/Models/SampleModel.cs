using System;
using Lucene.Net.Documents;

namespace LuceneConsole.Models
{
    public class SampleModel : IIndexable
    {
        public SampleModel()
        {
        }

        public SampleModel(Document document)
        {
            Id = new Guid(document.GetField("Id").StringValue);
            Description = document.GetField("Description").StringValue;
            CreatedDate = DateTools.StringToDate(document.GetField("CreatedDate").StringValue);
            ViewCount = Convert.ToInt32(document.GetField("ViewCount").StringValue);
        }

        public Guid Id { get; set; }

        public string Description { get; set; }

        public DateTime CreatedDate { get; set; }

        public long ViewCount { get; set; }

        public Document ToDocument()
        {
            var document = new Document();
            document.Add(new Field("Id", Id.ToString(), Field.Store.YES, Field.Index.ANALYZED));
            document.Add(new Field("Description", Description, Field.Store.YES, Field.Index.ANALYZED));
            document.Add(new Field("CreatedDate", DateTools.DateToString(CreatedDate, DateTools.Resolution.MILLISECOND), Field.Store.YES, Field.Index.ANALYZED));
            document.Add(new Field("ViewCount", ViewCount.ToString(), Field.Store.YES, Field.Index.ANALYZED));
            return document;
        }
    }
}