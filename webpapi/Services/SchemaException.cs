using DynamicMongoAPI.Constants;
using DynamicMongoAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace DynamicMongoAPI.Services
{
    public sealed class SchemaException : Exception
    {
        private SchemaException(string message) : base(message) { }

        public static void NotDefined(string entityName)
            => throw new SchemaException($"Schema for entity '{entityName}' is not defined.");

        public static void AlreadyExists(string entityName)
            => throw new SchemaException($"Schema for entity '{entityName}' already exists. Use PUT to update.");

        public static void NotFound(string entityName)
            => throw new SchemaException($"Schema for entity '{entityName}' not found.");
    }
}