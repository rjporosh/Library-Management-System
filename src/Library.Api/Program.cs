using Library.Api.Endpoints;
using Library.Application.DependencyInjection;
using Library.Infrastructure.DependencyInjection;
using Library.Infrastructure.Persistence.Repositories.InMemory.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapBookEndpoints();

var seeder = app.Services.GetRequiredService<InMemoryDataSeeder>();
seeder.Seed();

app.Run();