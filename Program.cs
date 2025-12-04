using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using DynamicMongoAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "XavierGrid API",
        Version = "v1",
        Description = "Dynamic MongoDB API - XavierGrid Project"
    });
    
    // Organize endpoints into groups
    c.TagActionsBy(api => {
        var route = api.RelativePath?.ToLower() ?? "";
        
        if (route.StartsWith("api/") && route.Contains("/history")) return new[] { "Entity History" };
        if (route.StartsWith("api/") && (route.Contains("/search") || route.Contains("/aggregate"))) return new[] { "Entity Advanced" };
        if (route.StartsWith("api/")) return new[] { "Crud Entity" };
        if (route.StartsWith("schemas/") && (route.Contains("/rules") || route.Contains("/relations"))) return new[] { "Rules and Relations" };
        if (route.StartsWith("schemas/")) return new[] { "Schemas" };
        
        return new[] { "General" };
    });
});

// Register MongoDB client
var mongoConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("MongoDB")
    ?? builder.Configuration["MongoDB:ConnectionString"];
builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConnectionString));

// Register services
var masterSchemaDatabaseName = builder.Configuration["MongoDB:MasterSchemaDatabase"] ?? "masterSchemas";
builder.Services.AddSingleton<MongoSchemaService>(provider =>
    new MongoSchemaService(
        provider.GetRequiredService<IMongoClient>(),
        masterSchemaDatabaseName
    ));
builder.Services.AddSingleton<DynamicMongoService>(provider =>
    new DynamicMongoService(
        provider.GetRequiredService<IMongoClient>(),
        provider.GetRequiredService<MongoSchemaService>(),
        builder.Configuration
    ));

// Configure Kestrel server options
var port = Environment.GetEnvironmentVariable("PORT");
builder.WebHost.ConfigureKestrel(options =>
{
    if (!string.IsNullOrEmpty(port) && int.TryParse(port, out var portNumber))
    {
        options.ListenAnyIP(portNumber);
    }
    else
    {
        options.ListenAnyIP(5000); // Default port
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseStaticFiles(); // Add static files middleware

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "XavierGrid API v1");
        c.RoutePrefix = "";
    });
}

app.UseAuthorization();
app.MapControllers();

app.Run();