using System;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using HtmlAgilityPack;
using LuceneConsole.Models;

namespace LuceneConsole
{
    internal  class Program
    {
        private static readonly DatabaseContext Db = new DatabaseContext();

        private static void Main(string[] args)
        {
            //ClearIndexAndDb();
            //GenerateSampleData(100000).Wait();
            
            var startTime = DateTime.Now;
            var dbSearchResult = Db.SampleModels.Find(new Guid("26A033D4-EA33-4CF5-A24A-FFF684BD16C5"));
            Console.WriteLine("Databse process");
            Console.WriteLine("result: " + dbSearchResult);
            Console.WriteLine("Time:" + (DateTime.Now - startTime));
            
            Console.WriteLine();
            
            startTime = DateTime.Now;
            var luceneSearchResult = LuceneRepository<SampleModel>.SearchById("26A033D4-EA33-4CF5-A24A-FFF684BD16C5");
            Console.WriteLine("Lucene process");
            Console.WriteLine("result: " + luceneSearchResult.Count());
            Console.WriteLine("Time:" + (DateTime.Now - startTime));            
        }        

        private static async Task GenerateSampleData(int recordsNumber)
        {
            var httpClient = new HttpClient();
            var htmlResult = new HtmlDocument();
            var random = new Random();

            for (int i = 0; i < recordsNumber; i++)
            {                
                var htmlStringResult = await httpClient.GetStringAsync("http://randomtextgenerator.com/");
                
                htmlResult.LoadHtml(htmlStringResult);
                var randomText = htmlResult.GetElementbyId("generatedtext").InnerText;            

                var randomNumber = random.Next(150);
                var newModel = new SampleModel
                               {
                                   Id = Guid.NewGuid(),
                                   Description = randomText,
                                   CreatedDate = DateTime.Now.AddDays(randomNumber),
                                   ViewCount = randomNumber
                               };

                Db.SampleModels.Add(newModel);
                LuceneRepository<SampleModel>.AddUpdateLuceneIndex(newModel);
            }
            Db.SaveChanges();
        }

        private static void ClearIndexAndDb()
        {
            LuceneRepository<SampleModel>.ClearLuceneIndex();
            foreach (var sampleModel in Db.SampleModels)
            {
                Db.SampleModels.Remove(sampleModel);
            }
            Db.SaveChanges();
        }
    }
}