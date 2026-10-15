var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

feat(api): configure ASP.NET Core Web API project and Swagger

- Target net8.0 with nullable and implicit usings enabled
- Register EF Core SQL Server, session and authentication services
- Add custom SessionAuthenticationHandler scheme
- Add Swagger/OpenAPI with ApiOperationDescriptions operation filter
- Document HTTP 400/401/403/404/409 responses for endpoints

