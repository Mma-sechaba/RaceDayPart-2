using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Security;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var databaseProvider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var databaseConnection = builder.Configuration.GetConnectionString("RaceDayDb");

if (string.IsNullOrWhiteSpace(builder.Configuration["ASPNETCORE_URLS"]) &&
    string.IsNullOrWhiteSpace(builder.Configuration["ASPNETCORE_HTTP_PORTS"]) &&
    string.IsNullOrWhiteSpace(builder.Configuration["ASPNETCORE_HTTPS_PORTS"]))
{
    builder.WebHost.UseUrls("http://localhost:5184");
}

builder.Services.AddControllers();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "RaceDay API", Version = "v1", Description = "RaceDay event management API for Organisers and Participants." });
    options.OperationFilter<RaceDay.Api.ApiOperationDescriptions>();
});
builder.Services.AddDbContext<RaceDayDbContext>(options =>
{
    if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(databaseConnection, sql => sql.EnableRetryOnFailure());
    }
    else if (databaseProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(databaseConnection ?? "Data Source=raceday.db");
    }
    else
    {
        throw new InvalidOperationException($"Unsupported database provider '{databaseProvider}'. Use 'Sqlite' or 'SqlServer'.");
    }
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddAuthentication("RaceDaySession")
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>("RaceDaySession", _ => { });
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
if (!app.Environment.IsEnvironment("Testing") &&
    builder.Configuration.GetValue<bool>("Database:EnsureCreatedOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    var strategy = db.Database.CreateExecutionStrategy();
    await strategy.ExecuteAsync(async () =>
    {
        await db.Database.EnsureCreatedAsync();
        if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[EVENT]', N'U') IS NOT NULL AND COL_LENGTH(N'EVENT', N'DistanceKm') IS NULL
            BEGIN
                ALTER TABLE [EVENT] ADD [DistanceKm] decimal(6,2) NOT NULL CONSTRAINT [DF_EVENT_DistanceKm] DEFAULT (0);
                IF COL_LENGTH(N'EVENT', N'Distance') IS NOT NULL UPDATE [EVENT] SET [DistanceKm] = [Distance];
            END;
            IF OBJECT_ID(N'[EVENT]', N'U') IS NOT NULL AND COL_LENGTH(N'EVENT', N'Distance') IS NOT NULL
                ALTER TABLE [EVENT] DROP COLUMN [Distance];
            IF OBJECT_ID(N'[EVENT]', N'U') IS NOT NULL AND COL_LENGTH(N'EVENT', N'BannerImageUrl') IS NULL
                ALTER TABLE [EVENT] ADD [BannerImageUrl] varchar(500) NULL;
            IF OBJECT_ID(N'[PARTICIPANT]', N'U') IS NOT NULL AND COL_LENGTH(N'PARTICIPANT', N'ProfilePictureUrl') IS NULL
                ALTER TABLE [PARTICIPANT] ADD [ProfilePictureUrl] varchar(500) NULL;
            IF OBJECT_ID(N'[EVENT_ORGANISER]', N'U') IS NOT NULL AND COL_LENGTH(N'EVENT_ORGANISER', N'ProfilePictureUrl') IS NULL
                ALTER TABLE [EVENT_ORGANISER] ADD [ProfilePictureUrl] varchar(500) NULL;
            IF OBJECT_ID(N'[CATEGORY]', N'U') IS NOT NULL AND COL_LENGTH(N'CATEGORY', N'Price') IS NULL
                ALTER TABLE [CATEGORY] ADD [Price] decimal(8,2) NOT NULL CONSTRAINT [DF_CATEGORY_Price] DEFAULT (0);
            IF OBJECT_ID(N'[CATEGORY]', N'U') IS NOT NULL AND COL_LENGTH(N'CATEGORY', N'Capacity') IS NULL
                ALTER TABLE [CATEGORY] ADD [Capacity] int NULL;
            IF OBJECT_ID(N'[RESULT]', N'U') IS NOT NULL AND COL_LENGTH(N'RESULT', N'Status') IS NULL
                ALTER TABLE [RESULT] ADD [Status] varchar(20) NOT NULL CONSTRAINT [DF_RESULT_Status] DEFAULT ('Finished');
            """);
        }
    });
}
app.Run();

public partial class Program { }
