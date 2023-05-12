using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Creational;

public class TaxoboxSpaceAnalyzer
{
    private readonly IDbContextFactory<ApplicationDb> dbContextFactory;
    private readonly TaxoboxParser taxoboxParser;

    public TaxoboxSpaceAnalyzer(IDbContextFactory<ApplicationDb> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;

        taxoboxParser = new TaxoboxParser();
    }

    public void Analyze()
    {
        var db = dbContextFactory.CreateDbContext();

        var contents = db.Taxoboxes.ToArray();
    }
}
