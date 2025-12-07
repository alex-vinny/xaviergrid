using DynamicMongoAPI.Services;
using MongoDB.Driver;

namespace DynamicMongoAPI.Utils
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMongoServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<IConfigurator, Configurator>();

            services.AddSingleton<IMongoClient>(provider =>
            {
                var config = provider.GetRequiredService<IConfigurator>();
                return new MongoClient(config.GetConnectionString());
            });

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