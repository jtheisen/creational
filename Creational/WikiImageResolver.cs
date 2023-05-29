using Microsoft.EntityFrameworkCore;
using NLog;
using System.Net;

namespace Creational;

public class WikiImageResolver
{
    static Logger log = LogManager.GetCurrentClassLogger();

    private readonly IDbContextFactory<ApplicationDb> dbFactory;

    public String GetGenericImageUrl(String fileName) => $"https://commons.wikimedia.org/wiki/Special:FilePath/{fileName}";

    public WikiImageResolver(IDbContextFactory<ApplicationDb> dbFactory)
    {
        this.dbFactory = dbFactory;
    }

    IQueryable<String> GetImageQuery(ApplicationDb db) =>
        db.ImageLinks.Select(i => i.Filename)
        .Union(db.TaxoboxImages.Select(i => i.Filename))
        .Distinct();

    public void ResolveAllImages()
    {
        var db = dbFactory.CreateDbContext();

        var total = GetImageQuery(db).Count();

        var alreadyDone = (
            from il in GetImageQuery(db)
            join ri in db.ResolvedImages on il equals ri.Filename into resolved2
            from ri in resolved2.DefaultIfEmpty()
            where ri != null
            select il
        ).Count();

        LogManager.Flush();

        Int32 resolved = 0, written = 0;

        var cycleNo = 0;

        while (true)
        {
            var retry = 0;

            try
            {
                written = ResolveSomeImages(10);
            }
            catch (Exception ex)
            {
                if (retry == 2)
                {
                    log.Warn(ex, "Got exception on retry #{retry}, giving up", retry);

                    throw;
                }

                ++retry;

                log.Warn("Got exception, doing retry #{retry}", retry);

                continue;
            }

            resolved += written;

            if (resolved == 0)
            {
                log.Info("No images to resolve");

                return;
            }
            else if (written == 0)
            {
                break;
            }

            if ((++cycleNo % 5) == 0)
            {
                log.Info("Resolved  {alreadyDone} + {resolved} of {total} so far", alreadyDone, resolved, total);
            }
        }

        log.Info("Resolved {alreadyDone} + {resolved} of {total}, done.", alreadyDone, resolved, total);
    }

    public Int32 ResolveSomeImages(Int32 batchSize)
    {
        var db = dbFactory.CreateDbContext();

        var pendingImages = (
            from fn in GetImageQuery(db)
            join ri in db.ResolvedImages on fn equals ri.Filename into resolved
            from ri in resolved.DefaultIfEmpty()
            where ri == null
            select fn
        )
        .Take(batchSize)
        .ToArray();

        if (pendingImages.Length == 0 )
        {
            return 0;
        }

        var tasks = pendingImages.Select(i => ResolveImageUrl(db, i)).ToArray();

        Task.WaitAll(tasks);

        db.SaveChanges();

        return tasks.Length;
    }

    public async Task<Uri> ResolveImageUrl(ApplicationDb db, String fileName)
    {
        log.Info("Resolving '{fileName}'", fileName);

        var url = GetGenericImageUrl(fileName);

        var request = WebRequest.CreateHttp(url);
        request.UserAgent = "creational";
        request.Method = "HEAD";

        HttpWebResponse response;

        try
        {
            response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(true);
        }
        catch (WebException ex)
        {
            response = (HttpWebResponse)ex.Response;
        }

        var status = response.StatusCode;

        db.ResolvedImages.Add(new WikiResolvedImage
        {
            Filename = fileName,
            Uri = response.ResponseUri?.ToString(),
            Status = (Int32)status
        });

        return response.ResponseUri;
    }
}
