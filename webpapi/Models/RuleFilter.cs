using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DynamicMongoAPI.Models
{
    public class RuleFilter
    {
        /// <summary>
        /// JSON Query Language payload from jsonquerylang.org
        /// Example:
        /// { "eq": ["status", "active"] }
        /// </summary>
        [BsonElement("jql")]
        [BsonRepresentation(BsonType.Document)]
        public JsonNode? Jql { get; set; }
    }
}