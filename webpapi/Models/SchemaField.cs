using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DynamicMongoAPI.Models
{
    public class SchemaField
    {
        public SchemaField()
        {
            Console.WriteLine("[SCHEMA FIELD] Creating new SchemaField instance");
        }
        
        [BsonElement("name")]
        [JsonPropertyName("name")]
        public string Name {
            get => _name;
            set {
                Console.WriteLine($"[SCHEMA FIELD] Setting Name from '{_name}' to '{value}'");
                _name = value;
            }
        }
        private string _name = string.Empty;

        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        [JsonPropertyName("type")]
        public FieldType Type { get; set; }

        [BsonElement("required")]
        [JsonPropertyName("required")]
        public bool Required { get; set; }

        [BsonElement("enum")]
        [JsonPropertyName("enum")]
        public List<string>? EnumValues { get; set; }

        [BsonElement("function")]
        [JsonPropertyName("function")]
        public string? Function { get; set; }
    }
}