using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.IO;
using RacingTelemetryAnalyzer.Data;
using RacingTelemetryAnalyzer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITelemetryAnalysisService, TelemetryAnalysisService>();
builder.Services.AddScoped<DataGeneratorService>();
builder.Services.AddScoped<CsvImportService>();

// Configure Kestrel Server Limits for large telemetry datasets (up to 250 MB)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 250_000_000;
});

// Configure Form Options for multipart file uploads
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 250_000_000;
    options.ValueLengthLimit = 250_000_000;
    options.MultipartHeadersLengthLimit = 64_000;
});

builder.Services.AddControllersWithViews();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

// Production React SPA static assets (from frontend/dist)
var distPath = Path.Combine(app.Environment.ContentRootPath, "frontend", "dist");
if (!Directory.Exists(distPath))
{
    distPath = Path.Combine(AppContext.BaseDirectory, "frontend", "dist");
}

var indexPath = Path.Combine(distPath, "index.html");
bool forceRazor = builder.Configuration.GetValue<bool>("UseRazorFrontend", false) || args.Contains("--razor");
bool hasReactBuild = !forceRazor && File.Exists(indexPath);

if (hasReactBuild)
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(distPath),
        RequestPath = ""
    });
}

app.UseCors();
app.UseRouting();

app.UseAuthorization();

app.MapControllers();

if (hasReactBuild)
{
    // Specific legacy MVC route so explicit action invocations (/Home/Index, /Track/Index, /Telemetry/Index, /Home/Privacy, etc.) remain functional
    app.MapControllerRoute(
        name: "mvc_legacy",
        pattern: "{controller}/{action}/{id?}");

    // SPA Fallback: Serves React index.html for all client-side routes (/, /tracks, /telemetry, /garage)
    // Strictly rejects /api/* requests so nonexistent API endpoints return HTTP 404 JSON, never index.html
    app.MapFallback(async context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"success\":false,\"message\":\"API endpoint not found.\"}");
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(indexPath);
    });
}
else
{
    // Fallback to legacy default route when no React production build exists
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
}

// Explicit database initialization triggered only by command-line argument (--seed) or configuration (SeedData=true)
if (args.Contains("--seed") || builder.Configuration.GetValue<bool>("SeedData", false))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DbInitializer.SeedDefaultDataAsync(db);
}

app.Run();
