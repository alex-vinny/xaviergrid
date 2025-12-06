using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DynamicMongoAPI.Models
{
    public class RuleDefinition
    {
        [BsonRepresentation(BsonType.String)]
        public RuleAction Action { get; set; }

        public RuleFilter? Rule { get; set; }

        public string Message { get; set; } = "Business rule violated";
    }
}