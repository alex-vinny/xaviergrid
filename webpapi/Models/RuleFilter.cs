using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DynamicMongoAPI.Models
{
    public class RuleFilter
    {
        private JsonNode? _jql;
        
        /// <summary>
        /// JSON Query Language payload from jsonquerylang.org
        /// Example:
        /// { "eq": ["status", "active"] }
        /// </summary>
        [BsonIgnore]
        public JsonNode? Jql {
            get => _jql;
            set => _jql = value;
        }
        
        // BSON serialization field - store as BSON document
        [BsonElement("jql")]
        public MongoDB.Bson.BsonDocument? BsonData { get; set; }
        
        // Add implicit conversion from JsonNode to RuleFilter
        public static implicit operator RuleFilter(JsonNode node)
        {
            var filter = new RuleFilter { _jql = node };
            // Convert JsonNode to BsonDocument for MongoDB storage
            if (node != null)
            {
                var json = node.ToJsonString();
                filter.BsonData = MongoDB.Bson.BsonDocument.Parse(json);
            }
            return filter;
        }
        
        // Add implicit conversion from RuleFilter to JsonNode
        public static implicit operator JsonNode(RuleFilter filter)
        {
            if (filter?._jql != null)
                return filter._jql;
            
            // Convert from BsonDocument if needed
            if (filter?.BsonData != null)
            {
                var json = filter.BsonData.ToJson();
                filter._jql = System.Text.Json.JsonSerializer.Deserialize<JsonNode>(json);
                return filter._jql;
            }
            
            return null;
        }
    }
}