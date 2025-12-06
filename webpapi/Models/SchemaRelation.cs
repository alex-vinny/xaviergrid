using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DynamicMongoAPI.Models
{
    public class SchemaRelation
    {
        public string Collection { get; set; } = string.Empty;
        public string ForeignField { get; set; } = "_id";
        public string As { get; set; } = string.Empty;

        public RelationType Type { get; set; } = RelationType.ManyToOne;
        public bool IsNullable { get; set; } = true;
        public object? DefaultValue { get; set; } = null;
    }
}