using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Xml.Serialization;

namespace Creational;

[AttributeUsage(AttributeTargets.Property)]
public class CascadeDeleteAttribute : Attribute
{
}

public class WikiPage
{
    [Key]
    [StringLength(200)]
    public String Title { get; set; }

    public Int32 Ns { get; set; }

    public Int32 Id { get; set; }

    [StringLength(8)]
    public String Issue { get; set; }

    public WikiPageContent Content { get; set; }
}

public class WikiPageContent
{
    [Key]
    [StringLength(200)]
    public String Title { get; set; }

    [CascadeDelete]
    public WikiPage Page { get; set; }

    [Required]
    public String Text { get; set; }

    [StringLength(200)]
    public String Model { get; set; }

    [StringLength(200)]
    public String Format { get; set; }

    [StringLength(40)]
    public String Sha1 { get; set; }
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

        modelBuilder.Entity<WikiPageContent>()
            .HasOne(e => e.Page)
            .WithOne(e => e.Content)
            .HasForeignKey<WikiPageContent>(e => e.Title)
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