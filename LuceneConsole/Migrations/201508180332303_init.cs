namespace LuceneConsole.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class init : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SampleModels",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        Description = c.String(),
                        CreatedDate = c.DateTime(nullable: false),
                        ViewCount = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.SampleModels");
        }
    }
}
