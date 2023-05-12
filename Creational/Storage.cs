using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Xml.Serialization;

namespace Creational;

[AttributeUsage(AttributeTargets.Property)]
public class CascadeDeleteAttribute : Attribute
{
}

public enum Step
{
    ToRead,
    ToParse,
    Finished
}

public enum PageType
{
    Ignored = 0,
    Content = 1,
    Redirect = 2
}

public class WikiPage
{
    [Key]
    [StringLength(200)]
    public String Title { get; set; }

    public PageType Type { get; set; }

    public Int32 Ns { get; set; }

    public Int32 Id { get; set; }

    [StringLength(8)]
    public String Issue { get; set; }

    public WikiPageContent Content { get; set; }

    public ParsingResult Parsed { get; set; }

    public Step Step { get; set; }
}

public class WikiPageContent
{
    [Key]
    [StringLength(200)]
    public String Title { get; set; }

    [CascadeDelete]
    public WikiPage Page { get; set; }

    [StringLength(200)]
    public String RedirectTitle { get; set; }

    public String Text { get; set; }

    [StringLength(200)]
    public String Model { get; set; }

    [StringLength(200)]
    public String Format { get; set; }

    [StringLength(40)]
    public String Sha1 { get; set; }
}

public class ParsingResult
{
    [Key]
    [StringLength(200)]
    public String Title { get; set; }

    [CascadeDelete]
    public WikiPage Page { get; set; }

    [StringLength(40)]
    public String Sha1 { get; set; }

    [StringLength(200)]
    public String Redirection { get; set; }

    [StringLength(1000)]
    public String Exception { get; set; }

    public Boolean WithTaxobox { get; set; }

    public ICollection<TaxoboxEntry> TaxoboxEntries { get; set; }
}

public class TaxoboxEntry
{
    [StringLength(200)]
    public String Title { get; set; }

    [CascadeDelete]
    public ParsingResult ParsedPage { get; set; }

    [StringLength(60)]
    public String Key { get; set; }

    [StringLength(200)]
    public String Value { get; set; }
}

//public class WpProcessedLocationClade
//{
//    [StringLength(200)]
//    public String Url { get; set; }

//    [CascadeDelete]
//    public WpProcessedLocation ProcessedLocation { get; set; }

//    public DateTimeOffset LastProcessedAt { get; set; }

//    public Int32 Order { get; set; }
//}

//public class Creature
//{
//    [Key]
//    [StringLength(80)]
//    public String Name { get; set; }

//    public Creature Parent { get; set; }

//    public String ParentName { get; set; }

//    public ICollection<CreatureImage> Images { get; set; }
//}

//public class CreatureImage
//{
//    [Key]
//    [StringLength(80)]
//    public String Name { get; set; }

//    public Creature Creature { get; set; }

//    [StringLength(200)]
//    public String Url { get; set; }
//}

public class ApplicationDb : DbContext
{
    public DbSet<WikiPage> Pages { get; set; }
    public DbSet<WikiPageContent> PageContents { get; set; }
    public DbSet<ParsingResult> ParsingResults { get; set; }
    public DbSet<TaxoboxEntry> TaxoboxEntries { get; set; }

    //public DbSet<WpProcessedLocation> WpProcessedLocations { get; set; }
    //public DbSet<Creature> Creatures { get; set; }
    //public DbSet<CreatureImage> Images { get; set; }

    public ApplicationDb(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Latin1_General_100_CI_AS_SC_UTF8");

        modelBuilder.Entity<WikiPage>()
            .HasIndex(e => e.Step);

        modelBuilder.Entity<WikiPageContent>()
            .HasOne(e => e.Page)
            .WithOne(e => e.Content)
            .HasForeignKey<WikiPageContent>(e => e.Title)
            ;

        modelBuilder.Entity<ParsingResult>()
            .HasOne(e => e.Page)
            .WithOne(e => e.Parsed)
            .HasForeignKey<ParsingResult>(e => e.Title)
            ;

        modelBuilder.Entity<TaxoboxEntry>()
            .HasKey(e => new { e.Title, e.Key })
            ;
        modelBuilder.Entity<TaxoboxEntry>()
            .HasOne(e => e.ParsedPage)
            .WithMany(e => e.TaxoboxEntries)
            ;

        //SetUnicode(modelBuilder);
        SetCascades(modelBuilder);
    }

    void SetUnicode(ModelBuilder modelBuilder)
    {
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(String) && p.GetColumnType() is null))
        {
            property.SetIsUnicode(false);
        }
    }

    void SetCascades(ModelBuilder modelBuilder)
    {
        foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            var navigation = relationship.GetNavigation(true);

            var cascadeDelete = navigation?.PropertyInfo?.GetCustomAttribute<CascadeDeleteAttribute>();

            if (cascadeDelete is not null)
            {
                relationship.DeleteBehavior = DeleteBehavior.Cascade;
            }
            else
            {
                relationship.DeleteBehavior = DeleteBehavior.ClientCascade;
            }
        }
    }
}