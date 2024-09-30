using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add Ocelot services
builder.Services.AddOcelot();
builder.Configuration.AddJsonFile("ocelot.json");


var app = builder.Build();

// Configure Ocelot middleware
await app.UseOcelot();

app.Run();
