using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DynamicMongoAPI.Models
{
    public class EntityResponse
    {
        public DynamicEntity Document { get; set; } = default!;
        public IList<string> Warnings { get; set; } = new List<string>();
    }
}