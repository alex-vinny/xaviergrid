using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DynamicMongoAPI.Models
{
    public class EntityDefinition
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; } = default!;

        [BsonElement("namespace")]
        public string Namespace { get; set; } = default!;

        [BsonElement("id_namespace")]
        public ObjectId NamespaceId { get; set; }

        [BsonElement("id_schema")]
        public ObjectId? SchemaId { get; set; }
    }
}