using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DynamicMongoAPI.Models
{
    public class SchemaField
    {
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public FieldType Type { get; set; }

        [BsonElement("required")]
        public bool Required { get; set; }

        [BsonElement("enum")]
        public List<string>? EnumValues { get; set; }

        [BsonElement("function")]
        public string? Function { get; set; }
    }
}