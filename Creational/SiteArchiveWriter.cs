using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;

namespace Creational;

public class SiteArchive
{
    [JsonProperty("creatures")]
    public SiteCreature[] Creatures { get; set; }

    [JsonProperty("images")]
    public SiteImage[] Images { get; set; }
}

public class SiteImage
{
    public String Filename { get; set; }

    public Int32[] PageIs { get; set; }
}

public class SiteCreature
{
    [JsonProperty("title")]
    public String Title { get; set; }

    [JsonProperty("parentI")]
    public Int32 ParentI { get; set; }

    [JsonProperty("lastChildI")]
    public Int32 LastChildI { get; set; }

    [JsonProperty("nextSiblingI")]
    public Int32 NextSiblingI { get; set; }
}

public class SiteArchiveWriter
{
    private readonly IDbContextFactory<ApplicationDb> dbContextFactory;

    public SiteArchiveWriter(IDbContextFactory<ApplicationDb> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    public Byte[] MakeArchive(Int32? limit = null)
    {
        var ms = new MemoryStream();

        WriteArchive(ms, limit);

        return ms.ToArray();
    }

    public void WriteArchive(String fileName, Int32? limit = null)
    {
        var mode = File.Exists(fileName) ? FileMode.Truncate : FileMode.Create;

        using var outputStream = new FileStream(fileName, mode, FileAccess.Write);

        WriteArchive(outputStream, limit);
    }

    public void WriteArchive(Stream outputStream, Int32? limit = null)
    {
        var db = dbContextFactory.CreateDbContext();

        var pages = db.Pages
            .Where(p => p.Step > Step.ToExtractContent)
            .ToArray();

        var relations = db.TaxonomyRelations.ToArray();

        var images = db.ImageLinks
            .Where(i => i.Filename.EndsWith(".jpeg") || i.Filename.EndsWith(".jpg") || i.Filename.EndsWith(".png"))
            .ToArray();

        var rootPage = pages.FirstOrDefault(p => p.Title == "Lebewesen");

        if (rootPage is null) throw new Exception("Can't find root page");

        var pageChildren = (
            from p in pages
            join r in relations on p.Title equals r.Ancestor into descendants
            from d in descendants
            select (p, d: d.DescendantPage)
        ).ToLookup(e => e.p.Title, e => e.d);

        SiteCreature[] GetSiteCreatures()
        {
            var creatures = new SiteCreature[pages.Length + 1];

            var i = 0;

            SiteCreature VisitDescendants(WikiPage page, Int32 parentI)
            {
                var children = pageChildren[page.Title];

                var selfI = i;

                var creature = creatures[selfI] = new SiteCreature
                {
                    Title = page.Title,
                    ParentI = parentI,
                };

                SiteCreature latestChild = null;

                foreach (var child in children)
                {
                    var currentChildI = ++i;

                    creature.LastChildI = currentChildI;

                    var nextChild = VisitDescendants(child, selfI);

                    if (latestChild is not null)
                    {
                        latestChild.NextSiblingI = currentChildI;
                    }

                    latestChild = nextChild;
                }

                if (latestChild is not null)
                {
                    latestChild.NextSiblingI = i;
                }

                return creature;
            }

            VisitDescendants(rootPage, 0);

            var firstNullI = Array.IndexOf(creatures, null);

            return creatures[..firstNullI];
        }

        var siteCreatures = GetSiteCreatures();

        var siteCreatureIsByTitle = siteCreatures
            .Select((c, i) => (c, i))
            .ToDictionary(p => p.c.Title, p => p.i);

        var siteImages = (
            from i in images
            group i by i.Filename into image
            select new SiteImage
            {
                Filename = image.Key,
                PageIs = image
                    .Select(i => siteCreatureIsByTitle.GetValueOrDefault(i.Title, -1))
                    .Where(i => i >= 0)
                    .ToArray()
            }
        ).ToArray();

        JsonSerializer serializer = new JsonSerializer();
        

        var writer = new StreamWriter(outputStream, Encoding.UTF8, leaveOpen: true);

        var jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented };

        var siteArchive = new SiteArchive
        {
            Creatures = siteCreatures,
            Images = siteImages
        };

        serializer.Serialize(jsonWriter, siteArchive);

        jsonWriter.Flush();
    }
}
