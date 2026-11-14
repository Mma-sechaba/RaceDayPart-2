using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Data;
using RaceDay.Api.Services;
using System.Collections.Concurrent;

namespace RaceDay.Api.Tests;

public class RaceDayApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var databaseName = "RaceDayTests_" + Guid.NewGuid();
            services.RemoveAll<DbContextOptions<RaceDayDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<RaceDayDbContext>>();
            services.RemoveAll<RaceDayDbContext>();
            services.AddDbContext<RaceDayDbContext>(options => options.UseInMemoryDatabase(databaseName));
            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton<FakeBlobStorageService>();
            services.AddSingleton<IBlobStorageService>(provider => provider.GetRequiredService<FakeBlobStorageService>());
        });
    }
}

public sealed class FakeBlobStorageService : IBlobStorageService
{
    public ConcurrentDictionary<string, byte[]> Files { get; } = new();

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, string folder, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var url = $"https://blob.test/{folder}/{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        Files[url] = buffer.ToArray();
        return url;
    }
}
