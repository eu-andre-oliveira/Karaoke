using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Karaoke.Api.Hubs;
using Karaoke.Application.Interfaces;
using Karaoke.Application.UseCases;
using Karaoke.Domain.Constants;
using Karaoke.Domain.Entities;
using Karaoke.Infrastructure.Data;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Banco de dados (Sqlite por padrão; Postgres/SqlServer via appsettings) ----------
var provider = builder.Configuration.GetValue<string>("Database:Provider") ?? "Sqlite";

builder.Services.AddDbContext<KaraokeDbContext>(options =>
{
    switch (provider)
    {
        case "Postgres":
            options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
            break;
        case "SqlServer":
            options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
            break;
        default:
            options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=karaoke.db");
            break;
    }
});
builder.Services.AddScoped<IKaraokeDbContext>(sp => sp.GetRequiredService<KaraokeDbContext>());

// ---------- Identity + cookie (o React roda no mesmo domínio via nginx/proxy do Vite) ----------
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;

        options.User.RequireUniqueEmail = true;

        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<KaraokeDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "karaoke.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;

    // API: nunca redirecionar para página de login, só responder o status.
    options.Events.OnRedirectToLogin = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

// ---------- Casos de uso ----------
builder.Services.AddScoped<PlaylistService>();
builder.Services.AddScoped<BibliotecaService>();
builder.Services.AddScoped<EventoService>();
builder.Services.AddScoped<FilaService>();
builder.Services.AddSingleton<IFilaNotifier, SignalRFilaNotifier>();

// ---------- Web ----------
builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddSignalR()
    .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SupportNonNullableReferenceTypes();
    o.UseAllOfToExtendReferenceSchemas();
    o.SchemaFilter<Karaoke.Api.OpenApi.RequireNonNullablePropertiesSchemaFilter>();
});

// Proteção básica contra abuso nas rotas públicas (login, cadastro, entrar em evento).
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("publico", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue("RateLimit:PublicoPorMinuto", 30),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // A API só é alcançável através do nginx do compose, então confiamos nos cabeçalhos dele.
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

// ---------- Migrations + seed ----------
using (var scope = app.Services.CreateScope())
{
    if (app.Configuration.GetValue("Database:AutoMigrate", true))
    {
        var db = scope.ServiceProvider.GetRequiredService<KaraokeDbContext>();
        await db.Database.MigrateAsync();
    }

    await SeedDataAsync(scope.ServiceProvider);
}

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<FilaHub>("/hubs/fila");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

static async Task SeedDataAsync(IServiceProvider services)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    foreach (var role in UserRoles.AllRoles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Admin só é criado se as credenciais vierem da configuração
    // (user-secrets, variável de ambiente Seed__AdminEmail / Seed__AdminPassword).
    var config = services.GetRequiredService<IConfiguration>();
    var adminEmail = config["Seed:AdminEmail"];
    var adminPassword = config["Seed:AdminPassword"];
    if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        return;

    if (await userManager.FindByEmailAsync(adminEmail) is not null)
        return;

    var admin = new ApplicationUser
    {
        UserName = adminEmail,
        Email = adminEmail,
        NomeCompleto = "Administrador",
        EmailConfirmed = true
    };

    var result = await userManager.CreateAsync(admin, adminPassword);
    if (result.Succeeded)
        await userManager.AddToRoleAsync(admin, UserRoles.Admin);
}

// Necessário para WebApplicationFactory nos testes de integração.
public partial class Program;
