using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using DynamicMongoAPI.Services;
using DynamicMongoAPI.Utils;
using DynamicMongoAPI.Constants;

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

builder.Services.AddMongoServices(builder.Configuration);

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

// Enable Swagger (always, not just in Development)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "XavierGrid API v1");
    c.RoutePrefix = "swagger";
});

// Redirect root to Swagger UI
app.MapGet("/", context => {
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});
app.UseAuthorization();
app.MapControllers();

app.Run();