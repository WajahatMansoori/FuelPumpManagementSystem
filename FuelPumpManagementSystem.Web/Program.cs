using System.Diagnostics;
using FuelPumpManagementSystem.Application.Interfaces;
using FuelPumpManagementSystem.Application.Services;
using Microsoft.EntityFrameworkCore;
using Shared.FPMS_DB;
using Shared.Helpers;

var builder = WebApplication.CreateBuilder(args);


//builder.Services.AddDbContext<FPMSDbContext>(options =>
//    options.UseSqlite(builder.Configuration.GetConnectionString("FPMSDb")));
var connectionString = @"Data Source=D:\Work\Freelance\Windsurf\FPMS\Shared\FuelPumpManagementSystem.db";
builder.Services.AddDbContext<FPMSDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IDispenserService, DispenserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ISiteService, SiteService>();
builder.Services.AddTransient<FileUploadHelper>();


// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

var chromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
var url = "http://localhost:5000";
var chromeProfilePath = @"C:\MyApp\ChromeProfile";

Directory.CreateDirectory(chromeProfilePath);

var chromeArgs =
    $"--app={url} " +
    $"--user-data-dir=\"{chromeProfilePath}\" " +
    "--disable-infobars " +
    "--no-first-run " +
    "--disable-session-crashed-bubble " +
    "--disable-translate";

Process? chromeProcess = null;
object chromeLock = new();

void StartChrome()
{
    lock (chromeLock)
    {
        try
        {
            chromeProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = chromePath,
                    Arguments = chromeArgs,
                    UseShellExecute = true
                },
                EnableRaisingEvents = true
            };

            chromeProcess.Exited += (s, e) =>
            {
                // Small delay to avoid rapid looping
                Thread.Sleep(1000);
                //StartChrome();
            };

            chromeProcess.Start();
        }
        catch (Exception ex)
        {
            File.AppendAllText("chrome_error.log", ex + Environment.NewLine);
        }
    }
}

// Start Chrome AFTER web server is ready
app.Lifetime.ApplicationStarted.Register(() =>
{
    Task.Run(() =>
    {
        // Small delay to ensure Kestrel is listening
        Thread.Sleep(1500);
        //StartChrome();
    });
});

// Stop Chrome when app shuts down
app.Lifetime.ApplicationStopping.Register(() =>
{
    try
    {
        if (chromeProcess != null && !chromeProcess.HasExited)
        {
            chromeProcess.Kill(true);
        }
    }
    catch { }
});



app.Run();
