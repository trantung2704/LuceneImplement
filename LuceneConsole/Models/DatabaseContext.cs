using System.Data.Entity;
using System.Data.Entity.Core.Common.CommandTrees;

namespace LuceneConsole.Models
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext()
            : base("DefaultConnection")
        {
        }


        public DbSet<SampleModel> SampleModels { get; set; }
    }
}