using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Creational;

//public class RequestJob
//{
//    private readonly ApplicationDb db;
//    private readonly HttpClient client;

//    public RequestJob(ApplicationDb db, HttpClient client)
//    {
//        this.db = db;
//        this.client = client;
//    }

//    public async Task Run()
//    {
//        var batchSize = 1;

//        var locations = await db.WpLocations
//            .Include(l => l.Content)
//            .Where(l => l.IsFetchPending)
//            .Take(1)
//            .ToArrayAsync();

//        if (locations.Length == 0) return;

//        foreach (var location in locations)
//        {
//            await Load(location);

//            await db.SaveChangesAsync();
//        }        
//    }

//    async Task Load(WikiPage location)
//    {
//        var content = location.Content ??= new WikiPageXml();
//        content.LastFetchedAt = DateTimeOffset.Now;

//        try
//        {
//            var response = await client.GetAsync(location.Name);

//            content.LastFetchStatus = (Int32)response.StatusCode;

//            if (response.IsSuccessStatusCode)
//            {
//                content.LastSuccessfulContent = await response.Content.ReadAsStringAsync();
//            }
//            else
//            {
//                content.LastErrorContent = await response.Content.ReadAsStringAsync();
//            }
//        }
//        catch (Exception ex)
//        {
//            content.LastErrorContent = ex.Message;
//        }
//    }
//}
