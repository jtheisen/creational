using DataflowPipelineBuilder;
using Microsoft.EntityFrameworkCore;
using NLog;
using Pipelines;
using System;
using System.IO.Pipes;

namespace Creational;

public class WikiImageDownloader
{
    static Logger log = LogManager.GetCurrentClassLogger();

    private readonly IDbContextFactory<ApplicationDb> dbFactory;

    HttpClient client = new HttpClient();

    public WikiImageDownloader(IDbContextFactory<ApplicationDb> dbFactory)
    {
        this.dbFactory = dbFactory;

        client.DefaultRequestHeaders.UserAgent.ParseAdd("Creational/1.0");
    }

    public void DownloadAllThumbs()
    {
        var db = dbFactory.CreateDbContext();

        var imagesQuery =
            from ri in db.ResolvedImages
            join id in db.ImageData on new { Kind = WikiImageDataKind.Thumbnail, ri.Filename } equals new { id.Kind, id.Filename } into data
            from id in data.DefaultIfEmpty()
            let haveData = id.Filename != null
            orderby haveData descending
            select new { Image = ri, HaveData = haveData }
            ;

        var input =
            Pipes.FromQueryable(imagesQuery)
            .Transform(source =>
                from i in source.AsParallel()
                where !i.HaveData
                where i.Image.Uri.StartsWith("https://upload.wikimedia.org/wikipedia/commons", StringComparison.InvariantCultureIgnoreCase)
                select DownloadImage(i.Image).Result
            )
            ;

        var output = Pipes.FromAction<WikiImageData>(SaveImage);

        var pipeline = input.BuildCopyingPipeline(output);

        pipeline.Start()
            .ReportSpectre()
            .Wait();
    }

    void SaveImage(WikiImageData image)
    {
        var db = dbFactory.CreateDbContext();

        db.ImageData.Add(image);

        db.SaveChanges();
    }

    async Task<WikiImageData> DownloadImage(WikiResolvedImage image)
    {
        var thumbUri = image.GetThumbnailImageUrl();

        try
        {
            return await DownloadImageCore(image, thumbUri);
        }
        catch (Exception)
        {
            log.Error($"Exception on downloading: {thumbUri}");

            throw;
        }
    }

    async Task<WikiImageData> DownloadImageCore(WikiResolvedImage image, String thumbUri)
    {
        var response = await client.GetAsync(thumbUri);

        var imageData = new WikiImageData
        {
            Filename = image.Filename,
            Kind = WikiImageDataKind.Thumbnail,
            Uri = thumbUri
        };

        if (!response.IsSuccessStatusCode)
        {
            imageData.Error = $"status: {response.StatusCode}";

            return imageData;
        }

        imageData.ContentType = response.Content.Headers.ContentType.MediaType;

        var data = imageData.Data = await response.Content.ReadAsByteArrayAsync();

        try
        {
            using (var sixLaborsImage = Image.Load(data.AsSpan()))
            {
                imageData.Width = sixLaborsImage.Width;
                imageData.Height = sixLaborsImage.Height;
            }
        }
        catch (Exception)
        {
            imageData.Error = "can't decode";
        }

        return imageData;
    }
}
