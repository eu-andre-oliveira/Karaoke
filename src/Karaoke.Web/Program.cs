using Microsoft.EntityFrameworkCore;
using Karaoke.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Database provider selection (configurable in appsettings.json)
// Supported values: "Sqlite", "Postgres", "SqlServer"
var provider = builder.Configuration.GetValue<string>("Database:Provider") ?? "Sqlite";

switch (provider)
{
    case "Postgres":
        builder.Services.AddDbContext<KaraokeDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
        break;

    case "SqlServer":
        builder.Services.AddDbContext<KaraokeDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));
        break;

    default: // Sqlite
        builder.Services.AddDbContext<KaraokeDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=karaoke.db"));
        break;
}

builder.Services.AddControllersWithViews();

// Optional: enable runtime compilation in development
#if DEBUG
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
#endif

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
