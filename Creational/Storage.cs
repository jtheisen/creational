﻿using Creational.Migrations;
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
    ToRead = 0,
    ToExtractContent = 1,
    ToParseTaxobox = 2,
    Finished,
    
    Failed = -1
}

public enum PageType
{
    Ignored = 0,
    Redirect = 1,
    Content = 2,
    TaxoTemplate = 3
}

public class WikiPage
{
    [StringLength(2)]
    public String Lang { get; set; }

    [StringLength(200)]
    public String Title { get; set; }

    public PageType Type { get; set; }

    public Int32 Ns { get; set; }

    public Int32 Id { get; set; }

    [StringLength(8)]
    public String Issue { get; set; }

    public WikiPageContent Content { get; set; }

    public WikiTaxobox Taxobox { get; set; }

    public ICollection<WikiImageLink> ImageLinks { get; set; }

    public ParsingResult Parsed { get; set; }

    public Step Step { get; set; }

    [StringLength(2000)]
    public String StepError { get; set; }
}

public class WikiPageContent
{
    [StringLength(2)]
    public String Lang { get; set; }

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

public class WikiTaxoboxImage
{
    [StringLength(2)]
    public String Lang { get; set; }

    [StringLength(200)]
    public String Title { get; set; }

    [StringLength(2000)]
    public String Filename { get; set; }
}

public class WikiImageLink
{
    [StringLength(2)]
    public String Lang { get; set; }

    [StringLength(200)]
    public String Title { get; set; }

    public Int32 Position { get; set; }

    [StringLength(2000)]
    public String Filename { get; set; }

    [StringLength(2000)]
    public String Text { get; set; }

    [CascadeDelete]
    public WikiPage Page { get; set; }
}

public class WikiResolvedImage
{
    [StringLength(2000)]
    public String Filename { get; set; }

    [StringLength(2000)]
    public String Uri { get; set; }

    public Int32 Status { get; set; }
}

public enum WikiImageDataKind
{
    Unknown = 0,
    Thumbnail = 1
}

public class WikiImageData
{
    [StringLength(2000)]
    public String Filename { get; set; }

    public WikiImageDataKind Kind { get; set; }

    [StringLength(2000)]
    public String Uri { get; set; }

    [StringLength(120)]
    public String ContentType { get; set; }

    public Int32 Width { get; set; }

    public Int32 Height { get; set; }

    [StringLength(60)]
    public String Error { get; set; }

    public Byte[] Data { get; set; }
}

public class WikiTaxobox
{
    [StringLength(2)]
    public String Lang { get; set; }

    [StringLength(200)]
    public String Title { get; set; }

    [CascadeDelete]
    public WikiPage Page { get; set; }

    [StringLength(40)]
    public String Sha1 { get; set; }

    [StringLength(8000)]
    public String Taxobox { get; set; }
}

public enum PageImageSituation
{
    Unknown = 0,
    NoEntry = 1,
    Unsupported = 2,
    Simple = 3,
    Multiple = 4
}

public class ParsingResult
{
    [StringLength(2)]
    public String Lang { get; set; }

    [StringLength(200)]
    public String Title { get; set; }

    [StringLength(60)]
    public String TemplateName { get; set; }

    public PageImageSituation ImageSituation { get; set; }

    [StringLength(60)]
    public String Taxon { get; set; }

    [StringLength(60)]
    public String Genus { get; set; }

    [StringLength(60)]
    public String Species { get; set; }

    [CascadeDelete]
    public WikiPage Page { get; set; }

    [StringLength(40)]
    public String Sha1 { get; set; }

    [StringLength(200)]
    public String Redirection { get; set; }

    [StringLength(1000)]
    public String Exception { get; set; }

    public Boolean HasTruncationIssue { get; set; }

    public Boolean HasDuplicateTaxoboxEntries { get; set; }

    public Boolean WithTaxobox { get; set; }

    public TaxoTemplateValues TaxoTemplateValues { get; set; }

    public ICollection<TaxoboxImageEntry> TaxoboxImageEntries { get; set; }

    public ICollection<TaxoboxEntry> TaxoboxEntries { get; set; }

    public ICollection<TaxonomyEntry> TaxonomyEntries { get; set; }
}

public class TaxoboxImageEntry
{
    [StringLength(2)]
    public String Lang { get; set; }

    [StringLength(200)]
    public String Title { get; set; }

    [StringLength(2000)]
    public String Filename { get; set; }

    public ParsingResult ParsedPage { get; set; }
}

public class TaxoboxEntry
{
    [StringLength(2)]
    public String Lang { get; set; }

    [StringLength(200)]
    public String Title { get; set; }

    [CascadeDelete]
    public ParsingResult ParsedPage { get; set; }

    [StringLength(60)]
    public String Key { get; set; }

    [StringLength(200)]
    public String Value { get; set; }
}

public class TaxoTemplateValues
{
    [StringLength(2)]
    public String Lang { get; set; }

    [StringLength(200)]
    public String Title { get; set; }

    [StringLength(80)]
    public String Rank { get; set; }

    [StringLength(80)]
    public String Parent { get; set; }

    [StringLength(80)]
    public String SameAs { get; set; }

    [StringLength(200)]
    public String PageTitle { get; set; }

    [StringLength(80)]
    public String Name { get; set; }

    [CascadeDelete]
    public ParsingResult ParsedPage { get; set; }
}

public class TaxonomyEntry
{
    [StringLength(2)]
    public String Lang { get; set; }

    [StringLength(200)]
    public String Title { get; set; }

    public Int32 No { get; set; }

    [CascadeDelete]
    public ParsingResult ParsedPage { get; set; }

    [StringLength(80)]
    public String Rank { get; set; }

    [StringLength(80)]
    public String Name { get; set; }

    [StringLength(80)]
    public String NameLocal { get; set; }
}

public class TaxonomyRelation
{
    [StringLength(2)]
    public String Lang { get; set; }

    [StringLength(200)]
    public String Ancestor { get; set; }

    public WikiPage AncestorPage { get; set; }

    [StringLength(200)]
    public String Descendant { get; set; }

    public WikiPage DescendantPage { get; set; }

    public Int32 No { get; set; }
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
    public DbSet<WikiImageLink> ImageLinks { get; set; }
    public DbSet<WikiTaxoboxImage> TaxoboxImages { get; set; }
    public DbSet<WikiResolvedImage> ResolvedImages { get; set; }
    public DbSet<WikiImageData> ImageData { get; set; }
    public DbSet<WikiTaxobox> Taxoboxes { get; set; }

    public DbSet<ParsingResult> ParsingResults { get; set; }
    public DbSet<TaxoTemplateValues> TaxoTemplateValues { get; set; }
    public DbSet<TaxoboxImageEntry> TaxoboxImageEntries { get; set; }
    public DbSet<TaxoboxEntry> TaxoboxEntries { get; set; }
    public DbSet<TaxonomyRelation> TaxonomyRelations { get; set; }

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
            .HasKey(e => new { e.Lang, e.Title })
            ;
        modelBuilder.Entity<WikiPage>()
            .HasIndex(e => new { e.Lang, e.Id })
            .IsUnique()
            ;
        modelBuilder.Entity<WikiPage>()
            .HasIndex(e => new { e.Lang, e.Step, e.Type })
            ;
        modelBuilder.Entity<WikiPage>()
            .HasIndex(e => new { e.Lang, e.StepError })
            ;

        modelBuilder.Entity<WikiPageContent>()
            .HasKey(e => new { e.Lang, e.Title })
            ;
        modelBuilder.Entity<WikiPageContent>()
            .HasOne(e => e.Page)
            .WithOne(e => e.Content)
            .HasForeignKey<WikiPageContent>(e => new { e.Lang, e.Title })
            ;
        modelBuilder.Entity<WikiPageContent>()
            .Property(e => e.Text)
            .HasColumnType("nvarchar(max)")
            ;

        modelBuilder.Entity<WikiTaxoboxImage>()
            .HasKey(e => new { e.Lang, e.Title })
            ;
        modelBuilder.Entity<WikiTaxoboxImage>()
            .HasKey(e => new { e.Title })
            ;
        modelBuilder.Entity<WikiTaxoboxImage>()
            .Property(e => e.Filename)
            .UseCollation("Latin1_General_BIN2")
            ;

        modelBuilder.Entity<WikiImageLink>()
            .HasKey(e => new { e.Lang, e.Title, e.Position })
            ;
        modelBuilder.Entity<WikiImageLink>()
            .Property(e => e.Filename)
            .UseCollation("Latin1_General_BIN2")
            ;
        modelBuilder.Entity<WikiImageLink>()
            .HasOne(e => e.Page)
            .WithMany(e => e.ImageLinks)
            ;

        modelBuilder.Entity<WikiResolvedImage>()
            .HasKey(e => e.Filename)
            ;
        modelBuilder.Entity<WikiResolvedImage>()
            .Property(e => e.Filename)
            .UseCollation("Latin1_General_BIN2")
            ;

        modelBuilder.Entity<WikiImageData>()
            .HasKey(e => new { e.Filename, e.Kind })
            ;
        modelBuilder.Entity<WikiImageData>()
            .Property(e => e.Filename)
            .UseCollation("Latin1_General_BIN2")
            ;

        modelBuilder.Entity<WikiTaxobox>()
            .HasKey(e => new { e.Lang, e.Title })
            ;
        modelBuilder.Entity<WikiTaxobox>()
            .HasOne(e => e.Page)
            .WithOne(e => e.Taxobox)
            .HasForeignKey<WikiTaxobox>(e => new { e.Lang, e.Title })
            ;

        modelBuilder.Entity<ParsingResult>()
            .HasKey(e => new { e.Lang, e.Title })
            ;
        modelBuilder.Entity<ParsingResult>()
            .HasOne(e => e.Page)
            .WithOne(e => e.Parsed)
            .HasForeignKey<ParsingResult>(e => new { e.Lang, e.Title })
            ;

        modelBuilder.Entity<TaxoTemplateValues>()
            .HasKey(e => new { e.Lang, e.Title })
            ;
        modelBuilder.Entity<TaxoTemplateValues>()
            .HasOne(e => e.ParsedPage)
            .WithOne(e => e.TaxoTemplateValues)
            .HasForeignKey<TaxoTemplateValues>(e => new { e.Lang, e.Title })
            ;

        modelBuilder.Entity<TaxoboxImageEntry>()
            .HasKey(e => new { e.Lang, e.Title })
            ;
        modelBuilder.Entity<TaxoboxImageEntry>()
            .Property(e => e.Filename)
            .UseCollation("Latin1_General_BIN2")
            ;
        modelBuilder.Entity<TaxoboxImageEntry>()
            .HasOne(e => e.ParsedPage)
            .WithMany(e => e.TaxoboxImageEntries)
            .HasForeignKey(e => new { e.Lang, e.Title })
            ;

        modelBuilder.Entity<TaxoboxEntry>()
            .HasKey(e => new { e.Lang, e.Title, e.Key })
            ;
        modelBuilder.Entity<TaxoboxEntry>()
            .HasOne(e => e.ParsedPage)
            .WithMany(e => e.TaxoboxEntries)
            ;
        modelBuilder.Entity<TaxonomyEntry>()
            .HasKey(e => new { e.Lang, e.Title, e.No })
            ;
        modelBuilder.Entity<TaxonomyEntry>()
            .HasOne(e => e.ParsedPage)
            .WithMany(e => e.TaxonomyEntries)
            ;
        modelBuilder.Entity<TaxonomyEntry>()
            .HasIndex(e => new { e.Lang, e.Rank, e.Title })
            .IncludeProperties(e => new { e.Name, e.NameLocal })
            ;
        modelBuilder.Entity<TaxonomyEntry>()
            .HasIndex(e => new { e.Lang, e.Name, e.Rank, e.Title })
            .IncludeProperties(e => new { e.NameLocal })
            ;
        modelBuilder.Entity<TaxonomyEntry>()
            .HasIndex(e => new { e.Lang, e.NameLocal, e.Rank, e.Title })
            .IncludeProperties(e => new { e.Name })
            ;

        modelBuilder.Entity<TaxonomyRelation>()
            .HasKey(e => new { e.Lang, e.Ancestor, e.Descendant })
            ;
        modelBuilder.Entity<TaxonomyRelation>()
            .HasIndex(e => new { e.Lang, e.Descendant, e.Ancestor })
            .IncludeProperties(e => new { e.No })
            ;
        modelBuilder.Entity<TaxonomyRelation>()
            .HasOne(e => e.AncestorPage)
            .WithMany()
            .HasForeignKey(e => new { e.Lang, e.Ancestor })
            ;
        modelBuilder.Entity<TaxonomyRelation>()
            .HasOne(e => e.DescendantPage)
            .WithMany()
            .HasForeignKey(e => new { e.Lang, e.Descendant })
            ;

        SetUnicode(modelBuilder);
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