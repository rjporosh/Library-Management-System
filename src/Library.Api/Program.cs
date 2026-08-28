using Library.Application.DependencyInjection;
using Library.Infrastructure.DependencyInjection;
using Library.Infrastructure.Persistence.Repositories.InMemory.Seed;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Application & Infrastructure
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

// MVC Controllers
builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi();

// Add CORS policy for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();
    
// Use the CORS policy
app.UseCors("Frontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Library Management System API")
            .WithTheme(ScalarTheme.Mars)
            .WithDefaultHttpClient(
                ScalarTarget.CSharp,
                ScalarClient.HttpClient);
    });
}

// Controllers
app.MapControllers();

// Seed demo data
var seeder = app.Services.GetRequiredService<InMemoryDataSeeder>();
seeder.Seed();

app.Run();
public partial class Program;