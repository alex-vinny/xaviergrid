using DynamicMongoAPI.Constants;
using DynamicMongoAPI.Services;
using MongoDB.Driver;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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
            // Escape regex-specific characters when writing to JSON
            writer.WriteStringValue(Regex.Escape(value));
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

            // Metadata service
            services.AddSingleton<IMetadataService, MetadataService>(); // <-- implement MetadataService

            // Namespace admin service
            services.AddSingleton<INamespaceManagementService, NamespaceManagementService>();

            // Schema service (depends on MetadataService + NamespaceManagementService)
            services.AddSingleton<ISchemaService>(provider =>
                new SchemaService(
                    provider.GetRequiredService<IMongoClient>(),
                    config,
                    provider.GetRequiredService<IMetadataService>(),
                    provider.GetRequiredService<INamespaceManagementService>()
                )
            );

            // Domain services
            services.AddSingleton<IHistoryService, HistoryService>();
            services.AddSingleton<IRelationService, RelationService>();
            services.AddSingleton<IFieldFunctionService, FieldFunctionService>();
            services.AddSingleton<IRuleWarningService, RuleWarningService>();
            services.AddSingleton<IRuleValidator, RuleValidator>();
            services.AddSingleton<ITranslator, Translator>();

            // Dynamic entity service
            services.AddSingleton(provider =>
                new DynamicEntityService(
                    provider.GetRequiredService<IMongoClient>(),
                    config,
                    provider.GetRequiredService<ISchemaService>(),
                    provider.GetRequiredService<IHistoryService>(),
                    provider.GetRequiredService<IRuleValidator>(),
                    provider.GetRequiredService<IRuleWarningService>(),
                    provider.GetRequiredService<IFieldFunctionService>(),
                    provider.GetRequiredService<IRelationService>(),
                    provider.GetRequiredService<ITranslator>(),
                    provider.GetRequiredService<IMetadataService>()
                )
            );

            return services;
        }
    }

}