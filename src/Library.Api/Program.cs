using Library.Application.DependencyInjection;
using Library.Infrastructure.DependencyInjection;
using Library.Infrastructure.Persistence.Repositories.InMemory.Seed;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Application & Infrastructure
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

// API
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Library Management System API")
            .WithTheme(ScalarTheme.Mars)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// Seed in-memory data
var seeder = app.Services.GetRequiredService<InMemoryDataSeeder>();
seeder.Seed();

app.UseHttpsRedirection();

app.Run();