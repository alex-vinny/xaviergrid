using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using DynamicMongoAPI.Services;
using DynamicMongoAPI.Utils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options => {
    options.JsonSerializerOptions.Converters.Add(new RegexConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "XavierGrid API",
        Version = "v1",
        Description = "Dynamic MongoDB API - XavierGrid Project"
    });
    
    // Group endpoints by controller attributes
    c.DocInclusionPredicate((docName, apiDesc) => true);
    c.TagActionsBy(api => api.GroupName ?? "Default");
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

// Redirect root to Swagger UI - MUST be first middleware
app.MapGet("/", context => {
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

// Only enable Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "XavierGrid API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseStaticFiles(); // Static files middleware
app.UseAuthorization();
app.MapControllers();

app.Run();