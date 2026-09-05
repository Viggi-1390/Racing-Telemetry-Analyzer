using Microsoft.EntityFrameworkCore;
using RacingTelemetryAnalyzer.Data;
using RacingTelemetryAnalyzer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITelemetryAnalysisService, TelemetryAnalysisService>();
builder.Services.AddScoped<ReportService>();
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
app.UseCors();
app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
