using Creational;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

// Add services to the container.
services.AddRazorPages();
services.AddServerSideBlazor();

var configuration = builder.Configuration;

var settings = configuration.Get<AppSettings>();

services.Configure<AppSettings>(configuration);

services.AddDbContextFactory<ApplicationDb>(options =>
    options
        .UseSqlServer(settings.DbConnection),
    ServiceLifetime.Transient
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
