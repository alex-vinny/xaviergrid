using DynamicMongoAPI.Constants;
using DynamicMongoAPI.Services;
using MongoDB.Driver;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DynamicMongoAPI.Utils
{
    public class RegexConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Return the raw string value - no transformation needed during deserialization
            return reader.GetString() ?? string.Empty;
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            // Write string value as-is without escaping
            writer.WriteStringValue(value);
        }
    }


    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMongoServices(this IServiceCollection services, IConfiguration config)
        {
            // Mongo client
            var mongoConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION")
                                        ?? config.GetConnectionString("MongoDB")
                                        ?? config["MongoDB:ConnectionString"];
            services.AddSingleton<IMongoClient>(new MongoClient(mongoConnectionString));

            // Schema service
            var masterSchemaDb = Environment.GetEnvironmentVariable("MONGODB_MASTER_DATABASE")
                               ?? config["MongoDB:MasterSchemaDatabase"]
                               ?? AppConstants.MasterSchemaDatabaseName;

            // Namespace admin service
            services.AddTransient<INamespaceService, NamespaceService>();
            services.AddTransient<INamespaceManagementService, NamespaceManagementService>();
            services.AddTransient<ISchemaService, SchemaService>();
            services.AddTransient<IHistoryService, HistoryService>();
            services.AddTransient<IRelationService, RelationService>();
            services.AddTransient<IFieldFunctionService, FieldFunctionService>();
            services.AddTransient<IRuleWarningService, RuleWarningService>();
            services.AddTransient<IRuleValidator, RuleValidator>();
            services.AddTransient<ITranslator, Translator>();
            services.AddTransient<IDynamicEntityService, DynamicEntityService>();

            return services;
        }
    }

}