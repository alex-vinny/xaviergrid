using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DynamicMongoAPI.Models
{
    public class RuleDefinition
    {
        [BsonElement("action")]
        [BsonRepresentation(BsonType.String)]
        [JsonPropertyName("action")]
        public RuleAction Action { get; set; }

        [BsonIgnore]
        [JsonPropertyName("rule")]
        public JsonNode? Rule {
            get => _rule;
            set => _rule = value;
        }
        private JsonNode? _rule;
        
        // BSON serialization field - store as JSON string
        [BsonElement("rule")]
        public string? RuleJson {
            get {
                if (_rule != null)
                    return _rule.ToJsonString();
                return null;
            }
            set {
                if (value != null)
                    _rule = System.Text.Json.JsonSerializer.Deserialize<JsonNode>(value);
                else
                    _rule = null;
            }
        }

        [BsonElement("message")]
        [JsonPropertyName("message")]
        public string Message { get; set; } = "Business rule violated";
    }
}